using MyBand.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public sealed partial class SetDailyGoal : Page
    {
        private bool initialSetup = false;

        public SetDailyGoal()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if ((e.Parameter as string) == "InitialSetup")
            {
                initialSetup = true;
            }
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
            int goal = 8000;
            string text = txtGoal.Text;
            if (Int32.TryParse(text, out goal))
            {
                bool ret = await MiBand.Band.SetGoal(goal);
                if (!ret)
                {
                    // algo ha ido mal
                    
                    MessageDialog dialog = new MessageDialog(loader.GetString("SetGoalErrorText"), loader.GetString("SetGoalErrorTitle"));
                    await dialog.ShowAsync();
                }
                this.Frame.Navigate(typeof(Main), initialSetup ? "InitialSetup" : null);
            }
            else
            {
                // algo ha ido mal
                MessageDialog dialog = new MessageDialog(loader.GetString("SetGoalRetryText"), loader.GetString("SetGoalErrorTitle"));
                await dialog.ShowAsync();
            }
            
        }
    }
}
