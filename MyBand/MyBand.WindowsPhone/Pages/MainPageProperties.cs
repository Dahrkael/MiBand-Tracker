using MyBand.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MyBand
{
    class MainPageProperties : INotifyPropertyChanged
    {
        private Pages.Main parent = null;
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            //parent.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            //{
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            //});
        }

        public MainPageProperties(Pages.Main page)
        {
            parent = page;
        }

        private bool loading = true;
        public bool Loading
        {
            get { return loading; }
            set { if (loading != value) { loading = value; NotifyPropertyChanged(); } }
        }

        private Visibility loadingV = Visibility.Visible;
        public Visibility LoadingV
        {
            get { return loadingV; }
            set { if (loadingV != value) { loadingV = value; NotifyPropertyChanged(); } }
        }

        private bool bgloading = true;
        public bool BGLoading
        {
            get { return bgloading; }
            set { if (bgloading != value) { bgloading = value; NotifyPropertyChanged(); } }
        }

        private Visibility bgloadingV = Visibility.Visible;
        public Visibility BGLoadingV
        {
            get { return bgloadingV; }
            set { if (bgloadingV != value) { bgloadingV = value; NotifyPropertyChanged(); } }
        }

        #region Datos actuales propiedades
        private string bandName = "MI";
        private string macAddress = "00:00:00:00:00:00";
        private string batteryState = "None";
        private string batterylevel = "0%";
        private string lastCharged = "";
        private int    goal = 0;
        private int    currentSteps = 0;
        private string currentDistance = "0 km";
        private string currentCalories = "0 cal";

        public string BandName      
        { 
            get { return bandName; }             
            set { if (value != this.bandName) { this.bandName = value; NotifyPropertyChanged(); } } 
        }
        public string MACAddress     
        { 
            get { return macAddress; }          
            set { if (value != this.macAddress) { this.macAddress = value; NotifyPropertyChanged(); } } 
        }
        public string BatteryState
        {
            get { return "ms-appx:///Assets/battery-" + batteryState + ".png"; }
            set { if (value != this.batteryState) { this.batteryState = value; NotifyPropertyChanged(); } } 
        }
        public string Batterylevel   
        { 
            get { return batterylevel; }        
            set { if (value != this.batterylevel) { this.batterylevel = value; NotifyPropertyChanged(); } } 
        }
        public string LastCharged    
        { 
            get { return lastCharged; }         
            set { if (value != this.lastCharged) { this.lastCharged = value; NotifyPropertyChanged(); } } 
        }
        public int Goal 
        { 
            get { return goal; }
            set { if (value != this.goal) { this.goal = value; this.RemainingSteps = 0; NotifyPropertyChanged(); } } 
        }
        public int CurrentSteps 
        { 
            get { return currentSteps; } 
            set 
            { 
                if (value != this.currentSteps) 
                { 
                    this.currentSteps = value;
                    if (MiBand.Band != null)
                    {

                        this.CurrentDistance = Math.Round(MiBand.Band.CurrentDistance(), 2) + " km";
                        this.CurrentCalories = Math.Truncate(MiBand.Band.CurrentCalories()) + " cal";
                    }
                    this.RemainingSteps = 0; 
                    NotifyPropertyChanged(); 
                } 
            } 
        }

        public string CurrentDistance
        {
            get
            {
                return currentDistance;
            }
            set { currentDistance = value; NotifyPropertyChanged(); }
        }
        public string CurrentCalories
        {
            get
            {
                return currentCalories;
            }
            set { currentCalories = value; NotifyPropertyChanged(); }
        }

        public int RemainingSteps 
        { 
            get 
            {
                if (Goal - CurrentSteps < 0) { return 0; }
                return Goal - CurrentSteps; 
            }
            set { ProgressBarSteps = 0; NotifyPropertyChanged(); }
        }

        public int ProgressBarSteps
        {
            get 
            { 
                if (CurrentSteps <= Goal) { return CurrentSteps; }
                return Goal;
            }
            set { NotifyPropertyChanged(); }
        }
        #endregion
        #region Datos actuales callbacks
        public async void batteryValueChanged(BatteryInfo info)
        {
            await parent.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (info.Valid)
                {
                    Batterylevel = info.Level + "%";
                    BatteryState = info.Status.ToString();
                    LastCharged = info.LastChargedS;
                }
            });
        }

        public async void realtimeStepsValueChanged(int Steps)
        {
            await parent.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                CurrentSteps = Steps;
            });
        }

        public void notificationValueChanged(byte Status)
        {

        }

        public void activityDataValueChanged()
        {

        }

        public void sensorDataValueChanged()
        {

        }
        #endregion

        #region Alarmas y ajustes propiedades
        private Alarm alarm1 = null;
        private Alarm alarm2 = null;
        private Alarm alarm3 = null;
        private ColorTheme colorTheme = ColorTheme.Aqua;
        private WearLocation wearLocation = WearLocation.LeftHand;

        public Alarm Alarm1 
        {
            get { return alarm1; }
            set 
            {
                if (value != alarm1) 
                { 
                    this.alarm1 = value;
                    NotifyPropertyChanged(); 
                } 
            }
        }
        public Alarm Alarm2
        {
            get { return alarm2; }
            set { if (value != alarm2) { this.alarm2 = value; NotifyPropertyChanged(); } }
        }
        public Alarm Alarm3
        {
            get { return alarm3; }
            set { if (value != alarm3) { this.alarm3 = value; NotifyPropertyChanged(); } }
        }
        public ColorTheme ColorTheme
        {
            get { return colorTheme; }
            set { if (value != colorTheme) { this.colorTheme = value; NotifyPropertyChanged(); } }
        }
        public WearLocation WearLocation
        {
            get { return wearLocation; }
            set { if (value != wearLocation) { this.wearLocation = value; WearLocationS = ""; NotifyPropertyChanged(); } }
        }
        public String WearLocationS
        {
            get
            {
                var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                if (WearLocation == Entities.WearLocation.LeftHand) { return loader.GetString("WearLocationLeftHand"); }
                if (WearLocation == Entities.WearLocation.RightHand) { return loader.GetString("WearLocationRightHand"); }
                if (WearLocation == Entities.WearLocation.Neck) { return loader.GetString("WearLocationNecklace"); }
                return "Desconocida";
            }
            set { NotifyPropertyChanged(); }
        }
        #endregion
        #region Alarmas y ajustes callbacks
        #endregion

        #region Informacion propiedades
        private DeviceInfo deviceInfo = null;
        public DeviceInfo DeviceInfo
        {
            get { return deviceInfo; }
            set 
            { 
                if (value != deviceInfo) 
                { 
                    this.deviceInfo = value;
                    FirmwareVersion = "a";
                    HardwareVersion = "b";
                    ProfileVersion  = "c";
                    Appearance      = "d";
                    Feature         = "e";
                    NotifyPropertyChanged(); 
                } 
            }
        }
        public String FirmwareVersion
        {
            get { return (deviceInfo == null) ? "0.0.0.0" : deviceInfo.FirmwareVersion; }
            set { NotifyPropertyChanged(); }
        }
        public String HardwareVersion
        {
            get { return (deviceInfo == null) ? "0x00" : String.Format("0x{0:X8}", deviceInfo.HardwareVersion.ToString("x")); }
            set { NotifyPropertyChanged(); }
        }
        public String ProfileVersion
        {
            get { return (deviceInfo == null) ? "0x00" : deviceInfo.ProfileVersion; }
            set { NotifyPropertyChanged(); }
        }
        public String Appearance
        {
            get { return (deviceInfo == null) ? "0x00" : String.Format("0x{0:X8}", deviceInfo.Appearance.ToString("x")); }
            set { NotifyPropertyChanged(); }
        }
        public String Feature
        {
            get { return (deviceInfo == null) ? "0x00" : String.Format("0x{0:X8}", deviceInfo.Feature.ToString("x")); }
            set { NotifyPropertyChanged(); }
        }
        private String horaSincronizada = "00/00/0000 00:00:00";
        public String HoraSincronizada
        {
            get { return horaSincronizada; }
            set { horaSincronizada = value; NotifyPropertyChanged(); }
        }

        public String AppVersion
        {
            get 
            {
                var pkgVersion = Windows.ApplicationModel.Package.Current.Id.Version;
                return String.Format("{0}.{1}.{2}.{3}", pkgVersion.Major, pkgVersion.Minor, pkgVersion.Build, pkgVersion.Revision);
            }
        }
        #endregion
        #region Informacion callbacks
        #endregion
    }
}
