using MyBand.Entities.ActivityTracking.Sleep;
using MyBand.Entities.ActivityTracking.Steps;
using SQLite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace MyBand.Entities.Database
{
    public class DB
    {
        public static string TempDBCS = "tempdata.db";
        public static string PermDBCS = "activity.db";

        public static void Create()
        {
            {
                SQLiteConnection connection = new SQLiteConnection(TempDBCS);
                connection.CreateTable<ActivityFrame>();
            }
            {
                SQLiteConnection connection = new SQLiteConnection(PermDBCS);
                connection.CreateTable<SleepPeriod>();
                connection.CreateTable<StepsPeriod>();
                connection.CreateTable<SleepBlock>();
                connection.CreateTable<StepsBlock>();
            }
        }

        public static async void Delete()
        {
            if (await isFilePresent(TempDBCS))
            {
                StorageFile tempDB = await ApplicationData.Current.LocalFolder.GetFileAsync(TempDBCS);
                await tempDB.DeleteAsync();

            }
            if (await isFilePresent(PermDBCS))
            {
                StorageFile permDB = await ApplicationData.Current.LocalFolder.GetFileAsync(PermDBCS);
                await permDB.DeleteAsync();

            }
        }

        #region activity frame
        public static bool AddActivityFrame(DateTime timeStamp, int intensity, int steps, byte mode, byte runs)
        {
            using (SQLiteConnection connection = new SQLiteConnection(TempDBCS))
            {
                ActivityFrame af = new ActivityFrame()
                {
                    TimeStamp = timeStamp,
                    Intensity = intensity,
                    Steps = steps,
                    Mode = mode,
                    Runs = runs
                };

                int ret = connection.InsertOrReplace(af);
            }
            return true;
        }

        public static int AddActivityFrames(List<ActivityFrame> frames)
        {
            using (SQLiteConnection connection = new SQLiteConnection(TempDBCS))
            {
                int ret = connection.InsertOrReplaceAll(frames);
                return ret;
            }
        }

        public static List<ActivityFrame> GetActivityFrames(DateTime? From=null, DateTime? To=null)
        {

            List<ActivityFrame> list = null;

            using (SQLiteConnection connection = new SQLiteConnection(TempDBCS))
            {
                if (From == null && To == null)
                {
                    list = connection.Table<ActivityFrame>().ToList();
                }
                else
                {
                    if (From != null)
                    {
                        if (To != null)
                        {
                            list = connection.Table<ActivityFrame>()
                                .Where(af => af.TimeStamp >= From && af.TimeStamp <= To)
                                .OrderBy(af => af.TimeStamp).ToList();
                        }
                        else
                        {
                            list = connection.Table<ActivityFrame>()
                                .Where(af => af.TimeStamp >= From)
                                .OrderBy(af => af.TimeStamp).ToList();
                        }
                    }
                }
            }
            return list;

        }

        public static void UpdateActivityFrame(DateTime timeStamp, int intensity, int steps, byte mode, byte runs)
        {
            using (SQLiteConnection connection = new SQLiteConnection(TempDBCS))
            {
                ActivityFrame frame = connection.Table<ActivityFrame>().Where(af => af.TimeStamp.Equals(timeStamp)).FirstOrDefault();
                frame.Intensity = intensity;
                frame.Steps = steps;
                frame.Mode = mode;
                frame.Runs = runs;
                connection.Update(frame);
            }
        }

        public static void DeleteActivityFrame(DateTime timeStamp)
        {
            using (SQLiteConnection connection = new SQLiteConnection(TempDBCS))
            {
                ActivityFrame frame = connection.Table<ActivityFrame>().Where(af => af.TimeStamp.Equals(timeStamp)).FirstOrDefault();

                if (frame != null)
                {
                    connection.Delete(frame);
                }
            }
        }

        public static void DeleteActivityFrames(DateTime From, DateTime To, int? mode=null)
        {
            using (SQLiteConnection connection = new SQLiteConnection(TempDBCS))
            {
                TableQuery<ActivityFrame> query;

                if (mode != null)
                {
                    query = connection.Table<ActivityFrame>()
                                    .Where(af => af.TimeStamp >= From && af.TimeStamp <= To && af.Mode == mode)
                                    .OrderBy(af => af.TimeStamp);
                }
                else
                {
                    query = connection.Table<ActivityFrame>()
                                    .Where(af => af.TimeStamp >= From && af.TimeStamp <= To)
                                    .OrderBy(af => af.TimeStamp);
                }

                connection.BeginTransaction();
                foreach (var frame in query.ToList<ActivityFrame>())
                {
                    connection.Delete(frame);
                }
                connection.Commit();
            }
        }
        #endregion

        #region sleep period
        public static bool AddSleepPeriod(SleepPeriod period)
        {
            using (SQLiteConnection connection = new SQLiteConnection(PermDBCS))
            {
                int ret = connection.InsertOrReplace(period);
            }
            return true;
        }

        public static int AddSleepPeriods(List<SleepPeriod> periods)
        {
            using (SQLiteConnection connection = new SQLiteConnection(PermDBCS))
            {
                int ret = connection.InsertAll(periods);
                return ret;
            }
        }

        public static List<SleepPeriod> GetSleepPeriods(DateTime? From = null, DateTime? To = null)
        {
            List<SleepPeriod> list = null;

            using (SQLiteConnection connection = new SQLiteConnection(PermDBCS))
            {
                if (From == null && To == null)
                {
                    list = connection.Table<SleepPeriod>().ToList();
                }
                else
                {
                    if (From != null)
                    {
                        if (To != null)
                        {
                            list = connection.Table<SleepPeriod>()
                                .Where(pe => pe.End >= From && pe.End <= To)
                                .OrderBy(pe => pe.End).ToList();
                        }
                        else
                        {
                            list = connection.Table<SleepPeriod>()
                                .Where(pe => pe.End >= From)
                                .OrderBy(pe => pe.End).ToList();
                        }
                    }
                }
            }

            foreach (var period in list)
            {
                var blocks = GetSleepBlocks(period.ID);
                period.AssignBlocks(blocks);
            }

            return list;
        }

        public static void UpdateSleepPeriod(SleepPeriod period)
        {
            using (SQLiteConnection connection = new SQLiteConnection(PermDBCS))
            {
                SleepPeriod periodUpdated = connection.Table<SleepPeriod>().Where(pe => pe.ID.Equals(period.ID)).FirstOrDefault();
                periodUpdated.Start = period.Start;
                periodUpdated.End = period.End;
                periodUpdated.AwakeAt = period.AwakeAt;
                periodUpdated.DeepLength = period.DeepLength;
                periodUpdated.LightLength = period.LightLength;
                periodUpdated.TotalLength = period.TotalLength;
                connection.Update(periodUpdated);
            }

        }

        public static void DeleteSleepPeriod(int id)
        {
            using (SQLiteConnection connection = new SQLiteConnection(PermDBCS))
            {
                SleepPeriod period = connection.Table<SleepPeriod>().Where(pe => pe.ID.Equals(id)).FirstOrDefault();
                if (period != null)
                {
                    connection.BeginTransaction();
                    var blocks = connection.Table<SleepBlock>().Where(b => b.Period.Equals(id)).ToList<SleepBlock>();
                    foreach (var block in blocks)
                    {
                        connection.Delete(block);
                    }
                    connection.Delete(period);
                    connection.Commit();
                }
            }
        }
        #endregion

        #region steps period
        public static bool AddStepsPeriod(StepsPeriod period)
        {
            using (SQLiteConnection connection = new SQLiteConnection(PermDBCS))
            {
                int ret = connection.InsertOrReplace(period);
            }
            return true;
        }

        public static int AddStepsPeriods(List<StepsPeriod> periods)
        {
            using (SQLiteConnection connection = new SQLiteConnection(PermDBCS))
            {
                int ret = connection.InsertAll(periods);
                return ret;
            }
        }

        public static List<StepsPeriod> GetStepsPeriods(DateTime? From = null, DateTime? To = null)
        {

            List<StepsPeriod> list = null;

            using (SQLiteConnection connection = new SQLiteConnection(PermDBCS))
            {
                if (From == null && To == null)
                {
                    list = connection.Table<StepsPeriod>().ToList();
                }
                else
                {
                    if (From != null)
                    {
                        if (To != null)
                        {
                            list = connection.Table<StepsPeriod>()
                                .Where(pe => pe.Start >= From && pe.Start <= To)
                                .OrderBy(pe => pe.Start).ToList();
                        }
                        else
                        {
                            list = connection.Table<StepsPeriod>()
                                .Where(pe => pe.Start >= From)
                                .OrderBy(pe => pe.Start).ToList();
                        }
                    }
                }
            }

            foreach (var period in list)
            {
                var blocks = GetStepsBlocks(period.ID);
                period.AssignBlocks(blocks);
            }

            return list;
        }

        public static void UpdateStepsPeriod(StepsPeriod period)
        {
            using (SQLiteConnection connection = new SQLiteConnection(PermDBCS))
            {
                StepsPeriod entityToUpdate = connection.Table<StepsPeriod>().Where(pe => pe.ID.Equals(period.ID)).FirstOrDefault();
                entityToUpdate.Start = period.Start;
                entityToUpdate.End   = period.End;
                entityToUpdate.TotalLength = period.TotalLength;
                entityToUpdate.RunLength   = period.RunLength;
                entityToUpdate.WalkLength  = period.WalkLength;
                entityToUpdate.RunSteps    = period.RunSteps;
                entityToUpdate.WalkSteps   = period.WalkSteps;
                connection.Update(entityToUpdate);
            }
        }

        public static void DeleteStepsPeriod(int id)
        {
            using (SQLiteConnection connection = new SQLiteConnection(PermDBCS))
            {
                StepsPeriod period = connection.Table<StepsPeriod>().Where(pe => pe.ID.Equals(id)).FirstOrDefault();
                if (period != null)
                {
                    connection.BeginTransaction();
                    var blocks = connection.Table<StepsBlock>().Where(b => b.Period.Equals(id)).ToList<StepsBlock>();
                    foreach (var block in blocks)
                    {
                        connection.Delete(block);
                    }
                    connection.Delete(period);
                    connection.Commit();
                }
            }
        }
        #endregion

        #region sleep block
        public static bool AddSleepBlock(SleepBlock block)
        {
            using (SQLiteConnection connection = new SQLiteConnection(PermDBCS))
            {
                int ret = connection.InsertOrReplace(block);
            }
            return true;
        }

        public static int AddSleepBlocks(List<SleepBlock> blocks)
        {
            using (SQLiteConnection connection = new SQLiteConnection(PermDBCS))
            {
                int ret = connection.InsertAll(blocks);
                return ret;
            }
        }

        public static List<SleepBlock> GetSleepBlocks(int period)
        {
            List<SleepBlock> list = null;
            using (SQLiteConnection connection = new SQLiteConnection(PermDBCS))
            {
                list = connection.Table<SleepBlock>()
                                .Where(b => b.Period.Equals(period)).ToList<SleepBlock>();
            }

            return list;
        }

        public static void DeleteSleepBlocks(int period)
        {
            using (SQLiteConnection connection = new SQLiteConnection(PermDBCS))
            {
                connection.BeginTransaction();
                var blocks = connection.Table<SleepBlock>().Where(b => b.Period.Equals(period)).ToList<SleepBlock>();
                foreach (var block in blocks)
                {
                    connection.Delete(block);
                }
                connection.Delete(period);
                connection.Commit();
            }
        }
        #endregion

        #region steps block
        public static bool AddStepsBlock(StepsBlock block)
        {
            using (SQLiteConnection connection = new SQLiteConnection(PermDBCS))
            {
                int ret = connection.InsertOrReplace(block);
            }
            return true;
            return true;
        }

        public static int AddStepsBlocks(List<StepsBlock> blocks)
        {
            using (SQLiteConnection connection = new SQLiteConnection(PermDBCS))
            {
                int ret = connection.InsertAll(blocks);
                return ret;
            }
        }

        public static List<StepsBlock> GetStepsBlocks(int period)
        {

            List<StepsBlock> list = null;
            using (SQLiteConnection connection = new SQLiteConnection(PermDBCS))
            {
                list = connection.Table<StepsBlock>()
                                .Where(b => b.Period.Equals(period)).ToList<StepsBlock>();
            }

            return list;
        }

        public static void DeleteStepsBlocks(int period)
        {
            using (SQLiteConnection connection = new SQLiteConnection(PermDBCS))
            {
                connection.BeginTransaction();
                var blocks = connection.Table<StepsBlock>().Where(b => b.Period.Equals(period)).ToList<StepsBlock>();
                foreach (var block in blocks)
                {
                    connection.Delete(block);
                }
                connection.Delete(period);
                connection.Commit();
            }
        }
        #endregion

        public static async Task<bool> isFilePresent(string fileName)
        {
            #if WINDOWS_PHONE_APP
            bool exists = true;
            try
            {
                StorageFile storageFile = await ApplicationData.Current.LocalFolder.GetFileAsync(fileName);
            }
            catch(Exception) { exists = false; }

            return exists;
            #else
            var item = await ApplicationData.Current.LocalFolder.TryGetItemAsync(fileName);
            return item != null;
            #endif
        }

    }
}
