using System;
using System.Collections.Generic;
using System.Text;

namespace MyBand.Entities.ActivityTracking.Steps
{
    public class WalkStepsBlock
    {
        private DateTime start;
        private DateTime end;
        private int steps;
        private int runs;

        public DateTime Start
        {
            get { return start; }
        }

        public DateTime End
        {
            get { return end; }
        }

        public int Steps
        {
            get { return steps; }
        }

        public int Runs
        {
            get { return runs; }
        }
        
        public WalkStepsBlock(DateTime start, DateTime end, int steps, int runs)
        {
            this.start = start;
            this.end   = end;
            this.steps = steps;
            this.runs  = runs;
        }
    }
}
