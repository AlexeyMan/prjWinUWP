using Microsoft.Toolkit.Uwp.UI.Animations;
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
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// Шаблон элемента пустой страницы задокументирован по адресу http://go.microsoft.com/fwlink/?LinkId=234238

namespace scadaPN
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    
    public sealed partial class homePage : Page
    {
        public class Speeker
        {
            public int age = 4;
            public string name = "Вовка";
        }
        Speeker Sp = new Speeker();
        TestViewModel VM = new TestViewModel();
        DispatcherTimer timer;
        public homePage()
        {
            this.InitializeComponent();
            LoadViewsModel();
            //Таймер
            //this.InitializeComponent();
            //timer = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 1) }; // 1 секунда
            //timer.Tick += Timer_Tick;
            //timer.Start();

        }
        

        private void Timer_Tick(object sender, object e)
        {
            miniGen.Source = new BitmapImage(new Uri("ms-appx:///assets/generator.png"));
            // timer.Stop();
        }
        private void LoadViewsModel()
        {
            VM.Load();
        }
        private void button_Click(object sender, RoutedEventArgs e)
        {
            VM.ForGen[0].Temperature += 10000;
            VM.ForGen[0].Pressure += 5000;
            // miniGen.Source = new ImageSource("ms-appx:///assets/mains3d.png;");
            miniGen.Source = new BitmapImage(new Uri("ms-appx:///assets/gense6t.png"));
            miniGen.Source = new BitmapImage(new Uri("ms-appx:///assets/mains3D.png"));
           
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            miniGen.Source = new BitmapImage(new Uri("ms-appx:///assets/gense6t.png"));
            miniGen.Source = new BitmapImage(new Uri("ms-appx:///assets/generator.png"));
        }
        //protected override CreateParams CreateParams
        //{
        //    get
        //    {
        //        CreateParams cp = base.CreateParams;
        //        cp.ExStyle |= 0x02000000;  // Turn on WS_EX_COMPOSITED
        //        return cp;
        //    }
        //}
        private  void button2_Click(object sender, RoutedEventArgs e)
        {
            //miniGen.Source = new BitmapImage(new Uri("ms-appx:///assets/gense6t.png")); 
            miniGen.Source = new BitmapImage(new Uri("ms-appx:///Assets/smb_Service_anim.gif"));
       

           // await test_fade.Rotate(value: 30f, duration: 0.3).StartAsync();


            //DiscretePictureElement element0 = new DiscretePictureElement(miniGen, 1);

            //element0.m_ImageStates = new List<List<ImageSource>>();
            //List<ImageSource> state0 = new List<ImageSource>();
            //state0.Add(new BitmapImage(new Uri("ms-appx:///assets/mains3D.png")));
            //element0.m_ImageStates.Add(state0);

            //List<ImageSource> state1 = new List<ImageSource>();
            //state1.Add(new BitmapImage(new Uri("ms-appx:///assets/genset.png")));
            //element0.m_ImageStates.Add(state1);

            //element0.Value = 0;
        }
    }

    //class DiscretePictureElement
    //{
    //    public DiscretePictureElement(Image image, int interval)
    //    {
    //        m_Image = image;
    //        m_Interval = interval;
    //        m_Timer = new DispatcherTimer() { Interval = new TimeSpan(0, 0, m_Interval) };
    //    }
    //    ~DiscretePictureElement()
    //    {
    //        if (m_Timer.IsEnabled)
    //        {
    //            m_Timer.Stop();
    //            m_Timer = null;
    //        }
    //    }

    //    public int Value
    //    {
    //        get { return m_Value; }
    //        set
    //        {
    //            if (value < m_ImageStates.Count && value >= 0 && m_Value != value)
    //            {
    //                if (m_Timer.IsEnabled)
    //                   m_Timer.Stop();

    //                m_Value = value;
    //                if (m_ImageStates[m_Value].Count > 1)
    //                {
    //                    m_CurrSubState = 0;
    //                    m_Timer.Tick += this.Timer_Tick;
    //                    m_Timer.Start();
    //                }
    //                else
    //                {
    //                    m_Image.Source = m_ImageStates[m_Value][0];
    //                }
    //            } 
    //        }
    //    }

    //    private void Timer_Tick(object sender, object e)
    //    {
    //        if (m_CurrSubState < m_ImageStates[m_Value].Count)
    //        {
    //            m_CurrSubState++;
    //            m_Image.Source = m_ImageStates[m_Value][m_CurrSubState];
    //        }
    //        else
    //            m_Image.Source = m_ImageStates[m_Value][0];
    //    }

    //    public Image m_Image;
    //    public List<List<ImageSource>> m_ImageStates;
    //    public int m_Interval;
    //    protected int m_CurrSubState;
    //    protected DispatcherTimer m_Timer;
    //    protected int m_Value = -1;
    //}
}
