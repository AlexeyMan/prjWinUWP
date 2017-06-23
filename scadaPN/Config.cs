using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Storage;
using Windows.Devices.SerialCommunication;
using Windows.Devices.Enumeration;
using System.Net;
using System.IO;

// Документацию по шаблону элемента "Пустая страница" см. по адресу http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace scadaPN
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>

    //  public ObservableCollection<OptionFile> ForGen2 { get; set; } = new ObservableCollection<EventGen>();
    public class DeviceForConfig
    {
        public string deviceId { get; set; }
        public string slaveId { get; set; }
        public string typeD { get; set; }
        public string conn { get; set; }
        public string timeOut { get; set; }
        public string interval { get; set; }
        public string period { get; set; }

    }

    public class ConnectionForConfig
    {
        public string typeConn { get; set; }
        public string Ncom { get; set; }
        public string speed { get; set; }
        public string bit { get; set; }
        public string parity { get; set; }
        public string stopBit { get; set; }
        public string IP { get; set; }
        public string portTcp { get; set; }
    }

    public class RootObjectForConfig
    {

        //public List<Device> device 
        // {
        //     get { return Device; }
        //     set { }
        // }

        public List<DeviceForConfig> Device = new List<DeviceForConfig>();
        //public List<Connection> Connections { get; set; }
        public List<ConnectionForConfig> Connections = new List<ConnectionForConfig>();
        public void wrList()
        {
            Device.Add(oDev);
            Connections.Add(ccc);

        }

        //Считываем настройки
        public void ReadOptions(MainPage page)
        {
            //  RootObject ro = new RootObject();
            oDev.slaveId = page.textRowSlaveId.Text;
            oDev.deviceId = page.textRowDeviceId.Text;
            // oDev.deviceId = comboDeviceNumber.SelectedItem.ToString();
            oDev.timeOut = page.textRowTimeOut.Text;
            oDev.interval = page.textRowInterval.Text;
            oDev.period = page.textRowPeriod.Text;
            ccc.Ncom = page.ComboBoxCom.SelectedItem.ToString();
            ccc.speed = "";
            ccc.bit = "";
            ccc.parity = "";
            ccc.stopBit = "";
            ccc.typeConn = "";
            ccc.IP = page.textRowIpAdres.Text;
            ccc.portTcp = page.textRowPortIP.Text;

            if (page.s9600.IsChecked == true)
                ccc.speed = "9600";
            else if (page.s19200.IsChecked == true)
                ccc.speed = "19200";
            else if (page.s38400.IsChecked == true)
                ccc.speed = "38400";
            else if (page.s57600.IsChecked == true)
                ccc.speed = "57600";
            else if (page.s115200.IsChecked == true)
                ccc.speed = "115200";

            if (page.bit7.IsChecked == true)
                ccc.bit = "7";
            else if (page.bit8.IsChecked == true)
                ccc.bit = "8";

            if (page.parityNone.IsChecked == true)
                ccc.parity = "none";
            else if (page.parityOdd.IsChecked == true)
                ccc.parity = "Odd";
            else if (page.parityEven.IsChecked == true)
                ccc.parity = "Even";
            if (page.stopBit2.IsChecked == true)
                ccc.stopBit = "2";
            else if (page.stopBit1.IsChecked == true)
                ccc.stopBit = "1";

            wrList();

        }

        public static DeviceForConfig oDev = new DeviceForConfig();
        public static ConnectionForConfig ccc = new ConnectionForConfig();
        //public static RootObjectForConfig ro = new RootObjectForConfig();

        public static string ReadConfigFile()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            string path = localFolder.Path + "\\config.ini";
            string text = "";

            if (File.Exists(path))
            {
                // Open the file to read from.
                using (StreamReader sr = File.OpenText(path))
                {
                    string s = "";
                    while ((s = sr.ReadLine()) != null)
                    {
                        text += s;
                    }
                }
            }

            return text;
        }

        // Заполняем DataModel данными
        public static void LoadConfigSystemFromFile(DataModel dm, SyncForParameterValue syncForParameterValue)
        {
            //const string RS_485 = "RS485";
            //const string TCP_IP = "TCPIP";
            const string PN_PANEL = "PN";
            const string PW_PANEL = "PW";

            string slaveId = "";
            string deviceId = "";
            string TimeOut = "";
            string interval = "";
            string period = "";
            string Ncom = "";
            string speed = "";
            string bit = "";
            string parity = "";
            string stopBit = "";
            string typeConn = "";
            string IP = "";
            string portTcp = "";
            string typeD = "";
            string conn = "";
            string nameConn = "";

            ////Открываем локальную папку
            //StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            //// получаем файл
            //StorageFile configFile = await localFolder.GetFileAsync("config.ini");
            //// читаем файл
            //string text = await FileIO.ReadTextAsync(configFile);

            string text = ReadConfigFile();

            //Десериализуем Json формат 
            dynamic xxx = Newtonsoft.Json.JsonConvert.DeserializeObject(text);

            int count = 0;
            foreach (object d in xxx.Connections)
            {
                typeConn = xxx.Connections[count].typeConn;
                Ncom = $"{xxx.Connections[count].Ncom}";
                speed = $"{xxx.Connections[count].speed}";
                bit = $"{xxx.Connections[count].bit}";
                parity = $"{xxx.Connections[count].parity}";
                stopBit = $"{xxx.Connections[count].stopBit}";
                IP = xxx.Connections[count].IP;
                portTcp = xxx.Connections[count].portTcp;
                nameConn = xxx.Connections[count].name;
                count++;

                if (typeConn == "COM")
                {
                    // Создаём точку подключения через COM по шине RS-485
                    Connection connection = new Connection();

                    connection.m_Name = nameConn;

                    // Создаём экземпляр класса InterfaceRS485 для работы с COM
                    connection.m_Interface = new InterfaceRS485();

                    connection.m_Interface.InterfaceParams.Add(Convert.ToInt32(Ncom)); // номер COM-порта

                    connection.m_Interface.InterfaceParams.Add(Convert.ToInt32(speed)); // скорость
                    connection.m_Interface.InterfaceParams.Add(Convert.ToInt32(bit)); // бит данных
                    if (parity == "Odd")
                        connection.m_Interface.InterfaceParams.Add(1); // паритет
                    else if (parity == "Even")
                        connection.m_Interface.InterfaceParams.Add(2); // паритет
                    else
                        connection.m_Interface.InterfaceParams.Add(0); // паритет
                    connection.m_Interface.InterfaceParams.Add(Convert.ToInt32(stopBit)); // стоп бит

                    dm.AddConnection(connection);
                }
                else
                {
                    int port = Convert.ToInt32(portTcp);
                    int[] ipAsInt = new int[4];
                    bool isValidIP = false;

                    IPAddress _ip;
                    if (IPAddress.TryParse(IP, out _ip) == true)
                    {
                        byte[] bytes = _ip.GetAddressBytes();
                        for (int i = 0; i < ipAsInt.Length; i++)
                            ipAsInt[i] = bytes[i];
                        isValidIP = true;
                    }

                    if (port > 0 && isValidIP)
                    {
                        // Создаём точку подключения через TCP/IP по шине RS-485
                        Connection connection = new Connection();

                        connection.m_Name = nameConn;

                        // Создаём экземпляр класса InterfaceTcpIP
                        connection.m_Interface = new InterfaceTcpIP();

                        connection.m_Interface.InterfaceParams.Add(ipAsInt[0]); // IP:0
                        connection.m_Interface.InterfaceParams.Add(ipAsInt[1]); // IP:1
                        connection.m_Interface.InterfaceParams.Add(ipAsInt[2]); // IP:2
                        connection.m_Interface.InterfaceParams.Add(ipAsInt[3]); // IP:3
                        connection.m_Interface.InterfaceParams.Add(port); // TCP-порт

                        dm.AddConnection(connection);
                    }
                }
            }


            count = 0;
            //Выбор Девайсов из config
            foreach (object d in xxx.Device)
            {
                slaveId = $"{xxx.Device[count].slaveId}";
                deviceId = $"{xxx.Device[count].deviceId}";
                TimeOut = $"{xxx.Device[count].timeOut}";
                interval = $"{xxx.Device[count].interval}";
                period = $"{xxx.Device[count].period}";
                typeD = $"{xxx.Device[count].typeD}";
                conn = $"{xxx.Device[count].conn}";
                count++;

                IDevice device_ = null;
                if (typeD == PW_PANEL)
                {
                    // Создаём экземпляр класса DevicePW для панели PowerWizard
                    device_ = new DevicePW(syncForParameterValue);
                }
                else if (typeD == PN_PANEL)
                {
                    // Создаём экземпляр класса DevicePW для панели PowerWizard
                    device_ = new DevicePN(syncForParameterValue);
                }

                if (device_ != null)
                {
                    device_.Number = Convert.ToInt32(deviceId); // Порядковый номер устройства
                    device_.Address = Convert.ToInt32(slaveId); // Адрес устройства 

                    device_.ParamsForInterface.Clear();
                    device_.ParamsForInterface.Add(Convert.ToInt32(TimeOut)); // Таймаут ответа устройства
                    device_.ParamsForInterface.Add(Convert.ToInt32(interval)); // Таймаут между запросами к устройству
                    device_.ParamsForInterface.Add(Convert.ToInt32(period)); // Таймаут между 2 байтами ответа устройства

                    foreach (Connection connection in dm.Connections)
                    {
                        if (connection.m_Name == conn)
                        {
                            if ((connection.m_Interface as InterfaceRS485) != null)
                                ((ModbusDevice)device_).Protocol = new ProtocolModbusRTU();
                            else if ((connection.m_Interface as InterfaceTcpIP) != null)
                                ((ModbusDevice)device_).Protocol = new ProtocolModbusTCP();

                            if (((ModbusDevice)device_).Protocol != null)
                                connection.m_Devices.Add(device_);

                            break;
                        }
                    }
                }
            }
        }

        public async void OpenOptionsFile(MainPage page)
        {
           
            string slaveId = "";
            string deviceId = "";
            string TimeOut = "";
            string interval = "";
            string period = "";
            string Ncom = "";
            string speed = "";
            string bit = "";
            string parity = "";
            string stopBit = "";
            string typeConn = "";
            string IP = "";
            string portTcp = "";
            string typeD = "";
            string conn = "";

            ////Открываем локальную папку
            //StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            //// получаем файл
            //StorageFile configFile = await localFolder.GetFileAsync("config.ini");
            //// читаем файл
            //string text = await FileIO.ReadTextAsync(configFile);

            string text = ReadConfigFile();

            //Десериализуем Json формат 
            dynamic xxx = Newtonsoft.Json.JsonConvert.DeserializeObject(text);


            var count = -1;
            int cd = 0;
            //Выбор Девайсов из config
            foreach (object d in xxx.Device)
            {
                count++;
                if (page.selectDev == count.ToString())
                {
                    slaveId = $"{xxx.Device[count].slaveId}";
                    deviceId = $"{xxx.Device[count].deviceId}";
                    TimeOut = $"{xxx.Device[count].timeOut}";
                    interval = $"{xxx.Device[count].interval}";
                    period = $"{xxx.Device[count].period}";
                    typeD = $"{xxx.Device[count].typeD}";
                    conn = $"{xxx.Device[count].conn}";
                    cd = count;

                }

            }

            count = -1;
            foreach (object d in xxx.Connections)
            {
                count++;
                typeConn = xxx.Connections[count].typeConn;
                Ncom = $"{xxx.Connections[count].Ncom}";
                speed = $"{xxx.Connections[count].speed}";
                bit = $"{xxx.Connections[count].bit}";
                parity = $"{xxx.Connections[count].parity}";
                stopBit = $"{xxx.Connections[count].stopBit}";
                IP = xxx.Connections[count].IP;
                portTcp = xxx.Connections[count].portTcp;

            }
            //slaveId = $"{xxx.Device[count].slaveId}";
            //    deviceId = $"{xxx.Device[count].deviceId}";
            //    TimeOut = $"{xxx.Device[count].timeOut}";
            //    interval = $"{xxx.Device[count].interval}";
            //    period = $"{xxx.Device[count].period}";
            //    typeD = $"{xxx.Device[count].typeD}";
            //    conn = $"{xxx.Device[count].conn}";
            //    typeConn = xxx.Connections[count].typeConn;
            //    Ncom = $"{xxx.Connections[count].Ncom}";
            //    speed = $"{xxx.Connections[count].speed}";
            //    bit = $"{xxx.Connections[count].bit}";
            //    parity = $"{xxx.Connections[count].parity}";
            //    stopBit = $"{xxx.Connections[count].stopBit}";
            //    IP = xxx.Connections[count].IP;
            //    portTcp = xxx.Connections[count].portTcp;

            // count = 0;
            page.comboDeviceNumber.Items.Clear();
            foreach (object d in xxx.Device)
            {
                count++;
                page.comboDeviceNumber.Items.Add(count);
            }

            if (typeConn == "COM")
            {
                page.connectCom.IsChecked = true;
            }
            else if (typeConn == "TCP")
            {
                page.connectTcp.IsChecked = true;
            };
            if (typeD == "PW")
                page.comboDeviceType.SelectedIndex = 0;
            else
                page.comboDeviceType.SelectedIndex = 1;


            page.textRowSlaveId.Text = slaveId;
            page.textRowTimeOut.Text = TimeOut;
            page.textRowInterval.Text = interval;
            page.textRowPeriod.Text = period;
            page.textRowDeviceId.Text = deviceId;
            page.textRowIpAdres.Text = IP;
            page.textRowPortIP.Text = portTcp;
            page.comboDeviceNumber.SelectedIndex = cd;
            if (speed == "9600")
                page.s9600.IsChecked = true;
            else if (speed == "19200")
                page.s19200.IsChecked = true;
            else if (speed == "38400")
                page.s38400.IsChecked = true;
            else if (speed == "57600")
                page.s57600.IsChecked = true;
            else if (speed == "115200")
                page.s115200.IsChecked = true;
            if (bit == "7")
                page.bit7.IsChecked = true;
            else
                page.bit8.IsChecked = true;
            if (parity == "none")
                page.parityNone.IsChecked = true;
            else if (parity == "Odd")
                page.parityOdd.IsChecked = true;
            else if (parity == "Even")
                page.parityEven.IsChecked = true;
            if (stopBit == "2")
                page.stopBit2.IsChecked = true;
            else
                page.stopBit1.IsChecked = true;

            List<string> namberPort = new List<string>();
            var lk = namberPort.Count;
            page.ComboBoxCom.Items.Clear();
            int first = 0;
            for (int i = 1; i < 10; i++)
            {
                // Берем селектор выбранного сом порта
                string filt = SerialDevice.GetDeviceSelector("COM" + i);
                // Выбираем все устройства с селектором данного сом порта
                DeviceInformationCollection devices = await DeviceInformation.FindAllAsync(filt);

                // Если обнаружено хоть одно устройство
                if (devices.Any())
                {
                    page.ComboBoxCom.Items.Add("COM" + i);
                    if (first == 0)
                    {
                        first = 2;
                        page.ComboBoxCom.SelectedIndex = 0;
                        //string ggh = "COM9" + i;
                        //ComboBoxCom.PlaceholderText = ggh;
                        //string ddd = ComboBoxCom.SelectedIndex.ToString();
                    }
                    namberPort.Add("COM" + i);
                }
            }
        }
    }




    //string filt = SerialDevice.GetDeviceSelector("COM1");
    // string filt1 = SerialDevice.GetDeviceSelector("COM2");
    // DeviceInformationCollection devices = await DeviceInformation.FindAllAsync(filt);

    // myTextBox.Text = text;
    //await new Windows.UI.Popups.MessageDialog("Файл открыт").ShowAsync();


}

