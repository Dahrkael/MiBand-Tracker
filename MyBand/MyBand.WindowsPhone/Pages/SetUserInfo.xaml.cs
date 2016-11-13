using MyBand.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace MyBand.Pages
{
    public sealed partial class SetUserInfo : Page, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private bool initialSetup = false;
        private Entities.UserInfo userInfo = null;

        public string Alias
        {
            get { return userInfo.Alias; }
            set { userInfo.Alias = value; NotifyPropertyChanged(); }
        }
        public bool Male
        {
            get { return userInfo.Male; }
            set { userInfo.Male = value; NotifyPropertyChanged(); }
        }
        public bool Female
        {
            get { return !userInfo.Male; }
            set { userInfo.Male = !value; NotifyPropertyChanged(); }
        }
        public object InfoAge
        {
            get { return userInfo.Age; }
            set { userInfo.Age = Convert.ToByte(value); NotifyPropertyChanged(); }
        }
        public object InfoHeight
        {
            get { return userInfo.Height; }
            set { userInfo.Height = Convert.ToByte(value); NotifyPropertyChanged(); }
        }
        public object InfoWeight
        {
            get { return userInfo.Weight; }
            set { userInfo.Weight = Convert.ToByte(value); NotifyPropertyChanged(); }
        }

        public SetUserInfo()
        {
            this.InitializeComponent();

            ObservableCollection<int> ages = new ObservableCollection<int>();
            for(int i = 1; i < 100; i++) { ages.Add(i); }
            cmbAge.ItemsSource = ages;
            ObservableCollection<int> heights = new ObservableCollection<int>();
            for (int i = 100; i < 250; i++) { heights.Add(i); }
            cmbHeight.ItemsSource = heights;
            ObservableCollection<int> weights = new ObservableCollection<int>();
            for (int i = 30; i < 200; i++) { weights.Add(i); }
            cmbWeight.ItemsSource = weights;

            if (MiBand.Band != null)
            {
                userInfo = Entities.UserInfo.FromSetting((ApplicationDataCompositeValue)MiBand.Band.SettingGet("UserInfo"));
                Alias  = userInfo.Alias;
                Male   = userInfo.Male;
                Female = !userInfo.Male;
                InfoAge    = userInfo.Age;
                InfoHeight = userInfo.Height;
                InfoWeight = userInfo.Weight;

                cmbAge.SelectedIndex = userInfo.Age - 1;
                cmbHeight.SelectedIndex = userInfo.Height - 100;
                cmbWeight.SelectedIndex = userInfo.Weight - 30;
            }
            if (userInfo == null)
            {
                Debugger.Break();
            }
            LayoutRoot.DataContext = this;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter != null)
            {
                if ((e.Parameter as string) == "InitialSetup")
                {
                    initialSetup = true;
                }
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // recuperamos del combobox el manejo de los datos
            ComboBoxItem cbi = cmbDataMode.SelectedItem as ComboBoxItem;
            DataMode mode = (DataMode)Int32.Parse((string)cbi.Tag);
            // cogemos el perfil actual
            Entities.UserInfo currentUserInfo = Entities.UserInfo.FromSetting((ApplicationDataCompositeValue)MiBand.Band.SettingGet("UserInfo"));
            // FIXME el comparador no funciona, necesita uno personalizado
            if (currentUserInfo != userInfo)
            {
                // si difiere lo cambiamos
                await MiBand.Band.SetUserInfo(userInfo, mode);
            }
            if (initialSetup)
            {
                this.Frame.Navigate(typeof(SetDailyGoal), "InitialSetup");
                return;
            }
            this.Frame.GoBack();
        }
    }
}
