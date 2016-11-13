using System;
using System.Collections.Generic;
using System.Text;

namespace MyBand.Entities.ActivityTracking.Sleep
{
    public class LightSleepBlock
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
        
        public LightSleepBlock(DateTime start, DateTime end)
        {
            this.start = start;
            this.end = end;
        }
    }
}
