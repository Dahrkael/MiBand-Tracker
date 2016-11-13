using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyBand.Entities.ActivityTracking.Steps
{
    [Table("steps_blocks")]
    public class StepsBlock
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

        [Column("steps"), NotNull]
        public int Steps
        {
            get;
            set;
        }

        [Column("runs"), NotNull]
        public int Runs
        {
            get;
            set;
        }
        
        public StepsBlock()
        { }

        public StepsBlock(int period, int mode, DateTime start, DateTime end, int steps, int runs)
        {
            this.Period = period;
            this.Mode = mode;
            this.Start = start;
            this.End   = end;
            this.Steps = steps;
            this.Runs  = runs;
        }
    }
}
