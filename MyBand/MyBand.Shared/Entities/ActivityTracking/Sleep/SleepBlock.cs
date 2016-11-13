using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyBand.Entities.ActivityTracking.Sleep
{
    [Table("sleep_blocks")]
    public class SleepBlock
    {
        [PrimaryKey, AutoIncrement, NotNull]
        public int ID { get; set; }

        [Column("period"), NotNull]
        public int Period
        {
            get;
            set;
        }

        [Column("mode"), NotNull]
        public int Mode 
        { 
            get; 
            set; 
        }

        [Column("start"), NotNull]
        public DateTime Start
        {
            get;
            set;
        }

        [Column("end"), NotNull]
        public DateTime End
        {
            get;
            set;
        }
        
        public SleepBlock()
        { }

        public SleepBlock(int period, int mode, DateTime start, DateTime end)
        {
            this.Start = start;
            this.End = end;
            this.Mode = mode;
            this.Period = period;
        }
    }
}
