using System;
using System.Collections.Generic;
using System.Text;

namespace MyBand.Entities.ActivityTracking.Sleep
{
    public class DeepSleepBlock
    {
        private DateTime start;
        private DateTime end;

        public DateTime Start
        {
            get { return start; }
        }

        public DateTime End
        {
            get { return end; }
        }
        
        public DeepSleepBlock(DateTime start, DateTime end)
        {
            this.start = start;
            this.end = end;
        }
    }
}
