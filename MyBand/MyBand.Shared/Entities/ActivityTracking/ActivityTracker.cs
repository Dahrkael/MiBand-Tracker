using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MyBand.Entities.Database;
using System.Linq;

namespace MyBand.Entities.ActivityTracking
{
    public class ActivityTracker
    {
        private Object streamLock = new Object();
        private long streamReadPosition  = 0;
        private long streamWritePosition = 0;
        private MemoryStream stream;

        private Progress syncingProgress;
        private int modeRegularDataType = 0;

        public ActivityTracker()
        {
            stream = new MemoryStream();
            syncingProgress = new Progress();

            //Database.DB.Create();
        }

        #region stream
        public void Write(byte[] buffer, int length)
        {
            if (stream != null)
            {
                //Debug.WriteLine(ByteArrayToHexViaLookup(buffer));
                lock (streamLock)
                {
                    stream.Seek(0, SeekOrigin.End);
                    stream.Write(buffer, 0, length);
                    streamWritePosition += length;
                    Debug.WriteLine("[Activity Data Buffer] " + length + " bytes added. Current total: " + stream.Length);
                }
            }
        }

        private int readStream()
        {
            if (stream != null)
            {
                lock(streamLock)
                {
                    stream.Position = streamReadPosition;
                    int ret = stream.ReadByte();
                    if (ret != -1)
                    {
                        streamReadPosition++;
                    }
                    return ret;
                }
            }
            return -1;
        }

        private void resetStream()
        {
            Debug.WriteLine("[SyncData] Reiniciando stream");
            if (stream != null)
            {
                stream.SetLength(0);
                stream.Position = 0;
            }
            streamReadPosition = 0;
            streamWritePosition = 0;
        }
        #endregion

        #region sync
        private async Task<List<ActivityDataFragment>> getActivities()
        {
            List<ActivityDataFragment> list = new List<ActivityDataFragment>();

            bool dataLeft = true;
            bool errorOcurred = false;
            // mientras queden datos se procesa una y otra vez
            while (dataLeft)
            {
                if (errorOcurred)
                {
                    Debug.WriteLine("Error ocurred during sync, theres data left to sync. try again later");
                    return list;
                }
                //cb.onStart();
                //registerCallback(m_CharActivityData, new _cls5());

                // reiniciamos el stream
                if (stream != null) { resetStream(); }

                Debug.WriteLine("[SyncData] Comando Fetch Data");
                bool start = await MiBand.Band.FetchData();

                if (!start)
                {
                    Debugger.Break();
                    //cb.onError("Write sync command failed!!!");
                    //cb.onStop();
                    //unRegisterCallback(m_CharActivityData);

                    // FIXME aqui deberia de signalizar algo
                    break;
                }
                //cb.onCommand();
                syncingProgress.Total = -1;
                syncingProgress.Current = 0;

                Debug.WriteLine("[SyncData] Espera antes de leer");

                // esperamos a que haya datos antes de leer
                while (streamWritePosition <= 10)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }

                Debug.WriteLine("[SyncData] Inicio de lectura");
                int fails = 0;
                while (true)
                {
                    int i = readStream();
                    if (i == -1)
                    {
                        // Unexpected 'EOF' detected...
                        Debug.WriteLine("[SyncData] EOF inicial");
                        errorOcurred = true;
                        break;
                    }

                    modeRegularDataType = i;
                    if (i == 0 || i == 1)
                    {
                        //break; // seguro?
                    }

                    Debug.WriteLine("Total fails before start: " + fails);
                    fails = 0;

                    Debug.WriteLine("[ActivityTracker] dataType: " + String.Format("0x{0:X02}", Convert.ToByte(i)));

                    ActivityDataFragment activitydatafragment = await parseActivityData();
                    if (activitydatafragment == null)
                    {
                        // reiniciar operativa y continuar donde se haya quedado ? 
                        Debug.WriteLine("[SyncData] Error de lectura");
                        bool stopped = await StopGetActivities();
                        errorOcurred = true;
                        break;
                    }
                    
                    int j = activitydatafragment.Data.Count;
                    // si es cero ya no hay mas datos
                    if (j == 0)
                    {
                        dataLeft = false;
                        break;
                    }

                    list.Add(activitydatafragment);
                    Debug.WriteLine("[SyncData] Añadido data fragment de " + j);
                }
            }
            Debug.WriteLine("[SyncData] No hay mas datos");
            //cb.onStop();
            //unRegisterCallback(m_CharActivityData);
            return list;
        }

        private async Task<ActivityDataFragment> parseActivityData()
        {
            int year   = readStream();
            int month  = readStream();
            int day    = readStream();
            int hour   = readStream();
            int minute = readStream();
            int second = readStream();

            // si alguno es null es que se la ido la pinza a la pulsera y habra que reiniciar el proceso desde el ultimo punto valido
            if ((year == -1) || (month == -1) || (day == -1) || (hour == -1) || (minute == -1) || (second == -1))
            {
                return null;
            }

            DateTime timeStamp = new DateTime(2000 + year, month + 1, day, hour, minute, second);
            Debug.WriteLine("[ActivityTracker] timestamp: " + timeStamp.ToString());
            int i = readStream();
            int j = readStream();

            // se jodio, reiniciar
            if ((i == -1) || (j == -1)) { return null; }

            int totalLength = (i & 0xFF) | ((j & 0xFF) << 8); // minutos por 3 valores cada uno
            if (modeRegularDataType == 1)
            {
                totalLength *= 3;
            }
            Debug.WriteLine("[ActivityTracker] totalLen: " + (totalLength / 3) + " minute(s)");
            syncingProgress.Total = totalLength;
            int l = readStream();
            int i1 = readStream();

            // hasta aqui son los 11 bytes iniciales *=*=*=*=*=*=*=*=*=*=*=*=*

            // se jodio, reiniciar
            if ((l == -1) || (i1 == -1)) {  return null; }

            int rawLength = (l & 0xFF) | ((i1 & 0xFF) << 8); // minutos por 3 valores cada uno
            int length;

            if (modeRegularDataType == 1)
            {
                length = rawLength * 3;
            }
            else
            {
                length = rawLength;
            }
            Debug.WriteLine("[ActivityTracker] len: " + (length / 3) + " minute(s)");
            List<ActivityData> list = new List<ActivityData>();
            while (length > 0)
            {
                byte category  = (byte)readStream();
                byte intensity = (byte)readStream();
                byte steps     = (byte)readStream();
                if (category == 0xFF || intensity == 0xFF || steps == 0xFF) 
                {
                    var last = list.OrderByDescending(a => a.Moment).FirstOrDefault();
                    if (last != null)
                    {
                        
                        var rescued = list.Where(a => a.Moment < last.Moment.Date).ToList<ActivityData>();
                        return new ActivityDataFragment(timeStamp, rescued);
                    }
                    return null; 
                }
                DateTime moment = timeStamp.AddMinutes(list.Count);
                ActivityData data = new ActivityData(intensity, steps, category, moment);
                Debug.WriteLine(data);
                list.Add(data);
                length -= 3;
                syncingProgress.Current += 3;
                //isyncactivitiescb.onProgress(m_ActivitySyncingProgress);
            }

            // esta confirmacion borra de la pulsera los datos ya que se supone que estan sincronizados
            bool ok =  await confirmFragmentSection(timeStamp, rawLength);
            return new ActivityDataFragment(timeStamp, list);
        }

        private async Task<bool> confirmFragmentSection(DateTime timeStamp, int length)
        {
            bool ok = await MiBand.Band.SyncConfirm(timeStamp, length);
            if (ok)
            {
                Debug.WriteLine("[" + timeStamp + ", " + length + "] Confirmada");
            }
            else
            {
                Debugger.Break();
            }
            return ok;
        }

        private bool processActivityFrames()
        {
            // comprobar si un dia tiene 1440 frames, entonces está completo
            // si hay dias incompletos antes resolverlos?
            // un dia completo => procesar pasos
            // para el sueño hay que buscar el patron 3->4->5->4->5->3
            // dos grupos de 3 implica acostarse y levantarse, puede ser entre varios dias

            var frames = DB.GetActivityFrames();
            var min = frames.Min(f => f.TimeStamp).Date;
            var max = frames.Max(f => f.TimeStamp).Date;

            if (min == max)
            {
                // si solo hay datos de un dia pasamos de procesar nada
                return false;
            }
            // procesamos los pasos y el sueño en orden inverso para poder ir borrando los dias
            for (DateTime curr = max; curr >= min; curr = curr.AddDays(-1))
            {
                // si el dia actual tiene todos los minutos (1440) se procesa normalmente
                // si no tenemos posibilidad de completar el dia se fuerza

                bool force = curr < max;
                bool stepsOK = processSteps(curr, force);
                bool sleepOK = processSleep(curr, force);

                if (stepsOK && sleepOK)
                {
                    // si uso la comprobacion de 1440 no puedo borrar los frames mas que al final
                    // borramos los frames de este dia
                    DB.DeleteActivityFrames(curr, curr.AddHours(23).AddMinutes(59).AddSeconds(59));
                }
            }

            return true;
        }

        private bool processSleep(DateTime date, bool force)
        {
            // comprobamos primero que este dia no este ya analizado
            var periodAlready = DB.GetSleepPeriods(date, date.AddHours(23).AddMinutes(59).AddSeconds(59)).FirstOrDefault();
            if (periodAlready != null)
            {
                Debug.WriteLine("[ProcessSleep] el dia " + date.ToString() + " ya tiene un periodo");
                return true;
            }

            // cogemos los frames de ayer y hoy
            List<ActivityFrame> frames = DB.GetActivityFrames(date.AddDays(-1), date.AddHours(23).AddMinutes(59).AddSeconds(59));

            // buscamos un grupo de 3 tras un grupo de 4 o 5
            var framesToSearch = frames.Where(f => f.Mode != (byte)ActivityCategory.Idle && f.Mode != (byte)ActivityCategory.NoData);

            ActivityFrame startFrame = null;
            ActivityFrame endFrame = null;
            // recorremos todos los frames en busca del 3->4/5 y 4/5->3
            // si por casualidad hay dos sueños en este bloque solo coge el segundo
            for (int i = 0; i < framesToSearch.Count()-1; i++)
            {
                var f = framesToSearch.ElementAt(i);
                if (f.Mode == (byte)ActivityCategory.Awake)
                {
                    var f2 = framesToSearch.ElementAt(i+1);
                    if ((f2.Mode == (byte)ActivityCategory.SleepWithMovement)  || (f2.Mode == (byte)ActivityCategory.SleepWithoutMovement))
                    {
                        startFrame = f;

                        // una vez localizado el ultimo 3 del inicio retrocedemos hasta dar con el primero
                        int j = i;
                        while(true)
                        {
                            if (j == 0) { break; }
                            var f3 = framesToSearch.ElementAt(j);
                            if (f3.Mode == (byte)ActivityCategory.Awake)
                            {
                                startFrame = f3;
                            }
                            else
                            {
                                break;
                            }
                            j--;
                        }
                    }
                }
                if (f.Mode == (byte)ActivityCategory.SleepWithMovement || f.Mode == (byte)ActivityCategory.SleepWithoutMovement)
                {
                    var f2 = framesToSearch.ElementAt(i+1);
                    if (f2.Mode == (byte)ActivityCategory.Awake)
                    {
                        endFrame = f2;

                        // una vez localizado el primer 3 del final avanzamos hasta dar con el ultimo
                        int j = i;
                        while (true)
                        {
                            if (j == (framesToSearch.Count() - 1)) { break; }
                            var f3 = framesToSearch.ElementAt(j);
                            if (f3.Mode == (byte)ActivityCategory.Awake)
                            {
                                startFrame = f3;
                            }
                            else
                            {
                                break;
                            }
                            j++;
                        }
                    }
                }
            }

            if (startFrame == null || endFrame == null)
            {
                // FIXME a lo mejor hay veces que los 3 no existen y tendria que buscar por 4s o 5s y ya despues buscar 3s para completar
                Debug.WriteLine("[ProcessSleep] " + date.ToString() + " el frame de inicio o de final no existen, no hay datos de sueño validos");
                return false;
            }
            // una vez tenemos el inicio y el fin, procedemos a generar los bloques
            // FIXME por ahora lo hago tal cual estan los minutos, ya lo mejorare despues
            var sleeps = frames.Where(f => f.Mode == (byte)ActivityCategory.SleepWithMovement || f.Mode == (byte)ActivityCategory.SleepWithoutMovement)
                                .Where(f => (f.TimeStamp > startFrame.TimeStamp) && (f.TimeStamp < endFrame.TimeStamp));

            if (sleeps.Count() > 0)
            {
                // iteramos por cada frame, metiendo en una misma lista los que son del mismo tipo, para luego generar los periodos
                List<List<ActivityFrame>> sleepBlocks = new List<List<ActivityFrame>>();
                List<ActivityFrame> currentBlock = new List<ActivityFrame>();

                // FIXME tener en cuenta los AWAKE

                for (int i = 0; i < sleeps.Count(); i++)
                {
                    // frame actual
                    var current = sleeps.ElementAt(i);
                    // si no hay frames en la lista
                    if (currentBlock.Count == 0)
                    {
                        // lo añadimos directamente
                        currentBlock.Add(current);
                        continue;
                    }
                    // si la lista ya tiene frames vemos que sea del mismo tipo que el primero
                    if (current.Mode == currentBlock.First().Mode)
                    {
                        // si lo es lo guardamos
                        currentBlock.Add(current);
                        continue;
                    }
                    // si es de diferente tipo guardamos la lista
                    sleepBlocks.Add(currentBlock);
                    // y creamos otra
                    currentBlock = new List<ActivityFrame>();
                    // retrocedemos el indice para que en la siguiente vuelta coja el mismo frame
                    i--;
                }
                // guardamos el ultimo bloque
                if (currentBlock.Count() > 0)
                {
                    sleepBlocks.Add(currentBlock);
                }

                // ahora que tenemos los bloques los transformamos a clases especificas para guardar en la BD
                List<Sleep.LightSleepBlock> lightSleeps = new List<Sleep.LightSleepBlock>();
                List<Sleep.DeepSleepBlock> deepSleeps = new List<Sleep.DeepSleepBlock>();
                foreach (var block in sleepBlocks)
                {
                    ActivityCategory mode = (ActivityCategory)block.First().Mode;
                    DateTime start = block.Min(f => f.TimeStamp);
                    DateTime end   = block.Max(f => f.TimeStamp);

                    if (mode == ActivityCategory.SleepWithMovement)
                    {
                        Sleep.LightSleepBlock section = new Sleep.LightSleepBlock(start, end);
                        lightSleeps.Add(section);
                    }

                    if (mode == ActivityCategory.SleepWithoutMovement)
                    {
                        Sleep.DeepSleepBlock section = new Sleep.DeepSleepBlock(start, end);
                        deepSleeps.Add(section);
                    }
                }

                // una vez transformados creamos el periodo
                Sleep.SleepPeriod period = new Sleep.SleepPeriod(lightSleeps, deepSleeps);
                // FIXME por ahora le asigno awakeat inicial, el final ya vere como lo pongo
                period.AwakeAt = startFrame.TimeStamp;

                //  y lo guardamos en la base de datos
                DB.AddSleepPeriod(period);
                // guardamos los bloques del periodo por separado (FIXME seguro?)
                DB.AddSleepBlocks(period.RetrieveBlocks());

                return true;
            }
            return false;
        }

        private bool processSteps(DateTime date, bool force)
        {
            // comprobamos primero que este dia no este ya analizado
            var periodAlready = DB.GetStepsPeriods(date, date).FirstOrDefault();
            if (periodAlready != null)
            {
                Debug.WriteLine("[ProcessSteps] el dia " + date.ToString() + " ya tiene un periodo");
                return true;
            }

            // cogemos los frames desde hoy a las 00:00 hasta las 23:59
            List<ActivityFrame> frames = DB.GetActivityFrames(date, date.AddHours(23).AddMinutes(59).AddSeconds(59));

            if (!force && frames.Count != 1440)
            {
                Debug.WriteLine("ProcessSteps] El dia " + date.ToString() + " no tiene todos los frames. (" + frames.Count + ")");
                return false;
            }
            var steps = frames.Where(f => f.Mode == (byte)ActivityCategory.Moving || f.Mode == (byte)ActivityCategory.Moving2).OrderBy(f => f.TimeStamp);
            if (steps.Count() > 0)
            {
                // iteramos por cada frame, metiendo en una misma lista los que son del mismo tipo, para luego generar los periodos
                List<List<ActivityFrame>> stepsBlocks = new List<List<ActivityFrame>>();
                List<ActivityFrame> currentBlock = new List<ActivityFrame>();

                for (int i = 0; i < steps.Count(); i++)
                {
                    // frame actual
                    var current = steps.ElementAt(i);
                    // si no hay frames en la lista
                    if (currentBlock.Count == 0)
                    {
                        // lo añadimos directamente
                        currentBlock.Add(current);
                        continue;
                    }
                    // si la lista ya tiene frames vemos que sea del mismo tipo que el primero
                    if (current.Mode == currentBlock.First().Mode)
                    {
                        // si lo es lo guardamos
                        currentBlock.Add(current);
                        continue;
                    }
                    // si es de diferente tipo guardamos la lista
                    stepsBlocks.Add(currentBlock);
                    // y creamos otra
                    currentBlock = new List<ActivityFrame>();
                    // retrocedemos el indice para que en la siguiente vuelta coja el mismo frame
                    i--;
                }
                // guardamos el ultimo bloque
                if (currentBlock.Count() > 0)
                {
                    stepsBlocks.Add(currentBlock);
                }

                // ahora que tenemos los bloques los transformamos a clases especificas para guardar en la BD
                List<Steps.WalkStepsBlock> walkSteps = new List<Steps.WalkStepsBlock>();
                List<Steps.RunStepsBlock> runSteps = new List<Steps.RunStepsBlock>();
                foreach (var block in stepsBlocks)
                {
                    ActivityCategory mode = (ActivityCategory)block.First().Mode;
                    DateTime start = block.Min(f => f.TimeStamp);
                    DateTime end   = block.Max(f => f.TimeStamp);
                    int stepCount  = block.Sum(f => f.Steps);
                    int runs       = block.Sum(f => f.Runs);

                    if (mode == ActivityCategory.Moving)
                    {
                        Steps.WalkStepsBlock section = new Steps.WalkStepsBlock(start, end, stepCount, runs);
                        walkSteps.Add(section);
                    }

                    if (mode == ActivityCategory.Moving2)
                    {
                        Steps.RunStepsBlock section = new Steps.RunStepsBlock(start, end, stepCount, runs);
                        runSteps.Add(section);
                    }
                }

                // una vez transformados creamos el periodo
                Steps.StepsPeriod period = new Steps.StepsPeriod(walkSteps, runSteps);
                //  y lo guardamos en la base de datos
                DB.AddStepsPeriod(period);
                // guardamos los bloques del periodo por separado (FIXME seguro?)
                DB.AddStepsBlocks(period.RetrieveBlocks());

                return true;
            }
            return false;
        }

        private bool saveActivityToDB(List<ActivityDataFragment> activities)
        {
            Debug.WriteLine("[SyncData] Guardando fragmentos en BD. numero: " + activities.Count);

            List<ActivityFrame> frames = new List<ActivityFrame>();
            foreach (var fragment in activities)
            {
                foreach (var data in fragment.Data)
                {
                    // FIXME los ceros los guardo para saber cuando tengo un dia completo (1440)
                    //if (data.Mode != (byte)ActivityCategory.Idle && data.Mode != (byte)ActivityCategory.NoData)
                    {
                        ActivityFrame frame = new ActivityFrame(data.Moment, data.Intensity, data.Steps, data.Mode, data.Runs);
                        frames.Add(frame);
                    }
                }
            }

            int ret = DB.AddActivityFrames(frames);
            Debug.WriteLine("[SyncData] Fragmento guardados en BD. numero: " + ret);
            return true;
        }
    
        private void resetSyncingProgress()
        {
            syncingProgress.Total = -1;
            syncingProgress.Current = 0;
        }

        #endregion

        private static string ByteArrayToHexViaLookup(byte[] bytes)
        {
            string[] hexStringTable = new string[] {
                "00", "01", "02", "03", "04", "05", "06", "07", "08", "09", "0A", "0B", "0C", "0D", "0E", "0F",
                "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "1A", "1B", "1C", "1D", "1E", "1F",
                "20", "21", "22", "23", "24", "25", "26", "27", "28", "29", "2A", "2B", "2C", "2D", "2E", "2F",
                "30", "31", "32", "33", "34", "35", "36", "37", "38", "39", "3A", "3B", "3C", "3D", "3E", "3F",
                "40", "41", "42", "43", "44", "45", "46", "47", "48", "49", "4A", "4B", "4C", "4D", "4E", "4F",
                "50", "51", "52", "53", "54", "55", "56", "57", "58", "59", "5A", "5B", "5C", "5D", "5E", "5F",
                "60", "61", "62", "63", "64", "65", "66", "67", "68", "69", "6A", "6B", "6C", "6D", "6E", "6F",
                "70", "71", "72", "73", "74", "75", "76", "77", "78", "79", "7A", "7B", "7C", "7D", "7E", "7F",
                "80", "81", "82", "83", "84", "85", "86", "87", "88", "89", "8A", "8B", "8C", "8D", "8E", "8F",
                "90", "91", "92", "93", "94", "95", "96", "97", "98", "99", "9A", "9B", "9C", "9D", "9E", "9F",
                "A0", "A1", "A2", "A3", "A4", "A5", "A6", "A7", "A8", "A9", "AA", "AB", "AC", "AD", "AE", "AF",
                "B0", "B1", "B2", "B3", "B4", "B5", "B6", "B7", "B8", "B9", "BA", "BB", "BC", "BD", "BE", "BF",
                "C0", "C1", "C2", "C3", "C4", "C5", "C6", "C7", "C8", "C9", "CA", "CB", "CC", "CD", "CE", "CF",
                "D0", "D1", "D2", "D3", "D4", "D5", "D6", "D7", "D8", "D9", "DA", "DB", "DC", "DD", "DE", "DF",
                "E0", "E1", "E2", "E3", "E4", "E5", "E6", "E7", "E8", "E9", "EA", "EB", "EC", "ED", "EE", "EF",
                "F0", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "FA", "FB", "FC", "FD", "FE", "FF",
            };
            StringBuilder result = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
            {
                result.Append(hexStringTable[b]);
            }
            return result.ToString();
        }

        public async Task<bool> SyncData()
        {
            Debug.WriteLine("[SyncData] Sincronizacion iniciada");
            await StopGetActivities();
            List<ActivityDataFragment> activities = await getActivities();
            bool ret = this.saveActivityToDB(activities);
            Debug.WriteLine("[SyncData] Sincronizacion finalizada");
            bool ret2 = ProcessData();
            if (ret2)
            {
                return true;
            }
            return false;
        }

        public bool ProcessData()
        {
            Debug.WriteLine("[ProcessData] Procesamiento iniciado");
            bool ret = processActivityFrames();
            Debug.WriteLine("[ProcessData] Procesamiento finalizado");
            return ret;
        }

        public async Task<bool> StopGetActivities()
        {
            Debug.WriteLine("[SyncData] Parando sincronizacion");
            resetStream();

            bool ret = await MiBand.Band.SyncStop();
            resetSyncingProgress();
            return ret;
        }

        public Sleep.SleepPeriod GetLastSleep()
        {
            // FIXME comprobar que esto es verdad para devolver el ultimo
            var periods = DB.GetSleepPeriods(DateTime.Today.AddDays(-1));
            var period = periods.Where(p => p.End.Date == DateTime.Today).FirstOrDefault();
            return period;
        }

        public Steps.StepsPeriod GetLastSteps()
        {
            // FIXME deberia devolver el dia de hoy o ayer?
            var periods = DB.GetStepsPeriods(DateTime.Today.AddDays(-1));
            var period = periods.FirstOrDefault();
            return period;
        }
    }

    
    public class ActivityData
    {
        // Cada ActivityData representa un minuto del dia

        // esto
        public int  Category  { get; set; }
        public int  Intensity { get; set; }
        public int  Steps     { get; set; }
        public byte Runs      { get; set; }
        public byte Mode      { get; set; }
        public DateTime Moment { get; set; }

        public override String ToString()
        {
            StringBuilder ret = new StringBuilder();
            /*ret.Append(Moment);
            ret.Append("\t").Append(Intensity);
            ret.Append("\t").Append(Steps);
            ret.Append("\t").Append(Category);
            ret.Append("\t").Append(Mode);
            ret.Append("\t").Append(Runs);*/

            
            ret.Append("ActivityData ["+Moment+"]");
            ret.Append("[intensity=").Append(Intensity);
            ret.Append(", steps=").Append(Steps);
            ret.Append(", category=").Append(Category);
            ret.Append(", mode=").Append(Mode);
            ret.Append(", runs=").Append(Runs);
            ret.Append("]");
            
            return ret.ToString();
        }

        public ActivityData(byte Intensity, byte Steps, byte Category, DateTime Moment)
        {
            this.Intensity = Intensity;
            this.Steps     = Steps;
            this.Category  = Category;
            this.Moment    = Moment;

            if (Category != 126)
            {
                this.Mode = (byte)(Category & 0x0F);
                if (Steps > 0)
                {
                    this.Runs = (byte)(Category >> 4);
                }
            }
            else
            {
                this.Mode = Category;
            }
        }
    }

    public class ActivityDataFragment
    {

        public List<ActivityData> Data { get; set; }
        public DateTime TimeStamp { get; set; }

        public override String ToString()
        {
            StringBuilder ret = new StringBuilder();
            ret.Append("[[ActivityDataFragment]]");
            ret.Append(" timestamp: ").Append(TimeStamp.Date.ToString());
            ret.Append(", size: ").Append(Data.Count);
            return ret.ToString();
        }

        public ActivityDataFragment(DateTime TimeStamp, List<ActivityData> Data)
        {
            this.TimeStamp = TimeStamp;
            this.Data = Data;
        }
    }

    class Progress
    {
        public int Current { get; set; }
        public int Total { get; set; }

        public int Percentage
        {
            get
            {
                int i = (100 * Current) / Total;
                return i;
            }
        }
    }

    public enum ActivityCategory
    {
        Idle = 0,
        Moving = 1,
        Moving2 = 2,
        Awake = 3, // lo intuyo por las posiciones
        SleepWithMovement = 4, // de estos hay menos que 5s
        SleepWithoutMovement = 5, // esto dicen en xda
        UnkRunning = 17, // esto se supone que es correr
        Unk2 = 33,
        NoData = 126
    }

    /*
     Date

    InBedMin
    DeepSleepMin
    LightSleepMin
    SleepStart
    SleepEnd
    AwakeMin

    DailyDistanceMeter
    DailyStepsMeter
    DailyBurnCalories

    WalkDistance
    WalkTimeMin
    WalkBurnCalories

    RunDistanceMeter
    RunTimeMin
    RunBurnCalories
    WalkRunSeconds


    DateUS
    Activity
    BedHour
    BedMinute
    AwakeHour
    AwakeMinute 
     */
}
