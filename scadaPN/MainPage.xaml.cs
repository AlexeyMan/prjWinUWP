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

// Документацию по шаблону элемента "Пустая страница" см. по адресу http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace scadaPN
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>

    public sealed partial class MainPage : Page
    {
        TestViewModel VM = new TestViewModel();
        public MainPage()
        {
            this.InitializeComponent();
            LoadViewsModel();
            textHeader.Text ="Главная страница";
            myFrame.Navigate(typeof(homePage));
           // DataContext = VM;
        }

        private void myFrame_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            
        }

        private void LoadViewsModel()
        {
            VM.Load();
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
           
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //await VM.Load();
        }


        private void button_Click(object sender, RoutedEventArgs e)
        {
            VM.ForGen[0].Temperature += 10000;
            VM.ForGen[0].Pressure += 5000;
        }

        private void btn_menu_Click(object sender, RoutedEventArgs e)
        {
            menuSplitView.IsPaneOpen = !menuSplitView.IsPaneOpen;
        }

        private void menuListBox_selectionChange(object sender, SelectionChangedEventArgs e)
        {
            if (ItemHome.IsSelected)
            {
                myFrame.Navigate(typeof(homePage));
                textHeader.Text ="Главная страница";
            }
            if (ItemDGU1.IsSelected)
            {
                myFrame.Navigate(typeof(Char));
                textHeader.Text = "Графики";
            }
            if (ItemDGU2.IsSelected)
            {
                myFrame.Navigate(typeof(fullData));
                textHeader.Text = "Генератор";
            }
        }

        private void btn_exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        private void btn_menu2_Click(object sender, RoutedEventArgs e)
        {
            myFrame.Navigate(typeof(homePage));
        }

        private void comboDeviceNumber_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectDev = comboDeviceNumber.SelectedIndex.ToString();
            /// selectDev = comboDeviceNumber.SelectedValue.ToString();
            //if(a>3)
            // OpenOptionsFile();
            //  a++;

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            ro.OpenOptionsFile(this);
            //ReadOptions();
        }

        private void TextBlock_Tapped(object sender, TappedRoutedEventArgs e)
        {
            //Открываем "Настройка сом порта"
            dialogBox1.ShowAt((ListBoxItem)sender);
            ////Открываем файл с настройками
            //OpenOptionsFile();
        }

        private void saveBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        public string selectDev = "0";
        private RootObjectForConfig ro = new RootObjectForConfig();
    }
}
