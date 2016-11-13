using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using SQLite;

namespace MyBand.Entities.ActivityTracking.Steps
{
    [Table("steps_periods")]
    public class StepsPeriod
    {
        [PrimaryKey, AutoIncrement, NotNull]
        public int ID { get; set; }

        private DateTime start;
        private DateTime end;
        private TimeSpan totalLength;
        private TimeSpan walkLength;
        private TimeSpan runLength;
        private int totalSteps;
        private int walkStepsCount;
        private int runStepsCount;

        private List<WalkStepsBlock> walkSteps;
        private List<RunStepsBlock> runSteps;

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

        [Column("total_length"), NotNull]
        public TimeSpan TotalLength 
        { 
            get { return totalLength; } 
            set { totalLength = value; } 
        }

        [Column("walk_length"), NotNull]
        public TimeSpan WalkLength 
        { 
            get { return walkLength; } 
            set { walkLength = value; } 
        }

        [Column("run_length"), NotNull]
        public TimeSpan RunLength 
        { 
            get { return runLength; } 
            set { runLength = value; } 
        }

        [Column("total_steps"), NotNull]
        public int TotalSteps
        { 
            get { return totalSteps; } 
            set { totalSteps = value; } 
        }

        [Column("walk_steps_count"), NotNull]
        public int WalkStepsCount
        {
            get { return walkStepsCount; }
            set { walkStepsCount = value; }
        }

        [Column("run_steps_count"), NotNull]
        public int RunStepsCount
        {
            get { return runStepsCount; }
            set { runStepsCount = value; }
        }

        [Ignore]
        public List<WalkStepsBlock> WalkSteps 
        { 
            get { return walkSteps; } 
            set { walkSteps = value; } 
        }
        [Ignore]
        public List<RunStepsBlock> RunSteps 
        { 
            get { return runSteps; } 
            set { runSteps = value; } 
        }

        public StepsPeriod()
        { }

        public StepsPeriod(List<WalkStepsBlock> walkSteps, List<RunStepsBlock> runSteps)
        {
            // calculamos el inicio y final
            if (walkSteps.Count > 0)
            {
                var walkmin = walkSteps.Min(s => s.Start);
                var walkmax = walkSteps.Max(s => s.End);

                this.start = walkmin.Date;
                this.end = walkmax.Date;
            }

            if (runSteps.Count > 0)
            {
                var runmin = runSteps.Min(s => s.Start);
                var runmax = runSteps.Max(s => s.End);

                if (this.start > runmin) { this.start = runmin.Date; }
                if (this.end < runmax)   { this.end = runmax.Date; }
            }
            
            // el final lo guardamos como el final del dia
            this.end = this.end.AddHours(23).AddMinutes(59).AddSeconds(59);

            // calculamos la duracion total de ligero y profundo
            this.walkLength = walkSteps.Count > 0 ? TimeSpan.FromMinutes(walkSteps.Sum(s => (s.End - s.Start).TotalMinutes)) : TimeSpan.FromSeconds(0);
            this.runLength  = runSteps.Count > 0 ? TimeSpan.FromMinutes(runSteps.Sum(s => (s.End - s.Start).TotalMinutes)) : TimeSpan.FromSeconds(0);

            // finalmente calculamos la duracion total
            this.totalLength = this.walkLength.Add(this.runLength);

            // guardamos ambas listas
            this.walkSteps = walkSteps;
            this.runSteps  = runSteps;
        }

        public void AssignBlocks(List<StepsBlock> blocks)
        {
            List<WalkStepsBlock> walkSteps = new List<WalkStepsBlock>();
            List<RunStepsBlock> runSteps = new List<RunStepsBlock>();

            foreach (var block in blocks)
            {
                if (block.Mode == (int)ActivityCategory.Moving)
                {
                    WalkStepsBlock walkBlock = new WalkStepsBlock(block.Start, block.End, block.Steps, block.Runs);
                    walkSteps.Add(walkBlock);
                }

                if (block.Mode == (int)ActivityCategory.Moving2)
                {
                    RunStepsBlock runBlock = new RunStepsBlock(block.Start, block.End, block.Steps, block.Runs);
                    runSteps.Add(runBlock);
                }
            }

            this.walkSteps = walkSteps;
            this.runSteps = runSteps;
        }

        public List<StepsBlock> RetrieveBlocks()
        {
            List<StepsBlock> stepsBlocks = new List<StepsBlock>();
            foreach (var block in walkSteps)
            {
                StepsBlock sleepBlock = new StepsBlock(this.ID, (int)ActivityCategory.Moving, block.Start, block.End, block.Steps, block.Runs);
                stepsBlocks.Add(sleepBlock);
            }
            foreach (var block in runSteps)
            {
                StepsBlock sleepBlock = new StepsBlock(this.ID, (int)ActivityCategory.Moving2, block.Start, block.End, block.Steps, block.Runs);
                stepsBlocks.Add(sleepBlock);
            }

            return stepsBlocks;
        }

        // propiedades para mostrar en pantalla
        public string DailyDistance { get { return "0" + "km"; } set { } }
        public string DailySteps    { get { return TotalSteps.ToString(); } set { } }
        public string DailyBurn     { get { return "0" + "kcal"; } set { } }
        public string WalkDistance  { get { return "0" + "km"; } set { } }
        public string WalkTime      { get { return WalkLength.TotalHours + "h"; } set { } }
        public string WalkBurn      { get { return "0" + "kcal"; } set { } }
        public string RunDistance   { get { return "0" + "km"; } set { } }
        public string RunTime       { get { return RunLength.TotalHours + "h"; } set { } }
        public string RunBurn       { get { return "0" + "kcal"; } set { } }
    }
}
