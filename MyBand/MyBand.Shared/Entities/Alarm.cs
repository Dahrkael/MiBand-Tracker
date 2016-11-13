using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using Windows.Storage;

namespace MyBand.Entities
{
    // estas deberian ser enum? dan mas vuelta los enums para pasarlo a bytes?
    // when = datetime.now() + timedelta(hours=8)
    class Alarm : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        
        public static byte Monday    = 0x01;
        public static byte Tuesday   = 0x02;
        public static byte Wednesday = 0x04;
        public static byte Thursday  = 0x08;
        public static byte Friday    = 0x10;
        public static byte Saturday  = 0x20;
        public static byte Sunday    = 0x40;
        public static byte Everyday  = 0x7F;
        

        private byte id;
        private bool smart; // FIXME el smart es una duracion en minutos supongo, no un bool
        private DateTime when;
        private byte repeat;
        private bool enabled;

        public Alarm(byte ID, bool Smart, DateTime When, byte Repeat, bool Enabled)
        {
            if (ID < 0 && ID > 2) { Valid = false; return; }
            this.id     = ID;
            this.smart  = Smart;
            this.when   = When.AddMonths(-1);
            this.repeat = Repeat;
            this.enabled = Enabled;
            Valid = true;
        }

        public bool Valid { get; set; }
        public byte     ID      { get { return this.id;      } set { this.id = value; NotifyPropertyChanged(); } }
        public bool     Smart   { get { return this.smart;   } set { this.smart = value; NotifyPropertyChanged(); } }
        public DateTime When    { get { return this.when;    } set { this.when = value; NotifyPropertyChanged(); } }
        public byte     Repeat  { get { return this.repeat;  } set { this.repeat = value; WhenText = ""; NotifyPropertyChanged(); } }
        public bool     Enabled { get { return this.enabled; } set { this.enabled = value; EnabledText = ""; NotifyPropertyChanged(); } }
        public String EnabledText 
        { 
            get 
            {
                var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                string text = loader.GetString("AlarmSetTo") + When.TimeOfDay.ToString(@"hh\:mm");
                if (Smart) { text += " " + loader.GetString("SmartMode"); }
                return text;
            } 
            set { NotifyPropertyChanged(); } 
        }
        public String WhenText 
        { 
            get 
            {
                var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                string text = "";
                if (repeat == 0) { text = loader.GetString("Never"); }
                else if ((repeat & Everyday) == Everyday) { text = loader.GetString("Everyday"); }
                else
                {
                    if ((repeat & Monday)    != 0) { text += loader.GetString("Monday")    + ", "; }
                    if ((repeat & Tuesday)   != 0) { text += loader.GetString("Tuesday")   + ", "; }
                    if ((repeat & Wednesday) != 0) { text += loader.GetString("Wednesday") + ", "; }
                    if ((repeat & Thursday)  != 0) { text += loader.GetString("Thursday")  + ", "; }
                    if ((repeat & Friday)    != 0) { text += loader.GetString("Friday")    + ", "; }
                    if ((repeat & Saturday)  != 0) { text += loader.GetString("Saturday")  + ", "; }
                    if ((repeat & Sunday)    != 0) { text += loader.GetString("Sunday")    + ", "; }
                    text = text.Remove(text.Length - 2);
                }
                return text;
            } 
            set { NotifyPropertyChanged(); } 
        }


        public static Alarm FromSetting(ApplicationDataCompositeValue setting)
        {
            Alarm info = new Alarm((byte)setting["id"], (bool)setting["smart"], DateTime.FromBinary((long)setting["when"]), (byte)setting["repeat"], (bool)setting["enabled"]);
            return info;
        }

        public ApplicationDataCompositeValue ToSetting()
        {
            ApplicationDataCompositeValue setting = new ApplicationDataCompositeValue();

            setting["id"]      = this.id;
            setting["smart"]   = this.smart;
            setting["when"]    = this.when.AddMonths(1).ToBinary();
            setting["repeat"]  = this.repeat;
            setting["enabled"] = this.enabled;

            return setting;
        }
    }
}
