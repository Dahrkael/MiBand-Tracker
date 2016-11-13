using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using MyBand.Entities.ActivityTracking;

namespace MyBand.Entities
{
    class MiBand
    {
        public static MiBand Band = null;

        #region Constantes
        #pragma warning disable 0414
        public static String MAC_ADDRESS_FILTER = "88:0F:10";

        public  static Guid UUID_MILI_SERVICE 	     = new Guid("0000fee0-0000-1000-8000-00805f9b34fb");
        private static Guid UUID_CHAR_DEVICE_INFO 	 = GattCharacteristic.ConvertShortIdToUuid(0xFF01); // read
        private static Guid UUID_CHAR_DEVICE_NAME    = GattCharacteristic.ConvertShortIdToUuid(0xFF02); // read write
        private static Guid UUID_CHAR_NOTIFICATION   = GattCharacteristic.ConvertShortIdToUuid(0xFF03); // read notify
        private static Guid UUID_CHAR_USER_INFO      = GattCharacteristic.ConvertShortIdToUuid(0xFF04); // read write
        private static Guid UUID_CHAR_CONTROL_POINT  = GattCharacteristic.ConvertShortIdToUuid(0xFF05); // write
        private static Guid UUID_CHAR_REALTIME_STEPS = GattCharacteristic.ConvertShortIdToUuid(0xFF06); // read notify
        private static Guid UUID_CHAR_ACTIVITY_DATA  = GattCharacteristic.ConvertShortIdToUuid(0xFF07); // read indicate
        private static Guid UUID_CHAR_FIRMWARE_DATA  = GattCharacteristic.ConvertShortIdToUuid(0xFF08); // write without response
        private static Guid UUID_CHAR_LE_PARAMS      = GattCharacteristic.ConvertShortIdToUuid(0xFF09); // read write
        private static Guid UUID_CHAR_DATE_TIME      = GattCharacteristic.ConvertShortIdToUuid(0xFF0A); // read write
        private static Guid UUID_CHAR_STATISTICS     = GattCharacteristic.ConvertShortIdToUuid(0xFF0B); // read write
        private static Guid UUID_CHAR_BATTERY        = GattCharacteristic.ConvertShortIdToUuid(0xFF0C); // read notify
        private static Guid UUID_CHAR_TEST           = GattCharacteristic.ConvertShortIdToUuid(0xFF0D); // read write
        private static Guid UUID_CHAR_SENSOR_DATA    = GattCharacteristic.ConvertShortIdToUuid(0xFF0E); // read notify
        private static Guid UUID_CHAR_PAIR           = GattCharacteristic.ConvertShortIdToUuid(0xFF0F); // read write

        private static byte CP_NOTIFY_REALTIMESTEPS = 0x03;
        private static byte CP_SET_ALARM            = 0x04;
        private static byte CP_SET_GOAL             = 0x05;
        private static byte CP_FETCH_DATA           = 0x06;
        private static byte CP_SEND_FIRMWARE_INFO   = 0x07;
        private static byte CP_START_VIBRATION      = 0x08;
        private static byte CP_FACTORY_RESET        = 0x09;
        private static byte CP_CONFIRM_SYNC         = 0x0A;
        private static byte CP_SYNC                 = 0x0B;
        private static byte CP_REBOOT               = 0x0C;
        private static byte CP_SET_THEME            = 0x0E;
        private static byte CP_SET_WEAR_LOCATION    = 0x0F;
        private static byte CP_SET_REALTIMESTEPS    = 0x10;
        private static byte CP_STOP_SYNC            = 0x11;
        private static byte CP_NOTIFY_SENSORDATA    = 0x12;
        private static byte CP_STOP_VIBRATION       = 0x13;

        private static byte TEST_REMOTE_DISCONNECT     = 0x01;
        private static byte TEST_SELFTEST              = 0x02;
        private static byte TEST_NOTIFICATION          = 0x03;
        private static byte TEST_WRITE_MD5             = 0x04;
        private static byte TEST_DISCONNECTED_REMINDER = 0x05;
        #pragma warning restore 0414
        #endregion

        private Dictionary<Guid, GattCharacteristic> characteristics = new Dictionary<Guid, GattCharacteristic>();
        public ApplicationDataContainer Settings { get; set; }
        public bool Authenticated { get; set; }

        private bool ready = false;
        public static bool Ready 
        {
            get { return (Band != null) && Band.ready; }
            set { if (Band != null) { Band.ready = value; } }
        }

        private DeviceInformation device = null;
        BluetoothLEDevice bleDevice = null;
        private GattDeviceService service = null;
        private SensorData sensorData = null;
        private ActivityTracker activityTracker = null;

        public static async Task<MiBand> FirstMatch()
        {
            Debug.WriteLine("Buscando mibands");
            var devices = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(GattDeviceService.GetDeviceSelectorFromUuid(MiBand.UUID_MILI_SERVICE));
            if (devices.Count > 0)
            {
                // FIXME filtrar por mac?
                Debug.WriteLine("miband " + devices[0].Id + " seleccionada");
                MiBand band = new MiBand(devices[0]);
                await band.initialize();
                return band;
            }

            return null;
        }

        public MiBand(DeviceInformation Device)
        {
            activityTracker = new ActivityTracker();
            Authenticated = false;
            ready = false;
            Debug.WriteLine("[miband] objeto creado");
            this.device = Device;
            //bleDevice = await BluetoothLEDevice.FromIdAsync(device.Id);
            //bleDevice.ConnectionStatus == BluetoothConnectionStatus.Connected;
        }        
        ~MiBand()
        {
           TurnOffNotifications();
        }

        public async Task<String> getAddress()
        {
            #if WINDOWS_PHONE_APP
            BluetoothLEDevice bleDevice = await BluetoothLEDevice.FromIdAsync(device.Id);
            ulong maclong = bleDevice.BluetoothAddress;
            var tempMac = maclong.ToString("X");
            var mac = Regex.Replace(tempMac, "(.{2})(.{2})(.{2})(.{2})(.{2})(.{2})", "$1:$2:$3:$4:$5:$6");
            return mac;
            #endif
            #if WINDOWS_APP
            return "00:00:00:00:00:00";
            #endif
        }

        private String C2S(Guid Characteristic)
        {
            if (Characteristic == GattCharacteristic.ConvertShortIdToUuid(0xFF01))
            {
                return "UUID_CHAR_DEVICE_INFO";
            }
            else if (Characteristic == GattCharacteristic.ConvertShortIdToUuid(0xFF02))
            {
                return "UUID_CHAR_DEVICE_NAME";
            }
            else if (Characteristic == GattCharacteristic.ConvertShortIdToUuid(0xFF03))
            {
                return "UUID_CHAR_NOTIFICATION";
            }
            else if (Characteristic == GattCharacteristic.ConvertShortIdToUuid(0xFF04))
            {
                return "UUID_CHAR_USER_INFO";
            }
            else if (Characteristic == GattCharacteristic.ConvertShortIdToUuid(0xFF05))
            {
                return "UUID_CHAR_CONTROL_POINT";
            }
            else if (Characteristic == GattCharacteristic.ConvertShortIdToUuid(0xFF06))
            {
                return "UUID_CHAR_REALTIME_STEPS";
            }
            else if (Characteristic == GattCharacteristic.ConvertShortIdToUuid(0xFF07))
            {
                return "UUID_CHAR_ACTIVITY_DATA";
            }
            else if (Characteristic == GattCharacteristic.ConvertShortIdToUuid(0xFF08))
            {
                return "UUID_CHAR_FIRMWARE_DATA";
            }
            else if (Characteristic == GattCharacteristic.ConvertShortIdToUuid(0xFF09))
            {
                return " UUID_CHAR_LE_PARAMS";
            }
            else if (Characteristic == GattCharacteristic.ConvertShortIdToUuid(0xFF0A))
            {
                return "UUID_CHAR_DATE_TIME";
            }
            else if (Characteristic == GattCharacteristic.ConvertShortIdToUuid(0xFF0B))
            {
                return "UUID_CHAR_STATISTICS";
            }
            else if (Characteristic == GattCharacteristic.ConvertShortIdToUuid(0xFF0C))
            {
                return "UUID_CHAR_BATTERY";
            }
            else if (Characteristic == GattCharacteristic.ConvertShortIdToUuid(0xFF0D))
            {
                return "UUID_CHAR_TEST";
            }
            else if (Characteristic == GattCharacteristic.ConvertShortIdToUuid(0xFF0E))
            {
                return " UUID_CHAR_SENSOR_DATA";
            }
            else if (Characteristic == GattCharacteristic.ConvertShortIdToUuid(0xFF0F))
            {
                return "UUID_CHAR_PAIR";
            }
            else if (Characteristic == (new Guid("0000fee0-0000-1000-8000-00805f9b34fb")))
            {
                return "UUID_MILI_SERVICE";
            }
            return "CARACTERISTICA DESCONOCIDA";
        }

        #region metodos BLE
        // retornar el servicio principal de la pulsera
        private async Task getService()
        {
            Debug.WriteLine("[miband] accediendo a servicio");
            GattDeviceService _service;
            if (device != null)
            {
                _service = await GattDeviceService.FromIdAsync(device.Id);
                if (_service != null)
                {
                    this.service = _service;
                }
            }
        }
        // retornar la caracteristica necesaria
        private GattCharacteristic getCharacteristic(Guid Characteristic)
        {
            if (characteristics.ContainsKey(Characteristic))
            {
                Debug.WriteLine("[miband] caracteristica " + C2S(Characteristic));
                return characteristics[Characteristic];
            }
            GattCharacteristic _characteristic = service.GetCharacteristics(Characteristic)[0];
            if (_characteristic != null)
            {
                characteristics.Add(Characteristic, _characteristic);
                Debug.WriteLine("[miband] caracteristica " + C2S(Characteristic));
                return _characteristic;
            }
            return null;
        }
        // escribir datos a una caracteristica
        private async Task<bool> writeToCharacteristic(Guid Characteristic, IBuffer data)
        {
            Debug.WriteLine("[miband] escribiendo a caracteristica " + C2S(Characteristic));
            var _characteristic = this.getCharacteristic(Characteristic);
            try
            {
                await _characteristic.WriteValueAsync(data, GattWriteOption.WriteWithResponse);
            }
            catch(Exception)
            {
                Debug.WriteLine("[miband] escritura fallida");
                return false; 
            }
            Debug.WriteLine("[miband] escritura exitosa");
            return true;
        }
        // leer datos desde una caracteristica
        private async Task<Byte[]> readFromCharacteristic(Guid Characteristic)
        {
            Debug.WriteLine("[miband] leyendo desde caracteristica " + C2S(Characteristic));
            var _characteristic = this.getCharacteristic(Characteristic);
            // modo sin cachear, para que no pase como al principio, que parecia que leia siempre lo mismo y ni siquiera leia
            var value = (await _characteristic.ReadValueAsync(BluetoothCacheMode.Uncached)).Value;
            if (value != null)
            {
                Byte[] data = value.ToArray();
                Debug.WriteLine("[miband] caracteristica leida");
                return data;
            }
            Debug.WriteLine("[miband] error al leer de caracteristica");
            return null;
        }
        // activar notificaciones desde una caracteristica
        private async Task<bool> TurnOffNotification(Guid Characteristic)
        {
            Debug.WriteLine("[miband] desactivando notificacion");
            var _characteristic = getCharacteristic(Characteristic);
            if (_characteristic != null)
            {
                var ret = await _characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
                if (ret == GattCommunicationStatus.Success)
                {
                    Debug.WriteLine("[miband] notificacion desactivada");
                    return true;
                }
            }
            Debug.WriteLine("[miband] error al activar notificacion");
            return false;
        }
        private async Task<bool> TurnOnNotification(Guid Characteristic, TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs> Callback)
        {
            Debug.WriteLine("[miband] activando notificacion");
            var _characteristic = getCharacteristic(Characteristic);
            if (_characteristic != null)
            {
                GattClientCharacteristicConfigurationDescriptorValue type = GattClientCharacteristicConfigurationDescriptorValue.None;
                if (((int)_characteristic.CharacteristicProperties & (int)GattCharacteristicProperties.Notify) != 0)
                {
                    type =GattClientCharacteristicConfigurationDescriptorValue.Notify;
                }
                if (((int)_characteristic.CharacteristicProperties & (int)GattCharacteristicProperties.Indicate) != 0)
                {
                    type = GattClientCharacteristicConfigurationDescriptorValue.Indicate;
                }
                _characteristic.ValueChanged += Callback;
                try
                {
                    var ret = await _characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(type);
                    if (ret == GattCommunicationStatus.Success)
                    {
                        Debug.WriteLine("[miband] notificacion activada");
                        return true;
                    }
                }
                catch(Exception)
                {
                    
                }
            }
            Debug.WriteLine("[miband] error al activar notificacion");
            return false;
        }
        private async Task<bool> TurnOffNotifications()
        {
            Debug.WriteLine("[miband] desactivando notificaciones");
            bool ret1 = await TurnOffNotification(MiBand.UUID_CHAR_NOTIFICATION);
            if (ret1) { this.RealTimeStepsNotifications(false); }
            bool ret2 = await TurnOffNotification(MiBand.UUID_CHAR_REALTIME_STEPS);
            bool ret3 = await TurnOffNotification(MiBand.UUID_CHAR_BATTERY);
            //bool ret4 = await TurnOffNotification(MiBand.UUID_CHAR_ACTIVITY_DATA);
            //bool ret5 = await TurnOffNotification(MiBand.UUID_CHAR_SENSOR_DATA);
            return (ret1 && ret2 && ret3);// && ret4 && ret5);
        }
        public async Task<int> TurnOnNotifications()
        {
            Debug.WriteLine("[miband] activando notificaciones");
            bool ret1 = await TurnOnNotification(MiBand.UUID_CHAR_NOTIFICATION, notificationValueChanged);
            if (ret1) { this.RealTimeStepsNotifications(true); }
            bool ret2 = await TurnOnNotification(MiBand.UUID_CHAR_REALTIME_STEPS, realtimeStepsValueChanged);
            bool ret3 = await TurnOnNotification(MiBand.UUID_CHAR_BATTERY, batteryValueChanged);
            bool ret4 = await TurnOnNotification(MiBand.UUID_CHAR_ACTIVITY_DATA,  activityDataValueChanged);
            //bool ret5 = await TurnOnNotification(MiBand.UUID_CHAR_SENSOR_DATA,    sensorDataValueChanged);
            int ret = 0x00;
            if (ret1) { ret = ret | 0x01; }
            if (ret2) { ret = ret | 0x02; }
            if (ret3) { ret = ret | 0x04; }
            if (ret4) { ret = ret | 0x08; }
            //if (ret5) { ret = ret | 0x10; }
            return ret;
        }

        private async Task<bool> initialize()
        {
            Debug.WriteLine("[miband] iniciando");
            await this.getService();
            getCharacteristic(UUID_CHAR_DEVICE_INFO);
            getCharacteristic(UUID_CHAR_DEVICE_NAME);
            getCharacteristic(UUID_CHAR_NOTIFICATION);
            getCharacteristic(UUID_CHAR_USER_INFO);
            getCharacteristic(UUID_CHAR_CONTROL_POINT);
            getCharacteristic(UUID_CHAR_REALTIME_STEPS);
            getCharacteristic(UUID_CHAR_ACTIVITY_DATA);
            getCharacteristic(UUID_CHAR_LE_PARAMS);
            getCharacteristic(UUID_CHAR_DATE_TIME);
            getCharacteristic(UUID_CHAR_STATISTICS);
            getCharacteristic(UUID_CHAR_BATTERY);
            getCharacteristic(UUID_CHAR_TEST);
            getCharacteristic(UUID_CHAR_SENSOR_DATA);
            getCharacteristic(UUID_CHAR_PAIR);
            Debug.WriteLine("[miband] todas las caracteristicas activas");
            //await TurnOnNotifications();
            return true;
        }
        #endregion

        #region metodos Setup&Settings
        public bool SettingSet(string Setting, object Data)
        {
            if (Settings != null)
            {
                Settings.Values[Setting] = Data;
                return true;
            }
            return false;
        }

        public object SettingGet(string Setting)
        {
            if (Settings != null)
            {
                return Settings.Values[Setting];
            }
            return null;
        }

        public async Task<bool> SetUnconfigurables()
        {
            bool ret = true;
            ret = ret & (await this.SetGoal((int)SettingGet("DailyGoal")));
            ret = ret & (await this.SetAlarm(Alarm.FromSetting((ApplicationDataCompositeValue)SettingGet("Alarm1"))));
            ret = ret & (await this.SetAlarm(Alarm.FromSetting((ApplicationDataCompositeValue)SettingGet("Alarm2"))));
            ret = ret & (await this.SetAlarm(Alarm.FromSetting((ApplicationDataCompositeValue)SettingGet("Alarm3"))));
            ret = ret & (await this.SetWearLocation((WearLocation)SettingGet("WearLocation")));
            ret = ret & (await this.SetColorTheme(ColorTheme.FromInt32((int)SettingGet("ColorTheme")), true));
            return ret;
        }

        public async Task<bool> ConfigureAsSettings()
        {
            bool ret = true;
            ret = ret & (await this.SetUserInfo(UserInfo.FromSetting((ApplicationDataCompositeValue)SettingGet("UserInfo")), DataMode.Normal));
            ret = ret & (await this.SetDateTime(DateTime.Now));
            ret = ret & (await this.SetName((string)SettingGet("Name")));
            ret = ret & (await this.SetGoal((int)SettingGet("DailyGoal")));
            ret = ret & (await this.SetCurrentSteps((int)SettingGet("CurrentSteps")));
            ret = ret & (await this.SetAlarm(Alarm.FromSetting((ApplicationDataCompositeValue)SettingGet("Alarm1"))));
            ret = ret & (await this.SetAlarm(Alarm.FromSetting((ApplicationDataCompositeValue)SettingGet("Alarm2"))));
            ret = ret & (await this.SetAlarm(Alarm.FromSetting((ApplicationDataCompositeValue)SettingGet("Alarm3"))));
            ret = ret & (await this.SetWearLocation((WearLocation)SettingGet("WearLocation")));
            ret = ret & (await this.SetColorTheme(ColorTheme.FromInt32((int)SettingGet("ColorTheme")), true));
            return ret;
        }
        public async Task<bool> CreateSettings()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            // si ya existe se borra
            if (localSettings.Containers.ContainsKey("MiBand"))
            {
                localSettings.DeleteContainer("MiBand");
            }
            // se crea nuevo
            var settings = localSettings.CreateContainer("MiBand", ApplicationDataCreateDisposition.Always);
            // si por lo que sea no se crea salimos
            if (!localSettings.Containers.ContainsKey("MiBand"))
            {
                return false;
            }
            Settings = settings;

            // creamos todas las claves 
            settings.Values["Name"]         = "MI";
            settings.Values["MAC"]          = await getAddress();
            settings.Values["BatteryInfo"]  = (new BatteryInfo(new byte[] {})).ToSetting();
            settings.Values["DailyGoal"]    = 8000;
            settings.Values["CurrentSteps"] = 0;
            settings.Values["Alarm1"]       = (new Alarm(0, false, DateTime.Today, Alarm.Everyday, false)).ToSetting();
            settings.Values["Alarm2"]       = (new Alarm(1, false, DateTime.Today, Alarm.Everyday, false)).ToSetting();
            settings.Values["Alarm3"]       = (new Alarm(2, false, DateTime.Today, Alarm.Everyday, false)).ToSetting();
            settings.Values["ColorTheme"]   = ColorTheme.Aqua.ToInt32();
            settings.Values["WearLocation"] = (int)WearLocation.LeftHand;
            settings.Values["DeviceInfo"]   = (new DeviceInfo(new byte[] {})).ToSetting();
            settings.Values["UserInfo"]     = (new UserInfo(true, 18, 180, 65, 0x01, "usuario 1")).ToSetting();

            return true;
        }

        public decimal CurrentDistance()
        {
            //retorna kilometros
            // FIXME esto hay que cachearlo
            UserInfo userInfo = UserInfo.FromSetting((ApplicationDataCompositeValue)SettingGet("UserInfo"));
            int steps = (int)SettingGet("CurrentSteps");
            if (userInfo.Male)
            {
                return steps * 76.2m / 100000;
            }
            else
            {
                return steps * 66.0m / 100000;
            }
        }

        public decimal CurrentCalories()
        {
            // FIXME esto hay que cachearlo
            UserInfo userInfo = UserInfo.FromSetting((ApplicationDataCompositeValue)SettingGet("UserInfo"));
            decimal distance = CurrentDistance();
            var calories = distance * userInfo.Weight * 0.73m;
            return calories;
        }
        #endregion

        // parece que la pulsera no responde hasta que no le mandas los datos del usuario y la fecha
        // o eso intuyo de la app oficial aunque ahi no se usa y los chascos iniciales
        public async Task<bool> Authenticate()
        {
            Debug.WriteLine("[miband] autenticando");
            UserInfo userInfo = UserInfo.FromSetting((ApplicationDataCompositeValue)SettingGet("UserInfo"));
            if (userInfo != null)
            {
                bool ret = await this.SetUserInfo(userInfo, DataMode.Normal);
                if (ret)
                {
                    DateTime now = DateTime.Now;
                    ret = await this.SetDateTime(now);
                    if (ret)
                    {
                        DateTime bandDate = await this.GetDateTime();
                        if (bandDate != now)
                        {
                            Debug.WriteLine("[miband] La hora de la pulsera difiere: " + now.ToString() + " vs " + bandDate.ToString());
                        }
                        Debug.WriteLine("[miband] autenticacion exitosa");
                        Authenticated = true;
                        return true;
                    }
                }
            }
            Debug.WriteLine("[miband] error de autenticacion");
            return false;
        }

        private static int getVersionCodeFromFwData(Byte[] data)
        {
            return (0xff & data[1059]) << 24 | (0xff & data[1058]) << 16 | (0xff & data[1057]) << 8 | 0xff & data[1056];
        }

        private static int getVersionCodeFromFwStream(Stream stream)
        {
            byte[] data = new byte[4];
            stream.Seek(1056, SeekOrigin.Begin);
            stream.Read(data, 0, 4);
            return (0xff & data[3]) << 24 | (0xff & data[2]) << 16 | (0xff & data[1]) << 8 | 0xff & data[0];
        }

        #region metodos de la pulsera
        public async Task<String> GetName()
        {
            Debug.WriteLine("[miband] recibiendo nombre");
            var deviceNameBytes = await readFromCharacteristic(MiBand.UUID_CHAR_DEVICE_NAME);
            if (deviceNameBytes != null)
            {
                var deviceName = Encoding.UTF8.GetString(deviceNameBytes, 0, deviceNameBytes.Length).Replace("\0`\t", "");
                SettingSet("Name", deviceName);
                Debug.WriteLine("[miband] nombre recibido");
                return deviceName;
            }
            Debug.WriteLine("[miband] error al recibir el nombre");
            return "";
        }
        public async Task<bool> SetName(String NewName)
        {
            Debug.WriteLine("[miband] escribiendo nombre");
            if (NewName.Length == 0) { return false; }
            var bytes = Encoding.UTF8.GetBytes(NewName);
            bool ret = await writeToCharacteristic(MiBand.UUID_CHAR_DEVICE_NAME, bytes.AsBuffer());
            if (ret) // FIXME esto va a dar error
            {
                SettingSet("Name", NewName);
            }
            Debug.WriteLine("[miband] nombre cambiado");
            return ret;
        }
        // imagino que esto es para que sea visible o no a traves de bluetooth para otros
        public async Task<bool> EnableConnectedBroadcast(bool Enable)
        {
            Byte[] buffer = new Byte[] { (byte)(Enable ? 0x01 : 0x00) };
            bool ret = await writeToCharacteristic(MiBand.UUID_CHAR_DEVICE_NAME, buffer.AsBuffer());
            return ret;
        }

        public async Task<BatteryInfo> GetBatteryInfo()
        {
            Debug.WriteLine("[miband] recibiendo bateria");
            Byte[] batteryInfoBytes = new Byte[1];
            // skip if received not exactly 10 bytes
            while (batteryInfoBytes.Length != 10)
            {
                batteryInfoBytes = await readFromCharacteristic(MiBand.UUID_CHAR_BATTERY);
            }
            BatteryInfo info = new BatteryInfo(batteryInfoBytes);
            if (info.Valid)
            {
                SettingSet("BatteryInfo", info.ToSetting());
            }
            Debug.WriteLine("[miband] bateria recibida");
            return info;
        }

        public async Task<bool> RealTimeStepsNotifications(bool Enable)
        {
            Byte[] buffer = new Byte[] { MiBand.CP_NOTIFY_REALTIMESTEPS, (byte)(Enable ? 0x01 : 0x00) };
            bool ret = await writeToCharacteristic(MiBand.UUID_CHAR_CONTROL_POINT, buffer.AsBuffer());
            return ret;
        }

        private async Task<bool> SensorDataNotifications(bool Enable)
        {
            Byte[] buffer = new Byte[] { MiBand.CP_NOTIFY_SENSORDATA, (byte)(Enable ? 0x01 : 0x00) };
            bool ret = await writeToCharacteristic(MiBand.UUID_CHAR_CONTROL_POINT, buffer.AsBuffer());
            return ret;
        }

        public async Task<bool> EnableGetSensorData(bool Enable)
        {
            bool ret;
            if (Enable)
            {
                this.sensorData = new SensorData();
                ret = await SensorDataNotifications(true);
            }
            else
            {
                ret = await SensorDataNotifications(false);
                this.sensorData = null;
            }   
                return ret;
        }

        public async Task<bool> SetAlarm(Alarm Alarm)
        {
            Debug.WriteLine("[miband] escribiendo alarma");
            if (!Alarm.Valid) { return false; }
            Byte[] buffer = new Byte[] { MiBand.CP_SET_ALARM, Alarm.ID, (byte)(Alarm.Enabled ? 1 : 0), 
                                        (byte)(Alarm.When.Year - 2000), (byte)Alarm.When.Month, (byte)Alarm.When.Day, 
                                        (byte)Alarm.When.Hour, (byte)Alarm.When.Minute, (byte)Alarm.When.Second, 
                                        (byte)(Alarm.Smart ? 30 : 0), Alarm.Repeat };

            bool ret = await writeToCharacteristic(MiBand.UUID_CHAR_CONTROL_POINT, buffer.AsBuffer());
            if (ret)
            {
                Debug.WriteLine("[miband] alarma escrita");
                SettingSet("Alarm" + (Alarm.ID+1), Alarm.ToSetting());
            }
            return ret;
        }

        public async void StartVibration(byte Type)
        {
            // el 0 vibra dos veces con todos los leds (color del tema)
            // el 1 vibra ¿sin parar? y deja el led central fijo (azul)
            // el 2 en adelante no parece que haga nada
            Byte[] buffer = new Byte[] { MiBand.CP_START_VIBRATION, Type };
            await writeToCharacteristic(MiBand.UUID_CHAR_CONTROL_POINT, buffer.AsBuffer());
        }
        public async void StopVibration()
        {
            Byte[] buffer = new Byte[] { MiBand.CP_STOP_VIBRATION};
            await writeToCharacteristic(MiBand.UUID_CHAR_CONTROL_POINT, buffer.AsBuffer());
        }

        public async Task<bool> SetColorTheme(ColorTheme Theme, bool Advertise)
        {
            Debug.WriteLine("[miband] cambiando esquema de color");
            Byte[] buffer = new Byte[] { MiBand.CP_SET_THEME, Theme.R, Theme.G, Theme.B, (byte)(Advertise ? 0x01 : 0x00) }; 
            bool ret = await writeToCharacteristic(MiBand.UUID_CHAR_CONTROL_POINT, buffer.AsBuffer());
            if (ret)
            {
                SettingSet("ColorTheme", Theme.ToInt32());
                Debug.WriteLine("[miband] esquema de color cambiado");
            }
            else
            {
                Debug.WriteLine("[miband] error al cambiar esquema de color");
            }
            return ret;
        }

        public async Task<int> GetCurrentSteps()
        {
            Debug.WriteLine("[miband] recibiendo pasos actuales");
            var buffer = await readFromCharacteristic(MiBand.UUID_CHAR_REALTIME_STEPS);
            Debug.WriteLine("[miband] pasos actuales recibidos");
            int steps = buffer[0] + (buffer[1] << 8);
            SettingSet("CurrentSteps", steps);
            return (steps);
        }
        public async Task<bool> SetCurrentSteps(int Steps)
        {
            Byte[] buffer = new Byte[] { MiBand.CP_SET_REALTIMESTEPS, (byte)(Steps & 0xFF), (byte)((Steps >> 8) & 0xFF)};
            bool ret = await writeToCharacteristic(MiBand.UUID_CHAR_CONTROL_POINT, buffer.AsBuffer());
            if (ret)
            {
                SettingSet("CurrentSteps", Steps);
            }
            return ret;
        }

        public async Task<bool> SetGoal(int Steps)
        {
            Debug.WriteLine("[miband] escribiendo objetivo");
            // ToDo el segundo no se que es, pero puede variar
            Byte[] buffer = new Byte[] { MiBand.CP_SET_GOAL, 0x00, (byte)(Steps & 0xFF), (byte)((Steps >> 8) & 0xFF)};
            bool ret = await writeToCharacteristic(MiBand.UUID_CHAR_CONTROL_POINT, buffer.AsBuffer());
            if (ret)
            {
                SettingSet("DailyGoal", Steps);
                Debug.WriteLine("[miband] objetivo escrito");
            }
            return ret;
        }

        public async Task<bool> SetUserInfo(UserInfo UserInfo, DataMode DataMode)
        {
            Debug.WriteLine("[miband] escribiendo info de usuario");
            string mac = await this.getAddress();
            bool ret = await writeToCharacteristic(MiBand.UUID_CHAR_USER_INFO, UserInfo.AsBuffer(mac, DataMode));
            if (ret)
            {
                SettingSet("UserInfo", UserInfo.ToSetting());
            }
            return ret;
        }

        public async Task<UserInfo> GetUserInfo()
        {
            Debug.WriteLine("[miband] recibiendo info de usuario");
            string mac = await this.getAddress();
            var buffer = await readFromCharacteristic(MiBand.UUID_CHAR_USER_INFO);
            if (buffer == null || buffer.Length == 0)
            { return null; }
            Debug.WriteLine("[miband] info de usuario recibida");
            UserInfo info = UserInfo.FromBuffer(buffer, mac);
            SettingSet("UserInfo", info.ToSetting());
            return info;
        }

        public async Task<bool> Pair()
        {
            Debug.WriteLine("[miband] emparejando");
            await writeToCharacteristic(MiBand.UUID_CHAR_PAIR, (new Byte[] { 0x02 }).AsBuffer());

            for (int i = 0; i < 5; i++)
            {
                try
                {
                    Byte[] buffer = await readFromCharacteristic(MiBand.UUID_CHAR_PAIR);
                    if (buffer[0] == 0x02)
                    {
                        Debug.WriteLine("[miband] emparejamiento exitoso");
                        return true;
                    }
                }
                catch (Exception)
                {
                    // FIXME
                }
            }
            return false;
        }

        public async Task<DateTime> GetDateTime()
        {
            Debug.WriteLine("[miband] recibiendo fecha");
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    var buffer = await readFromCharacteristic(MiBand.UUID_CHAR_DATE_TIME);
                    if (buffer != null)
                    {
                        Debug.WriteLine("[miband] fecha recibida");
                        DateTime date = new DateTime(buffer[0] + 2000, buffer[1] + 1, buffer[2], buffer[3], buffer[4], buffer[5]);
                        return date;
                    }
                }
                catch (Exception) { }
            }
            Debug.WriteLine("[miband] error al recibir fecha");
            return DateTime.MinValue;
        }
        public async Task<bool> SetDateTime(DateTime Date)
        {
            Debug.WriteLine("[miband] escribiendo fecha");
            Byte[] buffer = new Byte[] { (byte)((Date.Year - 2000) & 0xFF), (byte)(Date.Month - 1),  (byte)Date.Day, (byte)Date.Hour, (byte)Date.Minute, (byte)Date.Second};
            return await writeToCharacteristic(MiBand.UUID_CHAR_DATE_TIME, buffer.AsBuffer());
        }

        public async Task<LEParams> GetLEParams()
        {
            Byte[] buffer = await readFromCharacteristic(MiBand.UUID_CHAR_LE_PARAMS);
            LEParams leParams = new LEParams(buffer);
            if (leParams.Valid)
            {
                return leParams;
            }
            return null;
        }
        /*public async void SetLEParams()
        {
            // ToDo
        } */

        public async Task<DeviceInfo> GetDeviceInfo()
        {
            Byte[] buffer = await readFromCharacteristic(MiBand.UUID_CHAR_DEVICE_INFO);
            DeviceInfo deviceInfo = new DeviceInfo(buffer);
            SettingSet("DeviceInfo", deviceInfo.ToSetting());
            return deviceInfo;
        }

        public async void GetUsage()
        {
            Byte[] buffer = await readFromCharacteristic(MiBand.UUID_CHAR_STATISTICS);
            if (buffer.Length == 20)
            {
                var param1 = BitConverter.ToInt32(buffer, 0) / 1.6;
                var param2 = BitConverter.ToInt32(buffer, 4);
                var param3 = BitConverter.ToInt32(buffer, 8);
                var param4 = BitConverter.ToInt32(buffer, 12);
                var param5 = BitConverter.ToInt32(buffer, 16);
                // FIXME terminar
            }
        }

        public async void FactoryReset()
        {
            Byte[] buffer = new Byte[] { MiBand.CP_FACTORY_RESET };
            await writeToCharacteristic(MiBand.UUID_CHAR_CONTROL_POINT, buffer.AsBuffer());
        }

        public async void Reboot()
        {
            Byte[] buffer = new Byte[] { MiBand.CP_REBOOT };
            await writeToCharacteristic(MiBand.UUID_CHAR_CONTROL_POINT, buffer.AsBuffer());
        }

        public async void RemoteDisconnect()
        {
            Byte[] buffer = new Byte[] { MiBand.TEST_REMOTE_DISCONNECT };
            await writeToCharacteristic(MiBand.UUID_CHAR_TEST, buffer.AsBuffer());
        }

        public async void SelfTest()
        {
            Byte[] buffer = new Byte[] { MiBand.TEST_SELFTEST, 0x02 };
            await writeToCharacteristic(MiBand.UUID_CHAR_TEST, buffer.AsBuffer());
        }        

        public async Task<bool> SetWearLocation(WearLocation Location)
        {
            Byte[] buffer = new Byte[] { MiBand.CP_SET_WEAR_LOCATION, (byte)Location};
            bool ret = await writeToCharacteristic(MiBand.UUID_CHAR_CONTROL_POINT, buffer.AsBuffer());
            if (ret)
            {
                SettingSet("WearLocation", (int)Location);
            }
            return ret;
        }

        public async Task<bool> FetchData()
        {
            Byte[] buffer = new Byte[] { MiBand.CP_FETCH_DATA };
            bool ret = await writeToCharacteristic(MiBand.UUID_CHAR_CONTROL_POINT, buffer.AsBuffer());
            return ret;
        }

        public async Task<bool> SyncStart()
        {
            Byte[] buffer = new Byte[] { MiBand.CP_SYNC};
            bool ret = await writeToCharacteristic(MiBand.UUID_CHAR_CONTROL_POINT, buffer.AsBuffer());
            return ret;
        }

        public async Task<bool> SyncStop()
        {
            Byte[] buffer = new Byte[] { MiBand.CP_STOP_SYNC};
            bool ret = await writeToCharacteristic(MiBand.UUID_CHAR_CONTROL_POINT, buffer.AsBuffer());
            return ret;
        }

        public async Task<bool> SyncConfirm(DateTime timeStamp, int rawLength)
        {
            Byte[] buffer = new Byte[9] { MiBand.CP_CONFIRM_SYNC, 
                                          (byte)(-2000 + timeStamp.Year), (byte)(timeStamp.Month - 1),  (byte)timeStamp.Day,
                                          (byte)timeStamp.Hour,           (byte)timeStamp.Minute, (byte)timeStamp.Second, 
                                          (byte)(rawLength & 0xFF),       (byte)((rawLength >> 8) & 0xFF)};
            bool ret = await writeToCharacteristic(MiBand.UUID_CHAR_CONTROL_POINT, buffer.AsBuffer());
            return ret;
        }

        /*
        public async void WriteMD5()
        {
            int i = 0;
            boolean flag;
            byte abyte1[];
            if(abyte0.length == 16)
                flag = true;
            else
                flag = false;
            v.a(flag);
            abyte1 = new byte[17];
            abyte1[0] = 4;
            for(; i < 16; i++)
                abyte1[i + 1] = abyte0[i];

            return write(m_CharTest, abyte1);
            
        } // este no se pa que es

        public async void UpdateFirmware(int i, int j, int k, byte[] abyte0)
        {
            sendFirmwareInfo(i, j, abyte0.Length, k);
            sendFirmwareData(abyte0);
        } // pendiente

        private async void sendFirmwareData(byte[] abyte0)
        {
    /*
            int i;
            int j;
            int k;
            int l;
            v.d();
            i = abyte0.length;
            j = i / 20;
            v.a((new StringBuilder()).append("totalPackets = ").append(j).toString());
            m_FirmwareUpdatingProgress.total = i;
            m_FirmwareUpdatingProgress.progress = 0;
            k = 0;
            l = 0;
    _L5:
            if(k >= j) goto _L2; else goto _L1
    _L1:
            boolean flag1;
            byte abyte2[] = new byte[20];
            for(int k1 = 0; k1 < 20; k1++)
                abyte2[k1] = abyte0[k1 + k * 20];

            flag1 = write(m_CharFirmwareData, abyte2);
            v.a(flag1);
            if(flag1) goto _L4; else goto _L3
    _L3:
            return false;
    _L4:
            IMiLiProfile.Progress progress1 = m_FirmwareUpdatingProgress;
            progress1.progress = 20 + progress1.progress;
            int l1 = l + 20;
            v.a((new StringBuilder()).append("transferedPackets = ").append(l1).toString());
            if(l1 >= 1000)
            {
                _sync();
                l1 = 0;
            }
            k++;
            l = l1;
              goto _L5
    _L2:
            if(i % 20 == 0)
            {
                _sync();
                v.a("transferFirmwareData: complete");
                return true;
            }
            byte abyte1[] = new byte[i % 20];
            for(int i1 = 0; i1 < i % 20; i1++)
                abyte1[i1] = abyte0[i1 + j * 20];

            boolean flag = write(m_CharFirmwareData, abyte1);
            v.a(flag);
            if(flag)
            {
                IMiLiProfile.Progress progress = m_FirmwareUpdatingProgress;
                progress.progress = progress.progress + i % 20;
                int j1 = l + i % 20;
                v.a((new StringBuilder()).append("transferedPackets = ").append(j1).toString());
                _sync();
                v.a("transferFirmwareData: complete");
                return true;
            }
              goto _L3
             
    } // pendiente

        private async void sendFirmwareInfo(int i, int j, int k, int l)
        {
            
            v.d();
            BluetoothGattCharacteristic bluetoothgattcharacteristic = m_CharControlPoint;
            byte abyte0[] = new byte[13];
            abyte0[0] = 7;
            abyte0[1] = (byte)i;
            abyte0[2] = (byte)(i >> 8);
            abyte0[3] = (byte)(i >> 16);
            abyte0[4] = (byte)(i >> 24);
            abyte0[5] = (byte)j;
            abyte0[6] = (byte)(j >> 8);
            abyte0[7] = (byte)(j >> 16);
            abyte0[8] = (byte)(j >> 24);
            abyte0[9] = (byte)k;
            abyte0[10] = (byte)(k >> 8);
            abyte0[11] = (byte)l;
            abyte0[12] = (byte)(l >> 8);
            return write(bluetoothgattcharacteristic, abyte0);
             
        } // pendiente
        */
        #endregion

        public async Task<bool> SyncData()
        {
            return await activityTracker.SyncData();
        }

        #region notificaciones
        public Action<BatteryInfo> onBatteryCallback = null;
        void batteryValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            if (args.CharacteristicValue == null) { return; }
            byte[] buffer = new byte[args.CharacteristicValue.Length];
            DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(buffer);

            BatteryInfo info = new BatteryInfo(buffer);
            if (info.Valid)
            {
                SettingSet("BatteryInfo", info.ToSetting());
                if (onBatteryCallback != null)
                {
                    onBatteryCallback(info);
                }
            }
        }
        public Action<int> onRealtimeStepsCallback = null;
        void realtimeStepsValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            if (args.CharacteristicValue == null) { return; }
            byte[] buffer = new byte[args.CharacteristicValue.Length];
            DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(buffer);

            int steps = -1;
            if (buffer.Length == 2)
            {
                steps = (0xFF & buffer[0]) | ((0xFF & buffer[1]) << 8);
            }
            if (buffer.Length == 4)
            {
                steps = (0xFF & buffer[0]) | ((0xFF & buffer[1]) << 8) | ((0xFF & buffer[2]) << 16) | ((0xFF & buffer[3]) << 24);
            }
            if (steps != -1)
            {
                SettingSet("CurrentSteps", steps);
                if (onRealtimeStepsCallback != null)
                {
                    onRealtimeStepsCallback(steps);
                }
            }
        }
        public Action<byte> onNotificationCallback = null;
        void notificationValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            if (args.CharacteristicValue == null) { return; }
            byte[] buffer = new byte[args.CharacteristicValue.Length];
            DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(buffer);

            if (buffer.Length == 1)
            {
                byte status = buffer[0];
                if (onNotificationCallback != null)
                {
                    onNotificationCallback(status);
                }
            }
        }

        public Action onActivityDataCallback = null;
        void activityDataValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            if (args.CharacteristicValue == null) { return; }
            byte[] buffer = new byte[args.CharacteristicValue.Length];
            DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(buffer);

            if (activityTracker != null)
            {
                activityTracker.Write(buffer, buffer.Length);
            }
            if (onActivityDataCallback != null)
            {
                onActivityDataCallback();
            }
        }
        public Action onSensorDataCallback = null;
        void sensorDataValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            if (args.CharacteristicValue == null) { return; }
            byte[] buffer = new byte[args.CharacteristicValue.Length];
            DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(buffer);

            if (sensorData != null)
            {
                this.sensorData.NewPacket(buffer);
            }
            else
            {
                Debugger.Break();
            }

            if (onSensorDataCallback != null)
            {
                onSensorDataCallback();
            }
        }
        #endregion
    }
}
