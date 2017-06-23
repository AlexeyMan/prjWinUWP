using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;
using System.IO;


namespace scadaPN
{
   
    public class EventGen : INotifyPropertyChanged
    {
        int temperature;
        double pressure;

        public int Temperature { get { return temperature; } set { temperature = value; OnPropertyChanged();}}
        public double Pressure {get { return pressure; }set {pressure = value; OnPropertyChanged();}}
        //public BitmapImage Icon { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
       public void OnPropertyChanged([CallerMemberName]string prop = "")
       {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
       }
    } 
    class TestViewModel
    {
        public ObservableCollection<EventGen> ForGen { get; set; } = new ObservableCollection<EventGen>();
        public  void  Load()
        {
            ForGen.Add(new EventGen() {
                Temperature = 456,
                Pressure =987
            });

        //    int t1 = await WorkFunction();
        //    int c = 0;
        //}

        //static Task<int> WorkFunction()
        //{
        //    int result = 1;

        //    return Task.Run(() =>
        //    {
        //        for (int i = 1; i <= 1000000; i++)
        //        {
        //            result *= i;
        //        }
        //        return result;
        //    });
        }

    }


}
