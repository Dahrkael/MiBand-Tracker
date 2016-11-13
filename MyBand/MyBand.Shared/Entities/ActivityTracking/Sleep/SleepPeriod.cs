using MyBand.Entities.Database;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using SQLite;

namespace MyBand.Entities.ActivityTracking.Sleep
{
    [Table("sleep_periods")]
    public class SleepPeriod
    {
        [PrimaryKey, AutoIncrement, NotNull]
        public int ID { get; set; }

        private DateTime start;
        private DateTime end;
        private DateTime awakeAt;
        private TimeSpan totalLength;
        private TimeSpan lightLength;
        private TimeSpan deepLength;

        private List<LightSleepBlock> lightSleeps;
        private List<DeepSleepBlock> deepSleeps;

        [Column("start"), NotNull]
        public DateTime Start       
        { 
            get { return start; }       
            set { start = value; } 
        }

        [Column("end"), NotNull]
        public DateTime End         
        { 
            get { return end; }         
            set { end = value; } 
        }

        [Column("awake_at"), NotNull]
        public DateTime AwakeAt     
        { 
            get { return awakeAt; }     
            set { awakeAt = value; } 
        }

        [Column("total_length"), NotNull]
        public TimeSpan TotalLength 
        { 
            get { return totalLength; } 
            set { totalLength = value; } 
        }

        [Column("light_length"), NotNull]
        public TimeSpan LightLength 
        { 
            get { return lightLength; } 
            set { lightLength = value; } 
        }

        [Column("deep_length"), NotNull]
        public TimeSpan DeepLength  
        { 
            get { return deepLength; }  
            set { deepLength = value; } 
        }

        [Ignore]
        public List<LightSleepBlock> LightSleeps 
        { 
            get { return lightSleeps; } 
            set { lightSleeps = value; } 
        }
        [Ignore]
        public List<DeepSleepBlock> DeepSleeps 
        { 
            get { return deepSleeps; } 
            set { deepSleeps = value; } 
        }

        public SleepPeriod()
        { }

        public SleepPeriod(List<LightSleepBlock> lightSleeps, List<DeepSleepBlock> deepSleeps)
        {
            if (lightSleeps.Count > 0)
            {
                var lightmin = lightSleeps.Min(s => s.Start);
                var lightmax = lightSleeps.Max(s => s.End);

                this.start = lightmin;
                this.end = lightmax;
            }
            if(deepSleeps.Count > 0)
            {
                var deepmin = deepSleeps.Min(s => s.Start);
                var deepmax = deepSleeps.Max(s => s.End);

                if (this.start > deepmin) { this.start = deepmin; }
                if (this.end < deepmax)   { this.end = deepmax; }
            }

            // calculamos la duracion total de ligero y profundo
            this.lightLength = lightSleeps.Count > 0 ? TimeSpan.FromMinutes(lightSleeps.Sum(s => (s.End - s.Start).TotalMinutes)) : TimeSpan.FromSeconds(0);
            this.deepLength  = deepSleeps.Count > 0 ? TimeSpan.FromMinutes(deepSleeps.Sum(s => (s.End - s.Start).TotalMinutes)) : TimeSpan.FromSeconds(0);

            // finalmente calculamos la duracion total
            this.totalLength = this.lightLength.Add(this.deepLength);

            // guardamos ambas listas
            this.lightSleeps = lightSleeps;
            this.deepSleeps  = deepSleeps;
        }

        public void AssignBlocks(List<SleepBlock> blocks)
        {
            List<LightSleepBlock> lightSleeps = new List<LightSleepBlock>();
            List<DeepSleepBlock> deepSleeps = new List<DeepSleepBlock>();

            foreach(var block in blocks)
            {
                if (block.Mode == (int)ActivityCategory.SleepWithMovement)
                {
                    LightSleepBlock lightBlock = new LightSleepBlock(block.Start, block.End);
                    lightSleeps.Add(lightBlock);
                }

                if (block.Mode == (int)ActivityCategory.SleepWithoutMovement)
                {
                    DeepSleepBlock deepBlock = new DeepSleepBlock(block.Start, block.End);
                    deepSleeps.Add(deepBlock);
                }
            }

            this.lightSleeps = lightSleeps;
            this.deepSleeps = deepSleeps;
        }

        public List<SleepBlock> RetrieveBlocks()
        {
            List<SleepBlock> sleepBlocks = new List<SleepBlock>();
            foreach(var block in lightSleeps)
            {
                SleepBlock sleepBlock = new SleepBlock(this.ID, (int)ActivityCategory.SleepWithMovement, block.Start, block.End);
                sleepBlocks.Add(sleepBlock);
            }
            foreach (var block in deepSleeps)
            {
                SleepBlock sleepBlock = new SleepBlock(this.ID, (int)ActivityCategory.SleepWithoutMovement, block.Start, block.End);
                sleepBlocks.Add(sleepBlock);
            }

            return sleepBlocks;
        }
    }
}
