using MyBand.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace MyBand.Pages
{
    public sealed partial class SetAlarm : Page, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private Alarm alarm = null;
        private TimeSpan time = new TimeSpan(12, 0, 0);
        private bool smart;
        private bool daysAll       = false;
        private bool daysMonday    = false;
        private bool daysTuesday   = false;
        private bool daysWednesday = false;
        private bool daysThursday  = false;
        private bool daysFriday    = false;
        private bool daysSaturday  = false;
        private bool daysSunday    = false;

        public TimeSpan Time
        {
            get { return this.time; }
            set { this.time = value; NotifyPropertyChanged(); }
        }
        public bool Smart
        {
            get { return this.smart; }
            set { this.smart = value; NotifyPropertyChanged(); }
        }

        public bool DaysAll
        {
            get { return this.daysAll; }
            set { this.daysAll = value; NotifyPropertyChanged(); }
        }
        public bool DaysMonday
        {
            get { return this.daysMonday; }
            set { this.daysMonday = value; NotifyPropertyChanged(); }
        }
        public bool DaysTuesday
        {
            get { return this.daysTuesday; }
            set { this.daysTuesday = value; NotifyPropertyChanged(); }
        }
        public bool DaysWednesday
        {
            get { return this.daysWednesday; }
            set { this.daysWednesday = value; NotifyPropertyChanged(); }
        }
        public bool DaysThursday
        {
            get { return this.daysThursday; }
            set { this.daysThursday = value; NotifyPropertyChanged(); }
        }
        public bool DaysFriday
        {
            get { return this.daysFriday; }
            set { this.daysFriday = value; NotifyPropertyChanged(); }
        }
        public bool DaysSaturday
        {
            get { return this.daysSaturday; }
            set { this.daysSaturday = value; NotifyPropertyChanged(); }
        }
        public bool DaysSunday
        {
            get { return this.daysSunday; }
            set { this.daysSunday = value; NotifyPropertyChanged(); }
        }
    
        public SetAlarm()
        {
            this.InitializeComponent();

            LayoutRoot.DataContext = this;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter == null)
            {
                Debugger.Break();
            }
            var parameter = e.Parameter as Alarm;
            alarm = parameter;

            Smart = alarm.Smart;
            Time = alarm.When.TimeOfDay;

            if ((alarm.Repeat & Alarm.Everyday) == Alarm.Everyday) { DaysAll = true; return; }
            if ((alarm.Repeat & Alarm.Monday)    != 0) { DaysMonday = true; }
            if ((alarm.Repeat & Alarm.Tuesday)   != 0) { DaysTuesday = true; }
            if ((alarm.Repeat & Alarm.Wednesday) != 0) { DaysWednesday = true; }
            if ((alarm.Repeat & Alarm.Thursday)  != 0) { DaysThursday = true; }
            if ((alarm.Repeat & Alarm.Friday)    != 0) { DaysFriday = true; }
            if ((alarm.Repeat & Alarm.Saturday)  != 0) { DaysSaturday = true; }
            if ((alarm.Repeat & Alarm.Sunday)    != 0) { DaysSunday = true; }
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // FIXME esto tiene que haber una forma mas mejor de hacerlo
            byte repeat = 0;
            if (DaysAll) 
            { 
                repeat = Alarm.Everyday; 
            }
            else
            {
                if (DaysMonday)    { repeat = (byte)(repeat | Alarm.Monday); }
                if (DaysTuesday)   { repeat = (byte)(repeat | Alarm.Tuesday); }
                if (DaysWednesday) { repeat = (byte)(repeat | Alarm.Wednesday); }
                if (DaysThursday)  { repeat = (byte)(repeat | Alarm.Thursday); }
                if (DaysFriday)    { repeat = (byte)(repeat | Alarm.Friday); }
                if (DaysSaturday)  { repeat = (byte)(repeat | Alarm.Saturday); }
                if (DaysSunday)    { repeat = (byte)(repeat | Alarm.Sunday); }
            }

            this.alarm.Smart = smart;
            this.alarm.When = DateTime.Today.Add(Time);
            this.alarm.Repeat = repeat;
            
            if (MiBand.Band != null)
            {
                bool ret = await MiBand.Band.SetAlarm(alarm);
                if (!ret)
                {
                    var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                    MessageDialog dialog = new MessageDialog(loader.GetString("SetAlarmErrorText"), loader.GetString("SetAlarmErrorTitle"));
                    await dialog.ShowAsync();
                }
            }
            this.Frame.Navigate(typeof(Pages.Main), "FromSetAlarm");
        }
    }
}
