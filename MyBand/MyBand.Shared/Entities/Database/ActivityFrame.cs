using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyBand.Entities.Database
{
    [Table("activity_frames")]
    public class ActivityFrame
    {
        /*[Column(IsDbGenerated = true, IsPrimaryKey = true)]
        public int ID { get; set; }*/

        [PrimaryKey, Column("timestamp")]
        public DateTime TimeStamp { get; set; }

        [Column("intensity")]
        public int Intensity { get; set; }

        [Column("steps")]
        public int Steps { get; set; }

        [Column("mode")]
        public byte Mode { get; set; }

        [Column("runs")]
        public byte Runs { get; set; }

        public ActivityFrame()
        { }

        public ActivityFrame(DateTime Moment, int Intensity, int Steps, byte Mode, byte Runs)
        {
            this.TimeStamp = Moment;
            this.Intensity = Intensity;
            this.Steps = Steps;
            this.Mode = Mode;
            this.Runs = Runs;
        }
    }
}
