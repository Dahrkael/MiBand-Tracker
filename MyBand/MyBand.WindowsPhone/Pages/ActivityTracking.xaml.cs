using MyBand.Entities.ActivityTracking.Steps;
using MyBand.Entities.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

namespace MyBand.Pages
{
    public sealed partial class ActivityTracking : Page
    {
        private StepsPeriod currentSelected = new StepsPeriod();

        private ObservableDictionary dataBindings = new ObservableDictionary();
        public ObservableDictionary DataBindings
        {
            get { return dataBindings; }
        }
        

        public ActivityTracking()
        {
            this.InitializeComponent();

            reloadChart();
            DataBindings.Add("CurrentDay", currentSelected);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        private void reloadChart()
        {
            // cargamos los periodos de los ultimos siete dias
            List<StepsPeriod> periods = DB.GetStepsPeriods(DateTime.Today.AddDays(-7), DateTime.Today);

            // referencia para escalar las barras luego
            int maxSteps = 0;
            for (int i = 0; i < 7; i++)
            {
                // buscamos si hay un periodo o no guardado
                var period = periods.Find(p => p.Start == DateTime.Today.AddDays(-i));
                Bar bar = new Bar();
                bar.Date = DateTime.Today.AddDays(-i).Date;
                bar.Period = period;
                bar.ScaleY = 0.0;
                // actualizamos los pasos maximos de estos dias para el escalado
                if (period != null) { maxSteps = period.TotalSteps > maxSteps ? period.TotalSteps : maxSteps; }
                // FIXME no se si el acceso por clave al diccionario crea una entrada si no existe
                if (DataBindings.ContainsKey("Bar"+(i+1)))
                {
                    DataBindings["Bar" + (i + 1)] = bar;
                }
                else
                {
                    DataBindings.Add("Bar" + (i + 1), bar);
                }
                currentSelected = bar.Period;
            }
            // una vez asignadas las barras, calculamos el escalado
            for (int i = 0; i < 7; i++)
            {
                Bar bar = ((Bar)DataBindings["Bar" + (i + 1)]);
                if (bar.Period != null)
                {
                    bar.ScaleY = (bar.Period.TotalSteps * 100) / maxSteps;
                }
            }
        }

        private void Rectangle_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var selected = ((Bar)DataBindings[((Rectangle)sender).Name]).Period;
            currentSelected = selected;
            DataBindings["CurrentDay"] = currentSelected;
        }
    }

    class Bar
    {
        public DateTime Date { get; set; }
        public StepsPeriod Period { get; set; }
        public double ScaleY { get; set; }
    }
}
