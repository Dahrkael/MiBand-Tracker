using System;
using System.Collections.Generic;
using System.Text;
using Windows.Storage;

namespace MyBand.Entities
{
    class BatteryInfo
    {
        private byte          percentage  = 0;
        private int           charges     = 0;
        private BatteryStatus status      = BatteryStatus.None;
        private DateTime      lastCharged = DateTime.Now.Date.AddDays(-1);

        public BatteryInfo(byte Percentage, int Charges, BatteryStatus Status, DateTime LastCharged)
        {
            this.percentage = Percentage;
            this.charges = Charges;
            this.status = Status;
            this.lastCharged = LastCharged;
            Valid = true;
        }

        public BatteryInfo(Byte[] Data)
        {
            if (Data.Length != 10) { Valid = false; return; }

            this.percentage = Data[0];
            this.charges = 0xffff & (0xff & Data[7] | (0xff & Data[8]) << 8);  
            this.status = (BatteryStatus)Data[9];
            try
            {
                this.lastCharged = new DateTime(Data[1] + 2000, Data[2] + 1, Data[3], Data[4], Data[5], Data[6]);
            } 
            catch(Exception)
            {
                this.lastCharged = DateTime.MinValue;
            }
            Valid = true;
        }

        public bool Valid { get; set; }
        public byte          Level       { get { return this.percentage; } }
        public int           Charges     { get { return this.charges; } }
        public BatteryStatus Status      { get { return this.status; } }
        public DateTime      LastCharged { get { return this.lastCharged; } }

        public static BatteryInfo FromSetting(ApplicationDataCompositeValue setting)
        {
            BatteryInfo info = new BatteryInfo((byte)setting["percentage"], (int)setting["charges"], (BatteryStatus)setting["status"], DateTime.FromBinary((long)setting["lastCharged"]));
            return info;
        }

        public ApplicationDataCompositeValue ToSetting()
        {
            ApplicationDataCompositeValue setting = new ApplicationDataCompositeValue();
            setting["percentage"]  = this.percentage;
            setting["charges"]     = this.charges;
            setting["status"]      = (int)this.status;
            setting["lastCharged"] = this.lastCharged.ToBinary();

            return setting;
        }

        public String LastChargedS
        {
            get
            {
                var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                if (Valid)
                {
                    if (lastCharged == DateTime.MinValue) { return loader.GetString("BatteryNeverCharged"); }
                    TimeSpan lastCharge = new TimeSpan(LastCharged.Ticks);
                    TimeSpan today = new TimeSpan(DateTime.Now.Ticks);
                    var span = today - lastCharge;
                    double days = Math.Truncate(span.TotalDays);
                    double hours = Math.Truncate(span.TotalHours);
                    double minutes = Math.Truncate(span.TotalMinutes);
                    if (days > 0)
                    {
                        return loader.GetString("BatteryChargedSince") + days + loader.GetString("BatteryDays");

                    }
                    else if (hours > 0)
                    {
                        return loader.GetString("BatteryChargedSince") + hours + loader.GetString("BatteryHours");
                    }
                    else if (minutes > 0)
                    {
                        return loader.GetString("BatteryChargedSince") + minutes + loader.GetString("BatteryMinutes");
                    }
                    else
                    {
                        return loader.GetString("BatteryRecent");
                    }
                }
                return loader.GetString("BatteryMissing");
            }
        }
    }

    enum BatteryStatus
    {
        None = 0,
        Low = 1,
        Charging = 2,
        Full = 3,
        NotCharging = 4
    }
}
