using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Шаблон элемента пустой страницы задокументирован по адресу http://go.microsoft.com/fwlink/?LinkId=234238

namespace scadaPN
{
    public class Param
    {
        public string Value { get; set; }
        public string Maxsize
        {
            get;
            set;
        }
    }

    public class MenuItem
    {
        public string IName
        {
            get;
            set;
        }
        public List<Param> Params
        {
            get { return m_Params;  }
        }

        public List<Param> m_Params = new List<Param>();

    }
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class fullData : Page
    {
        public fullData()
        {
            Param ttt = new Param();
            this.InitializeComponent();
            VM1.FillDevices(new SyncForParameterValue(SynchronizationContext.Current, UpdateParameter));
            ttt.Maxsize = "300";
           
            //VM1.DM.Devices[0].Params
            //ObservableCollection<MenuItem> items = new ObservableCollection<MenuItem>();
            //for (int i = 0; i < 25; i++)
            //{
            //    MenuItem item = new MenuItem();
            //    Param param0 = new Param();
            //    param0.Value = "111";
            //    item.m_Params.Add(param0);

            //    Param param1 = new Param();
            //    param1.Value = "222";
            //    item.m_Params.Add(param1);
            //    items.Add(item);
                //items.Add(new MenuItem()
                //{
                //    //IName = "ms-appx:///Assets//energy.jpg"
                //    Value = "ms-appx:///Assets//genset.png"
                //});
            //}

           control.ItemsSource = VM1.DM.Devices;
        }

        private void UpdateParameter(object state)
        {
            ((Parameter)state).NotifyPropertyChanged();
        }

        public string Maxsize
        {
            get;
            set;
        }

        private ViewsMode VM1 = new ViewsMode();

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            VM1.Destroy();
        }
    }
}
