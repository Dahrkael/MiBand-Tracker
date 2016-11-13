using MyBand.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Email;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
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
    public sealed partial class Main : Page
    {
        private MiBand band = null;
        private MainPageProperties properties;
        private Action<string> tapCallback = null;

        public Main()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;

            properties = new MainPageProperties(this);
            Hubbie.DataContext = properties;
            pgConnecting.DataContext = properties;
            
            initialSetup();
        }

        private async void initialSetup()
        {
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
            // guardo la pulsera en un singleton global
            MiBand.Band = await MiBand.FirstMatch();
            // pero la asigno localmente para que no de tanto por culo
            band = MiBand.Band;

            if (band != null)
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                
                if (localSettings.Containers.ContainsKey("MiBand"))
                {
                    var miBandSettings = localSettings.Containers["MiBand"];
                    string savedMAC = (string)miBandSettings.Values["MAC"];
                    string currentMAC = await band.getAddress();
                    if (savedMAC.Equals(currentMAC))
                    {
                        // misma pulsera
                        Debug.WriteLine("[mainpage] misma pulsera");
                        band.Settings = miBandSettings;
                    }
                    else
                    {
                        // diferente pulsera
                        Debug.WriteLine("[mainpage] diferente pulsera");
                        properties.MACAddress = currentMAC;
                        bool ret = await band.ConfigureAsSettings();
                        if (ret)
                        {
                            Debug.WriteLine("[mainpage] error al configurar como en los ajustes");
                        }
                    }
                }
                else
                {
                    MessageDialog dialog = new MessageDialog(loader.GetString("GreetingText"), loader.GetString("GreetingTitle"));
                    await dialog.ShowAsync();
                    // la aplicacion esta recien instalada?
                    // ir a pagina de configuracion inicial
                    Debug.WriteLine("[mainpage] no hay datos de pulsera");
                    if (await band.CreateSettings())
                    {
                        string currentMAC = await band.getAddress();
                        properties.MACAddress = currentMAC;
                        properties.LoadingV = Visibility.Collapsed;
                        properties.Loading = false;
                        this.Frame.Navigate(typeof(Pages.SetUserInfo), "InitialSetup");
                        return;
                    }
                }
                updateInfoFromSettings();
                properties.LoadingV = Visibility.Collapsed;
                properties.Loading = false;
                Task.Factory.StartNew(() => { finishSetup(); });
            }
            else 
            {
                // si no detectamos la pulsera se cierra la aplicacion y fuera
                MessageDialog dialog = new MessageDialog(loader.GetString("MiBandNotFoundText"), loader.GetString("MiBandNotFoundTitle"));
                await dialog.ShowAsync();

                updateInfoFromSettings();
                properties.LoadingV = Visibility.Collapsed;
                properties.Loading = false;
                //Application.Current.Exit();
            }
        }

        private async void finishSetup()
        {
            if (band != null)
            {
                var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                bool authed = await band.Authenticate();
                if (!authed)
                {
                    Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                    {
                        MessageDialog dialog = new MessageDialog(loader.GetString("AuthErrorText"), loader.GetString("ErrorTitle"));
                        await dialog.ShowAsync();
                    });
                }
                #region notifications
                int notificationsRet = await band.TurnOnNotifications();
                if (notificationsRet > 0)
                {
                    if ((notificationsRet & 0x01) > 0)
                    { band.onNotificationCallback = properties.notificationValueChanged; }
                    else
                    {
                        Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                        {
                            MessageDialog dialog = new MessageDialog(loader.GetString("NotifError1"), loader.GetString("ErrorTitle"));
                            await dialog.ShowAsync();
                        });
                    }
                    if ((notificationsRet & 0x02) > 0)
                    { band.onRealtimeStepsCallback = properties.realtimeStepsValueChanged; }
                    else
                    {
                        Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                        {
                            MessageDialog dialog = new MessageDialog(loader.GetString("NotifError2"), loader.GetString("ErrorTitle"));
                            await dialog.ShowAsync();
                        });
                    }
                    if ((notificationsRet & 0x04) > 0)
                    { band.onBatteryCallback = properties.batteryValueChanged; }
                    else
                    {
                        Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                        {
                            MessageDialog dialog = new MessageDialog(loader.GetString("NotifError3"), loader.GetString("ErrorTitle"));
                            await dialog.ShowAsync();
                        });
                    }
                    if ((notificationsRet & 0x08) > 0)
                    { band.onActivityDataCallback = properties.activityDataValueChanged; }
                    else
                    {
                        Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                        {
                            MessageDialog dialog = new MessageDialog(loader.GetString("NotifError4"), loader.GetString("ErrorTitle"));
                            await dialog.ShowAsync();
                        });
                    }
                    /*
                    if ((notificationsRet & 0x10) > 0)
                    { band.onSensorDataCallback = properties.sensorDataValueChanged; }
                    else
                    {
                       Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => { 
                        MessageDialog dialog = new MessageDialog(loader.GetString("NotifError5"), loader.GetString("ErrorTitle"));
                        await dialog.ShowAsync();
                       });
                    }
                    */
                }
                else
                {
                    Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                    {
                        MessageDialog dialog = new MessageDialog("error al activar las notificaciones", loader.GetString("ErrorTitle"));
                        await dialog.ShowAsync();
                    });
                }
                #endregion
                bool ret = await band.SetUnconfigurables();
                updateInfoFromBand();
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    properties.BGLoading = false;
                    properties.BGLoadingV = Visibility.Collapsed;
                });
                MiBand.Ready = true;
            }
        }
        private void updateInfoFromSettings()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            if (localSettings.Containers.ContainsKey("MiBand"))
            {
                var settings = localSettings.Containers["MiBand"];
                // datos de la band
                properties.BandName = (string)settings.Values["Name"];
                properties.MACAddress = (string)settings.Values["MAC"];
                // datos de la bateria
                BatteryInfo battery = BatteryInfo.FromSetting((ApplicationDataCompositeValue)settings.Values["BatteryInfo"]);
                properties.Batterylevel = battery.Level + "%";
                properties.BatteryState = battery.Status.ToString();
                properties.LastCharged = battery.LastChargedS;
                // datos de los pasos
                properties.Goal = (int)settings.Values["DailyGoal"];
                properties.CurrentSteps = (int)settings.Values["CurrentSteps"];

                // datos de alarmas
                properties.Alarm1 = Alarm.FromSetting((ApplicationDataCompositeValue)settings.Values["Alarm1"]);
                properties.Alarm2 = Alarm.FromSetting((ApplicationDataCompositeValue)settings.Values["Alarm2"]);
                properties.Alarm3 = Alarm.FromSetting((ApplicationDataCompositeValue)settings.Values["Alarm3"]);
                tgAlarm1.DataContext = properties.Alarm1;
                tgAlarm2.DataContext = properties.Alarm2;
                tgAlarm3.DataContext = properties.Alarm3;
                txtAlarm1.DataContext = properties.Alarm1;
                txtAlarm2.DataContext = properties.Alarm2;
                txtAlarm3.DataContext = properties.Alarm3;
                // datos de localizacion y colores
                properties.ColorTheme = ColorTheme.FromInt32((int)settings.Values["ColorTheme"]);
                properties.WearLocation = (WearLocation)settings.Values["WearLocation"];

                // datos del dispositivo
                properties.DeviceInfo = DeviceInfo.FromSetting((ApplicationDataCompositeValue)settings.Values["DeviceInfo"]);
            }
        }

        private async void updateInfoFromBand()
        {
            if (band != null)
            {
                // datos de la band
                var bandName = await band.GetName();
                var HoraSincronizada = (await band.GetDateTime()).ToString();
                // datos de la bateria
                var battery = await band.GetBatteryInfo();
                // datos de los pasos
                var currentSteps = await band.GetCurrentSteps();
                // datos del dispositivo
                var deviceInfo = await band.GetDeviceInfo();
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    properties.BandName = bandName;
                    properties.HoraSincronizada = HoraSincronizada;
                    properties.Batterylevel = battery.Level + "%";
                    properties.BatteryState = battery.Status.ToString();
                    properties.LastCharged = battery.LastChargedS;
                    properties.CurrentSteps = currentSteps;
                    properties.Goal = (int)band.SettingGet("DailyGoal");
                    properties.DeviceInfo = deviceInfo;
                });
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter != null)
            {
                string parameter = e.Parameter as string;
                if (parameter == "InitialSetup")
                {
                    if (properties.Loading)
                    {

                        updateInfoFromSettings();
                        finishSetup();
                        var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                        MessageDialog dialog = new MessageDialog(loader.GetString("InitialConfigWarningText"), loader.GetString("InitialConfigWarningTitle"));
                        await dialog.ShowAsync();
                    }
                    else
                    {
                        updateInfoFromSettings();
                    }
                }
            
                if (parameter == "FromSetAlarm")
                {
                    properties.Alarm1 = Alarm.FromSetting((ApplicationDataCompositeValue)band.Settings.Values["Alarm1"]);
                    properties.Alarm2 = Alarm.FromSetting((ApplicationDataCompositeValue)band.Settings.Values["Alarm2"]);
                    properties.Alarm3 = Alarm.FromSetting((ApplicationDataCompositeValue)band.Settings.Values["Alarm3"]);
                    tgAlarm1.DataContext = properties.Alarm1;
                    tgAlarm2.DataContext = properties.Alarm2;
                    tgAlarm3.DataContext = properties.Alarm3;
                    txtAlarm1.DataContext = properties.Alarm1;
                    txtAlarm2.DataContext = properties.Alarm2;
                    txtAlarm3.DataContext = properties.Alarm3;
                }
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
        }

        private void txtBandName2_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (MiBand.Ready)
            {
                tapCallback = setBandName;
                InputDialogBox.Text = "";
                /*
                InputScope scope = new InputScope();
                InputScopeName name = new InputScopeName();
                name.NameValue = InputScopeNameValue.AlphanumericFullWidth;
                scope.Names.Add(name);
                InputDialogBox.InputScope = scope;
                */
                FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
            }
            else
            {
                showNotReadyMessage();
            }
        }
        private void txtCurrentSteps2_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (MiBand.Ready)
            {
                tapCallback = setCurrentSteps;
                InputDialogBox.Text = "";
                /*
                InputScope scope = new InputScope();
                InputScopeName name = new InputScopeName();
                name.NameValue = InputScopeNameValue.Number;
                scope.Names.Add(name);
                InputDialogBox.InputScope = scope;
                */
                FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
            }
            else
            {
                showNotReadyMessage();
            }
        }
        private void txtGoal2_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (MiBand.Ready)
            {
                this.Frame.Navigate(typeof(SetDailyGoal), "FromMain");
            }
            else
            {
                showNotReadyMessage();
            }
        }
        private async void txtBatteryLevel_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (MiBand.Ready)
            {
                // datos de la bateria
                BatteryInfo battery = await band.GetBatteryInfo();
                properties.Batterylevel = battery.Level + "%";
                properties.BatteryState = battery.Status.ToString();
                properties.LastCharged = battery.LastChargedS;
            }
        }
        private void txtThemeColor_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (MiBand.Ready)
            {
                FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
            }
            else
            {
                showNotReadyMessage();
            }
        }
        private void txtCurrentLocation_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (MiBand.Ready)
            {
                FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
            }
            else
            {
                showNotReadyMessage();
            }
        }

        private void InputDialogButton_Click(object sender, RoutedEventArgs e)
        {
            InputDialogFlyout.Hide();
            if (tapCallback != null)
            {
                tapCallback(InputDialogBox.Text);
            }
        }

        private async void setBandName(string NewName)
        {
            if (MiBand.Ready)
            {
                var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                if (NewName.Length <= 10)
                {
                    // da error pero se deja escribir?
                    bool ret = await band.SetName(NewName);
                    if (ret)
                    {
                        properties.BandName = NewName;
                    }
                    else
                    {
                        MessageDialog dialog = new MessageDialog(loader.GetString("SetNameError"), loader.GetString("ErrorTitle"));
                        await dialog.ShowAsync();
                    }
                }
                else
                {
                    MessageDialog dialog = new MessageDialog(loader.GetString("SetNameMaxChars"), loader.GetString("ErrorTitle"));
                    await dialog.ShowAsync();
                }
            }
        }
        private async void setCurrentSteps(string CurrentStepsS)
        {
            if (MiBand.Ready)
            {
                var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                Int32 CurrentSteps = 0;
                if (Int32.TryParse(CurrentStepsS, out CurrentSteps))
                {
                    bool ret = await band.SetCurrentSteps(CurrentSteps);
                    if (ret)
                    {
                        properties.CurrentSteps = CurrentSteps;
                    }
                    else
                    {
                        MessageDialog dialog = new MessageDialog(loader.GetString("SetStepsError"), loader.GetString("ErrorTitle"));
                        await dialog.ShowAsync();
                    }
                }
                else
                {
                    MessageDialog dialog = new MessageDialog(loader.GetString("SetStepsInvalid"), loader.GetString("ErrorTitle"));
                    await dialog.ShowAsync();
                }
            }
        }
        private async void setGoal(string StepsGoalS)
        {
            if (MiBand.Ready)
            {
                var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                Int32 StepsGoal = 0;
                if (Int32.TryParse(StepsGoalS, out StepsGoal))
                {
                    bool ret = await band.SetGoal(StepsGoal);
                    if (ret)
                    {
                        properties.Goal = StepsGoal;
                    }
                    else
                    {
                        MessageDialog dialog = new MessageDialog(loader.GetString("SetGoalError"), loader.GetString("ErrorTitle"));
                        await dialog.ShowAsync();
                    }
                }
                else
                {
                    MessageDialog dialog = new MessageDialog(loader.GetString("SetGoalInvalid"), loader.GetString("ErrorTitle"));
                    await dialog.ShowAsync();
                }
            }
        }
        private async void setLocation_Click(object sender, RoutedEventArgs e)
        {
            if (MiBand.Ready)
            {
                object tag = ((MenuFlyoutItem)sender).Tag;
                WearLocation location = (WearLocation)Int32.Parse((string)tag);
                bool ret = await band.SetWearLocation(location);
                if (ret)
                {
                    properties.WearLocation = location;
                }
                else
                {
                    var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                    MessageDialog dialog = new MessageDialog(loader.GetString("SetWearLocationError"), loader.GetString("ErrorTitle"));
                    await dialog.ShowAsync();
                }
            }
        }
        private async void setColorTheme_Click(object sender, RoutedEventArgs e)
        {
            if (MiBand.Ready)
            {
                string tag = ((MenuFlyoutItem)sender).Tag as string;
                ColorTheme theme = ColorTheme.FromInt32(Int32.Parse(tag , System.Globalization.NumberStyles.HexNumber));
                bool ret = await band.SetColorTheme(theme, true);
                if (ret)
                {
                    properties.ColorTheme = theme;
                }
                else
                {
                    var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                    MessageDialog dialog = new MessageDialog(loader.GetString("SetThemeColorError"), loader.GetString("ErrorTitle"));
                    await dialog.ShowAsync();
                }
            }
        }

        private void btnChangeUserInfo_Click(object sender, RoutedEventArgs e)
        {
            if (MiBand.Ready)
            {
                this.Frame.Navigate(typeof(Pages.SetUserInfo), "FromMain");
            }
            else
            {
                showNotReadyMessage();
            }
        }

        private void tgAlarm_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (MiBand.Ready)
            {
                string ids = ((ToggleSwitch)sender).Name as string;
                if (ids == null) { return; }

                Alarm alarm = null;
                if (ids == "tgAlarm1") { alarm = properties.Alarm1; }
                if (ids == "tgAlarm2") { alarm = properties.Alarm2; }
                if (ids == "tgAlarm3") { alarm = properties.Alarm3; }

                this.Frame.Navigate(typeof(SetAlarm), alarm);
            }
            else
            {
                showNotReadyMessage();
            }
        }

        private async void tgAlarm_Toggled(object sender, RoutedEventArgs e)
        {
            if (MiBand.Ready)
            {
                string ids = ((ToggleSwitch)sender).Name as string;
                if (ids == null) { return; }

                Alarm alarm = null;
                if (ids == "tgAlarm1") { alarm = properties.Alarm1; }
                if (ids == "tgAlarm2") { alarm = properties.Alarm2; }
                if (ids == "tgAlarm3") { alarm = properties.Alarm3; }

                if (band != null & band.Authenticated)
                {
                    bool ret = await band.SetAlarm(alarm);
                }
            }
            else
            {
                if (!properties.BGLoading)
                {
                    showNotReadyMessage();
                    // FIXME aun asi se mueve el toggle
                }
            }
        }

        private async void btnDeleteSettings_Click(object sender, RoutedEventArgs e)
        {
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
            MessageDialog dialog = new MessageDialog(loader.GetString("DeleteInfoConfirmText"), loader.GetString("DeleteInfoConfirmTitle"));
            var yes = new UICommand(loader.GetString("DeleteInfoConfirmYes")); yes.Id = 0;
            var no = new UICommand(loader.GetString("DeleteInfoConfirmNo")); no.Id = 1;
            dialog.Commands.Add(yes);
            dialog.Commands.Add(no);
            dialog.DefaultCommandIndex = 1;
            dialog.CancelCommandIndex = 1;
            IUICommand command = await dialog.ShowAsync();
            if ((int)command.Id == 0)
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                if (localSettings.Containers.ContainsKey("MiBand"))
                {
                    localSettings.DeleteContainer("MiBand");
                    MessageDialog dialog2 = new MessageDialog(loader.GetString("DeleteInfoSuccessText"), loader.GetString("DeleteInfoSuccessTitle"));
                    await dialog2.ShowAsync();
                    Application.Current.Exit();
                }
            }
        }

        private void ExtraCommands_Locate(object sender, RoutedEventArgs e)
        {
            if (MiBand.Ready)
            {
                band.StartVibration(Vibration.WithLeds);
            }
            else
            {
                showNotReadyMessage();
            }
        }

        private void ExtraCommands_Reboot(object sender, RoutedEventArgs e)
        {
            if (MiBand.Ready)
            {
                band.Reboot();
            }
            else
            {
                showNotReadyMessage();
            }
        }

        private void ExtraCommands_SelfTest(object sender, RoutedEventArgs e)
        {
            if (MiBand.Ready)
            {
                band.SelfTest();
            }
            else
            {
                showNotReadyMessage();
            }
        }

        private void ExtraCommands_FactoryReset(object sender, RoutedEventArgs e)
        {
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
            MessageDialog dialog = new MessageDialog(loader.GetString("HardResetUnavailable"), "Nope");
            dialog.ShowAsync();
        }

        private async void appbtnForum_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("http://foro.microsoftinsider.es/index.php?threads/miband-tracker.609/"));
        }

        private async void appbtnReport_Click(object sender, RoutedEventArgs e)
        {
            EmailRecipient sendTo = new EmailRecipient() {  Name = "Soporte MiBand Tracker", Address = "contacto@dahrkael.net" };

            EmailMessage mail = new EmailMessage();
            mail.Subject = "Reporte sobre MiBand Tracker";
            mail.Body = "";
            mail.To.Add(sendTo);

            await EmailManager.ShowComposeNewEmailAsync(mail);
        }

        private async void btnSleepTracking_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void btnStepsTracking_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(Pages.ActivityTracking), "FromMain");
        }

        private async void showNotReadyMessage()
        {
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
            MessageDialog dialog = new MessageDialog(loader.GetString("BandNotReadyText"), loader.GetString("BandNotReadyTitle"));
            await dialog.ShowAsync();
        }

        private async void btnSyncTracking_Click(object sender, RoutedEventArgs e)
        {
            if (MiBand.Ready)
            {
                bool ret = await band.SyncData();
                MessageDialog dialog = new MessageDialog("New data ready", "Sync complete");
                await dialog.ShowAsync();
            }
            else
            {
                showNotReadyMessage();
            }
        }
    }
}
