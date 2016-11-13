using System;
using System.Collections.Generic;
using System.Text;

namespace MyBand.Entities
{
    class LEParams
    {
        private int connIntMin;
        private int connIntMax;
        private int latency;
        private int timeout;
        private int connInt;
        private int advInt;

        public LEParams(Byte[] data)
        {
            if (data.Length != 12) { Valid = false; return; }
            this.connIntMin = data[0] | (data[1] << 8);
            this.connIntMax = data[2] | (data[3] << 8);
            this.latency    = data[4] | (data[5] << 8);
            this.timeout    = data[6] | (data[7] << 8);
            this.connInt    = data[8] | (data[9] << 8);
            this.advInt     = data[10] | (data[11] << 8);

            /*connIntMin * 1.25  milliseconds
            connIntMax * 1.25  milliseconds
            latency            milliseconds
            timeout    * 10    milliseconds
            connInt    * 1.25  milliseconds
            advInt     * 0.625 milliseconds*/
            Valid = true;
        }

        public bool Valid { get; set; }
        public int ConnIntMin { get { return this.connIntMin; } set {} }
        public int ConnIntMax { get { return this.connIntMax; } set {} }
        public int Latency    { get { return this.latency; }    set {} }
        public int Timeout    { get { return this.timeout; }    set {} }
        public int ConnInt    { get { return this.connInt; }    set {} }
        public int AdvInt     { get { return this.advInt; }     set {} }
    }
}
