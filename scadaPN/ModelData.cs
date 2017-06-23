using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using scadaPN;
using System.Collections.ObjectModel;
using System.ComponentModel;

using Windows.Devices.SerialCommunication;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
using System.Threading;
using System.Runtime.InteropServices;
using System.IO;
using System.Net.Sockets;
using System.Net;
using Windows.Storage;

namespace scadaPN
{
    /// /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// 
    /// Класс для чтения/записи INI-файлов
    /// 
    /// /////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public class INIManager
    {
        // Конструктор, принимающий путь к INI-файлу
        public INIManager(string aPath)
        {
            path = new FileInfo(aPath).FullName.ToString();
        }

        // Конструктор без аргументов (путь к INI-файлу нужно будет задать отдельно)
        public INIManager() : this("") { }

        // Возвращает значение из INI-файла (по указанным секции и ключу) 
        public string GetPrivateString(string aSection, string aKey)
        {
            // Для получения значения
            StringBuilder buffer = new StringBuilder(SIZE);

            // Получить значение в buffer
            GetPrivateString(aSection, aKey, null, buffer, SIZE, path);
            uint err = GetLastError();

            // Вернуть полученное значение
            return buffer.ToString();
        }

        // Пишет значение в INI-файл (по указанным секции и ключу) 
        public void WritePrivateString(string aSection, string aKey, string aValue)
        {
            // Записать значение в INI-файл
            WritePrivateString(aSection, aKey, aValue, path);
        }

        // Возвращает значение из INI-файла (по указанным секции и ключу) 
        public int GetPrivateInt(string aSection, string aKey, int value_def = 0)
        {
            // Для получения значения
            StringBuilder buffer = new StringBuilder(SIZE);

            // Получить значение в buffer
            return GetPrivateInt(aSection, aKey, value_def, path);
        }

        // Возвращает или устанавливает путь к INI файлу
        public string Path
        {
            get { return path; }
            set { path = value; }
        }

        // Поля класса
        private const int SIZE = 1024; //Максимальный размер (для чтения значения из файла)
        private string path = null; //Для хранения пути к INI-файлу

        // Импорт функции GetPrivateProfileString (для чтения значений) из библиотеки kernel32.dll
        [DllImport("kernel32.dll", EntryPoint = "GetPrivateProfileString")]
        private static extern int GetPrivateString(string section, string key, string def, StringBuilder buffer, int size, string path);

        // Импорт функции WritePrivateProfileString (для записи значений) из библиотеки kernel32.dll
        [DllImport("kernel32.dll", EntryPoint = "WritePrivateProfileString")]
        private static extern int WritePrivateString(string section, string key, string str, string path);

        // Импорт функции GetPrivateProfileInt (для чтения значений) из библиотеки kernel32.dll
        [DllImport("kernel32.dll", EntryPoint = "GetPrivateProfileInt")]
        private static extern int GetPrivateInt(string section, string key, int value_default, string path);

        [DllImport("kernel32.dll", EntryPoint = "GetLastError")]
        private static extern uint GetLastError();
    }

    /// /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// 
    /// Интерфейсы подключения: RS-485, TCP-IP-сеть, ...
    /// 
    /// /////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // Базовый interface для всех типов интерфейсов, который задаёт общие свойства и типы поведения для всех типов интерфейсов
    public interface IInterface
    {
        // МЕТОДЫ:

        // Открыть интерфейс
        bool Open();

        // Проверить соединение
        bool IsOpen();

        // Закрыть интерфейс
        bool Close();

        // Очисть входной буфер интерфейса
        void ClearInBuffer();

        // Очисть выходной буфер интерфейса
        void ClearOutBuffer();

        // Чтение данных из интерфейса
        bool ReadData(ref byte[] data);

        // Запись данных в интерфейс
        bool WriteData(byte[] data);

        // Установить параметры интерфейса устройством
        void SetParamsByDevice(List<int> paramsByDevice);

        // Свойства:

        // Параметры интерфейса (например: для RS-485: m_InterfaceParams[0] - номер COM-порта, m_InterfaceParams[1] - скорость COM-порта, ...)
        List<int> InterfaceParams { get; }
    }

    // Интерфейс Rs-485
    public class InterfaceRS485 : IInterface
    {
        // Список ошибок 
        const string INVALID_INTERFACE_PARAM_COM_ERR = "Некорректное количество параметров интерфейса";
        const string OPEN_COM_ERR = "Ошибка открытия COM{0} порта: {1}";
        const string READ_COM_ERR = "Ошибка чтения из COM{0} порт: {1}";
        const string WRITE_COM_ERR = "Ошибка записи в COM{0} порт: {1}";
        const string CLOSE_COM_ERR = "Ошибка закрытия COM{0} порта: {1}";
        const string UNKNOW_COM_ERR = "Неизвестная ошибка COM порта";

        // МЕТОДЫ:

        // Открыть COM-порт
        public bool Open()
        {
            m_SerialDevice = null;
            m_LastError = "";

            if (m_InterfaceParams.Count < 5)
            {
                m_LastError = INVALID_INTERFACE_PARAM_COM_ERR;
                return false;
            }

            try
            {
                string selector = SerialDevice.GetDeviceSelector("COM" + m_InterfaceParams[0]);
                //return true;
                //Task delay = Task.Delay(1500);
                //delay.Wait();

                Task<DeviceInformationCollection> taskInfo = (Task<DeviceInformationCollection>)DeviceInformation.FindAllAsync(selector).AsTask();

                taskInfo.Wait();
                DeviceInformationCollection devices = taskInfo.Result;

                if (devices.Any())
                {
                    DeviceInformation deviceInfo = devices.First();

                    Task<SerialDevice> taskDevice = (Task<SerialDevice>)SerialDevice.FromIdAsync(deviceInfo.Id).AsTask();

                    taskDevice.Wait();
                    m_SerialDevice = taskDevice.Result;

                    m_SerialDevice.BaudRate = (uint)m_InterfaceParams[1];
                    m_SerialDevice.DataBits = (ushort)m_InterfaceParams[2];

                    if (m_InterfaceParams[3] == 1)
                        m_SerialDevice.StopBits = SerialStopBitCount.One;
                    else
                        m_SerialDevice.StopBits = SerialStopBitCount.Two;

                    switch ((m_InterfaceParams[4]))
                    {
                        case 0:
                        default:
                            m_SerialDevice.Parity = SerialParity.None;
                            break;

                        case 1:
                            m_SerialDevice.Parity = SerialParity.Odd;
                            break;

                        case 2:
                            m_SerialDevice.Parity = SerialParity.Even;
                            break;
                    }

                    m_IsOpened = true;
                    return true;
                }
            }
            catch (Exception e)
            {
                //MessageDialog popup = new MessageDialog("Sorry, no device found.");
                //await popup.ShowAsync();
                m_SerialDevice = null;
                m_IsOpened = false;
                m_LastError = String.Format(OPEN_COM_ERR, m_InterfaceParams[0], e.Message);
                return false;
            }

            m_LastError = UNKNOW_COM_ERR;
            return false;
        }

        // Проверить соединение
        public virtual bool IsOpen()
        {
            //return (m_SerialDevice != null);
            return m_IsOpened;
        }

        // Закрыть COM-порт
        virtual public bool Close()
        {
            m_LastError = "";

            //Ensure port is opened before attempting to close:
            if (IsOpen())
            {
                try
                {
                    m_SerialDevice.Dispose();
                    m_SerialDevice = null;
                    m_IsOpened = false;
                }
                catch (Exception e)
                {

                    m_LastError = String.Format(CLOSE_COM_ERR, m_InterfaceParams[0], e.Message);
                    return false;
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        // Очистить входной буфер интерфейса
        public virtual void ClearInBuffer()
        {
            //m_SerialPort.DiscardOutBuffer();
        }

        // Очистить выходной буфер интерфейса
        public virtual void ClearOutBuffer()
        {
            //m_SerialPort.DiscardOutBuffer();
        }

        // Чтение данных из COM-порта

        public virtual bool ReadData(ref byte[] data)
        {
            m_LastError = "";

            try
            {
                DataReader reader = new DataReader(m_SerialDevice.InputStream);
                CancellationTokenSource cts = new CancellationTokenSource((int)m_Timeouts[(int)Timeouts.Response]);

                Task<uint> task = reader.LoadAsync((uint)data.Length).AsTask(cts.Token);

                task.Wait();
                uint readedBytes = task.Result;

                if (readedBytes > 0)
                {
                    int i = 0;
                    for (i = 0; i < readedBytes; i++)
                        data[i] = reader.ReadByte();

                    if (readedBytes < data.Length)
                    {
                        cts = new CancellationTokenSource((int)m_Timeouts[(int)Timeouts.BetweenRespByte]);
                        task = reader.LoadAsync((uint)data.Length).AsTask(cts.Token);

                        task.Wait();
                        readedBytes = task.Result;

                        for (; i < readedBytes; i++)
                            data[i] = reader.ReadByte();
                    }
                    reader.DetachStream();
                    return true;
                }

                reader.DetachStream();
                reader = null;

                return false;
            }
            catch (Exception e)
            {
                m_LastError = String.Format(READ_COM_ERR, m_InterfaceParams[0], e.Message);
                return false;
            }
        }

        // Запись данных в COM-порт
        public virtual bool WriteData(byte[] data)
        {
            m_LastError = "";

            try
            {
                DataWriter writer = new DataWriter(m_SerialDevice.OutputStream);

                writer.WriteBytes(data);
                //CancellationTokenSource cts = new CancellationTokenSource(m_SerialDevice.WriteTimeout);
                Task task = writer.StoreAsync().AsTask(/*cts.Token*/);

                task.Wait();
                writer.DetachStream();
                writer = null;

                return true;
            }
            catch (Exception e)
            {
                m_LastError = String.Format(WRITE_COM_ERR, m_InterfaceParams[0], e.Message);
                return false;
            }
        }

        // Установить параметры интерфейса устройством
        public virtual void SetParamsByDevice(List<int> paramsByDevice)
        {
            if (paramsByDevice.Count > 0)
            {
                //m_SerialPort.ReadTimeout = paramsByDevice[0];
                for (int i = 0; i < paramsByDevice.Count; i++)
                {
                    if (i < (int)Timeouts.Count)
                        m_Timeouts[i] = (uint)paramsByDevice[i];
                }
            }
        }

        // СВОЙСТВА:

        public List<int> InterfaceParams
        {
            get { return m_InterfaceParams; }
        }

        public string LastError
        {
            get { return m_LastError; }
        }

        // ДАННЫЕ:

        // Параметры интерфейса RS-485: m_InterfaceParams[0] - номер COM-порта, m_InterfaceParams[1] - скорость COM-порта, ...
        private List<int> m_InterfaceParams = new List<int>();

        // Последовательный порт
        private SerialDevice m_SerialDevice;

        private bool m_IsOpened = false;

        public enum Timeouts
        {
            // Таймаут ответа устройства
            Response = 0,

            // Таймаут между запросами к устройству
            BetweenReq = 1,

            // Таймаут между 2 байтами ответа устройства
            BetweenRespByte = 2,

            // Количество таймаутов
            Count = 3
        }

        // Массив таймаутов
        private uint[] m_Timeouts = new uint[(int)Timeouts.Count];

        private string m_LastError = "";
    }

    // Интерфейс TCP-IP
    public class InterfaceTcpIP : IInterface
    {
        // Список ошибок 
        const string INVALID_INTERFACE_PARAM_TCP_ERR = "Некорректное количество параметров интерфейса";
        const string OPEN_TCP_ERR = "Ошибка открытия TCP-соединения: IP={0} порт={1}: {2}";
        const string READ_TCP_ERR = "Ошибка чтения из COM{0} порт: {1}";
        const string WRITE_TCP_ERR = "Ошибка записи в COM{0} порт: {1}";
        const string CLOSE_TCP_ERR = "Ошибка закрытия COM{0} порта: {1}";
        const string UNKNOW_TCP_ERR = "Неизвестная ошибка COM порта";

        // МЕТОДЫ:

        // Открыть TCP-соединение
        virtual public bool Open()
        {
            m_LastError = "";
            tcpSynCl = null;

            if (m_InterfaceParams.Count < 5)
            {
                m_LastError = INVALID_INTERFACE_PARAM_TCP_ERR;
                return false;
            }

            try
            {
                m_IP = m_InterfaceParams[0].ToString() + "." + m_InterfaceParams[1].ToString() + "." + m_InterfaceParams[2].ToString() + "." + m_InterfaceParams[3].ToString();
                m_Port = m_InterfaceParams[4];

                IPAddress _ip;
                if (IPAddress.TryParse(m_IP, out _ip) == false)
                {
                    return false;
                    //IPHostEntry hst = Dns.GetHostEntry(ip);
                    //ip = hst.AddressList[0].ToString();
                }
                // ----------------------------------------------------------------
                // Connect asynchronous client
                //tcpAsyCl = new Socket(IPAddress.Parse(m_IP).AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                //tcpAsyCl.Connect(new IPEndPoint(IPAddress.Parse(m_IP), m_Port));
                //tcpAsyCl.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, _timeout);
                //tcpAsyCl.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, _timeout);
                //tcpAsyCl.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, 1);
                // ----------------------------------------------------------------
                // Connect synchronous client
                tcpSynCl = new Socket(IPAddress.Parse(m_IP).AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                tcpSynCl.Connect(new IPEndPoint(IPAddress.Parse(m_IP), m_Port));
                tcpSynCl.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, _timeout);
                tcpSynCl.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, _timeout);
                tcpSynCl.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, 1);
                m_IsOpened = true;

                return true;
            }
            catch (/*System.IO.IO*/Exception e)
            {
                m_IsOpened = false;
                m_LastError = String.Format(OPEN_TCP_ERR, m_IP, m_Port, e.Message);
                return false;
            }

            m_LastError = UNKNOW_TCP_ERR;
            return false;
        }

        // Проверить соединение
        public virtual bool IsOpen()
        {
            //return (tcpSynCl != null);
            return m_IsOpened;
        }

        // Закрыть TCP-соединение
        virtual public bool Close()
        {
            //if (tcpAsyCl != null)
            //{
            //    if (tcpAsyCl.Connected)
            //    {
            //        try { tcpAsyCl.Shutdown(SocketShutdown.Both); }
            //        catch { }
            //        tcpAsyCl.Dispose();
            //    }
            //    tcpAsyCl = null;
            //}
            m_LastError = "";
            if (tcpSynCl != null)
            {
                if (tcpSynCl.Connected)
                {
                    try { tcpSynCl.Shutdown(SocketShutdown.Both); }
                    catch { }
                    tcpSynCl.Dispose();
                }
                tcpSynCl = null;
                m_IsOpened = false;
            }
            return true;
        }

        // Очистить входной буфер интерфейса
        public virtual void ClearInBuffer()
        {
        }

        // Очистить выходной буфер интерфейса
        public virtual void ClearOutBuffer()
        {
        }

        // Чтение данных из TCP-соединения
        public virtual bool ReadData(ref byte[] data)
        {
            if (tcpSynCl != null)
            {
                try
                {
                    int result = tcpSynCl.Receive(data, 0, data.Length, SocketFlags.None);
                    return true;
                }
                catch (Exception e)
                {
                    m_LastError = String.Format(READ_TCP_ERR, m_IP, m_Port, e.Message);
                    Close();
                    return false;
                }
            }

            return false;
        }

        // Запись данных в TCP-соединение
        virtual public bool WriteData(byte[] data)
        {
            m_LastError = "";
            if (tcpSynCl != null)
            {
                try
                {
                    tcpSynCl.Send(data, 0, data.Length, SocketFlags.None);
                    return true;
                }
                catch (Exception e)
                {
                    m_LastError = String.Format(WRITE_TCP_ERR, m_IP, m_Port, e.Message);
                    Close();
                    return false;
                }
            }

            return false;
        }

        // Установить параметры интерфейса устройством
        public virtual void SetParamsByDevice(List<int> paramsByDevice)
        {
            if (paramsByDevice.Count > 0)
            {
                _timeout = (ushort)paramsByDevice[0];
                tcpSynCl.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, _timeout);
                tcpSynCl.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, _timeout);
                tcpSynCl.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, 1);
            }
        }

        // СВОЙСТВА:

        //public Socket TcpAsyncClient
        //{
        //    get { return tcpAsyCl; }
        //}

        public Socket TcpSyncClient
        {
            get { return tcpSynCl; }
        }

        public List<int> InterfaceParams
        {
            get { return m_InterfaceParams; }
        }

        public string LastError
        {
            get { return m_LastError; }
        }

        // ДАННЫЕ:

        // Параметры интерфейса TCP-IP: m_InterfaceParams[0] - IP адрес, m_InterfaceParams[1] - TCP-порт
        protected List<int> m_InterfaceParams = new List<int>();

        // Private declarations
        private ushort _timeout = 500;
        private bool m_IsOpened = false;

        private static string m_IP;
        private static int m_Port;

        //private Socket tcpAsyCl = null;
        //private byte[] tcpAsyClBuffer = new byte[2048];

        private Socket tcpSynCl = null;
        //private byte[] tcpSynClBuffer = new byte[2048];

        private string m_LastError = "";
    }

    /// /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// 
    /// Протоколы ModbusRTU, ModbusTCP, SNMP
    /// 
    /// /////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // Базовый interface для всех типов протоколов, который задаёт общие свойства и типы поведения для всех типов протоколов
    public interface IProtocol<RETURN_DATA_TYPE>
    {
        // МЕТОДЫ:

        // Запрос данных по протоколу
        RETURN_DATA_TYPE GetData(IInterface protocolInterface, int deviceAddress, IProtocolParamsForOperation protocolParams);
    }

    // Протокол Modbus-RTU
    public class ProtocolModbusRTU : IProtocol<short[]>
    {
        // МЕТОДЫ:
        // Запрос данных по протоколу Modbus-RTU: 
        // protocolInterface.m_InterfaceParams[0] - номер COM-порта
        // protocolInterface.m_InterfaceParams[1] - скорость COM-порта, ...
        // deviceAddress - Modbus-адрес устройства
        // protocolParams[0] - Modbus-адрес
        // protocolParams[1] - количество Modbus-регистров
        public virtual short[] GetData(IInterface protocolInterface, int deviceAddress, IProtocolParamsForOperation protocolParams)
        {
            // Простой вариант: protocolParams[0] - Modbus-адрес, protocolParams[1] - количество Modbus-регистров, protocolParams[2] - тип данных
            byte address = (byte)deviceAddress;
            ushort reg_start = ((ModbusProtocolParamsForOperation)protocolParams).m_RegAddress;
            ushort reg_count = ((ModbusProtocolParamsForOperation)protocolParams).m_RegCount;
            //int data_type = protocolParams[2];

            //List<string> valuesList = new List<string>();

            short[] values = new short[reg_count];
            if (ReadRegistersByFunc3(protocolInterface, address, reg_start, reg_count, ref values))
                return values;
            
            return null;
        }

        #region CRC Computation
        private void GetCRC(byte[] message, ref byte[] CRC)
        {
            //Function expects a modbus message of any length as well as a 2 byte CRC array in which to 
            //return the CRC values:

            ushort CRCFull = 0xFFFF;
            byte CRCHigh = 0xFF, CRCLow = 0xFF;
            char CRCLSB;

            for (int i = 0; i < (message.Length) - 2; i++)
            {
                CRCFull = (ushort)(CRCFull ^ message[i]);

                for (int j = 0; j < 8; j++)
                {
                    CRCLSB = (char)(CRCFull & 0x0001);
                    CRCFull = (ushort)((CRCFull >> 1) & 0x7FFF);

                    if (CRCLSB == 1)
                        CRCFull = (ushort)(CRCFull ^ 0xA001);
                }
            }
            CRC[1] = CRCHigh = (byte)((CRCFull >> 8) & 0xFF);
            CRC[0] = CRCLow = (byte)(CRCFull & 0xFF);
        }
        #endregion

        #region Build Message
        private void BuildMessage(byte address, byte type, ushort start, ushort registers, ref byte[] message)
        {
            //Array to receive CRC bytes:
            byte[] CRC = new byte[2];

            message[0] = address;
            message[1] = type;
            message[2] = (byte)(start >> 8);
            message[3] = (byte)start;
            message[4] = (byte)(registers >> 8);
            message[5] = (byte)registers;

            GetCRC(message, ref CRC);
            message[message.Length - 2] = CRC[0];
            message[message.Length - 1] = CRC[1];
        }
        #endregion

        #region Check Response
        private bool CheckResponse(byte[] response)
        {
            //Perform a basic CRC check:
            byte[] CRC = new byte[2];
            GetCRC(response, ref CRC);
            if (CRC[0] == response[response.Length - 2] && CRC[1] == response[response.Length - 1])
                return true;
            else
                return false;
        }
        #endregion

        #region Function 3 - Read Registers
        private bool ReadRegistersByFunc3(IInterface interface485, byte address, ushort start, ushort registers, ref short[] values)
        {
            //Ensure port is open:
            if (interface485.IsOpen())
            {
                //Clear in/out buffers:
                interface485.ClearOutBuffer();
                interface485.ClearInBuffer();

                //Function 3 request is always 8 bytes:
                byte[] message = new byte[8];

                //Function 3 response buffer:
                byte[] response = new byte[5 + 2 * registers];

                //Build outgoing modbus message:
                BuildMessage(address, (byte)3, start, registers, ref message);

                //Send modbus message to Serial Port and recieve response from Serial Port:
                try
                {
                    interface485.Close();
                    interface485.Open();
                    if (interface485.WriteData(message))
                        interface485.ReadData(ref response);
                }
                catch (Exception err)
                {
                    //modbusStatus = "Error in read event: " + err.Message;
                    return false;
                }

                //Evaluate message:
                if (CheckResponse(response))
                {
                    //Return requested register values:
                    for (int i = 0; i < (response.Length - 5) / 2; i++)
                    {
                        values[i] = response[2 * i + 3];
                        values[i] <<= 8;
                        values[i] += response[2 * i + 4];
                    }
                    //modbusStatus = "Read successful";
                    return true;
                }
                else
                {
                    //modbusStatus = "CRC error";
                    return false;
                }
            }
            else
            {
                //modbusStatus = "Serial port not open";
                return false;
            }

        }
        #endregion
    }

    // Протокол Modbus-TCP
    public class ProtocolModbusTCP : IProtocol<short[]>
    {
        // Constants for access
        //private const byte fctReadCoil = 1;
        //private const byte fctReadDiscreteInputs = 2;
        private const byte fctReadHoldingRegister = 3;
        //private const byte fctReadInputRegister = 4;
        //private const byte fctWriteSingleCoil = 5;
        //private const byte fctWriteSingleRegister = 6;
        //private const byte fctWriteMultipleCoils = 15;
        //private const byte fctWriteMultipleRegister = 16;
        //private const byte fctReadWriteMultipleRegister = 23;

        /// <summary>Constant for exception illegal function.</summary>
        public const byte excIllegalFunction = 1;
        /// <summary>Constant for exception illegal data address.</summary>
        public const byte excIllegalDataAdr = 2;
        /// <summary>Constant for exception illegal data value.</summary>
        public const byte excIllegalDataVal = 3;
        /// <summary>Constant for exception slave device failure.</summary>
        public const byte excSlaveDeviceFailure = 4;
        /// <summary>Constant for exception acknowledge.</summary>
        public const byte excAck = 5;
        /// <summary>Constant for exception slave is busy/booting up.</summary>
        public const byte excSlaveIsBusy = 6;
        /// <summary>Constant for exception gate path unavailable.</summary>
        public const byte excGatePathUnavailable = 10;
        /// <summary>Constant for exception not connected.</summary>
        public const byte excExceptionNotConnected = 253;
        /// <summary>Constant for exception connection lost.</summary>
        public const byte excExceptionConnectionLost = 254;
        /// <summary>Constant for exception response timeout.</summary>
        public const byte excExceptionTimeout = 255;
        /// <summary>Constant for exception wrong offset.</summary>
        private const byte excExceptionOffset = 128;
        /// <summary>Constant for exception send failt.</summary>
        private const byte excSendFailt = 100;

        private ushort m_TransactionID = 0;

        // МЕТОДЫ:

        // Запрос данных по протоколу Modbus-TCP
        public virtual short[] GetData(IInterface protocolInterface, int deviceAddress, IProtocolParamsForOperation protocolParams)
        {
            byte address = (byte)deviceAddress;
            ushort reg_start = ((ModbusProtocolParamsForOperation)protocolParams).m_RegAddress;
            ushort reg_count = ((ModbusProtocolParamsForOperation)protocolParams).m_RegCount;
            //int data_type = protocolParams[2];

            //List<string> valuesList = new List<string>();
            if (m_TransactionID == 0xFFFF)
                m_TransactionID = 1;
            else
                m_TransactionID++;

            short[] values = new short[reg_count];
            if (ReadHoldingRegister((InterfaceTcpIP)protocolInterface, m_TransactionID, address, reg_start, reg_count, ref values))
                return values;

            return null;
        }

        /// <summary>Read holding registers from slave synchronous.</summary>
        /// <param name="id">Unique id that marks the transaction. In asynchonous mode this id is given to the callback function.</param>
        /// <param name="unit">Unit identifier (previously slave address). In asynchonous mode this unit is given to the callback function.</param>
        /// <param name="startAddress">Address from where the data read begins.</param>
        /// <param name="numInputs">Length of data.</param>
        /// <param name="values">Contains the result of function.</param>
        private bool ReadHoldingRegister(InterfaceTcpIP protocolInterface, ushort id, byte unit, ushort startAddress, ushort numInputs, ref short[] values)
        {
            byte[] data = WriteSyncData(protocolInterface, CreateReadHeader(id, unit, startAddress, numInputs, fctReadHoldingRegister), id);

            if (data != null && 2*values.Length == data.Length)
            { 
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = data[2*i];
                    values[i] <<= 8;
                    values[i] += data[2*i + 1];
                }
                return true;
            }

            return false;
        }

        // ------------------------------------------------------------------------
        // Write data and and wait for response
        private byte[] WriteSyncData(InterfaceTcpIP protocolInterface, byte[] write_data, ushort id)
        {
            if (protocolInterface.IsOpen())
            {
                try
                {
                    protocolInterface.WriteData(write_data);

                    byte[] tcpSynClBuffer = new byte[2048];
                    protocolInterface.ReadData(ref tcpSynClBuffer);

                    //int result = tcpSynCl.Receive(tcpSynClBuffer, 0, tcpSynClBuffer.Length, SocketFlags.None);

                    byte unit = tcpSynClBuffer[6];
                    byte function = tcpSynClBuffer[7];
                    byte[] data;

                    //if (result == 0) CallException(id, unit, write_data[7], excExceptionConnectionLost);

                    // ------------------------------------------------------------
                    // Response data is slave exception
                    if (function > excExceptionOffset)
                    {
                        function -= excExceptionOffset;
                        //CallException(id, unit, function, tcpSynClBuffer[8]);
                        return null;
                    }
                    // ------------------------------------------------------------
                    // Write response data
                    //else if ((function >= fctWriteSingleCoil) && (function != fctReadWriteMultipleRegister))
                    //{
                    //    data = new byte[2];
                    //    Array.Copy(tcpSynClBuffer, 10, data, 0, 2);
                    //}
                    // ------------------------------------------------------------
                    // Read response data
                    else
                    {
                        data = new byte[tcpSynClBuffer[8]];
                        Array.Copy(tcpSynClBuffer, 9, data, 0, tcpSynClBuffer[8]);
                    }
                    return data;
                }
                catch (Exception e)
                {
                    return null;
                    //CallException(id, write_data[6], write_data[7], excExceptionConnectionLost);
                }
            }
            else //CallException(id, write_data[6], write_data[7], excExceptionConnectionLost);
                return null;
        }

        // ------------------------------------------------------------------------
        // Create modbus header for read action
        private byte[] CreateReadHeader(ushort id, byte unit, ushort startAddress, ushort length, byte function)
        {
            byte[] data = new byte[12];

            byte[] _id = BitConverter.GetBytes((short)id);
            data[0] = _id[1];			    // Slave id high byte
            data[1] = _id[0];				// Slave id low byte
            data[5] = 6;					// Message size
            data[6] = unit;					// Slave address
            data[7] = function;				// Function code
            byte[] _adr = BitConverter.GetBytes((short)IPAddress.HostToNetworkOrder((short)startAddress));
            data[8] = _adr[0];				// Start address
            data[9] = _adr[1];				// Start address
            byte[] _length = BitConverter.GetBytes((short)IPAddress.HostToNetworkOrder((short)length));
            data[10] = _length[0];			// Number of data to read
            data[11] = _length[1];			// Number of data to read
            return data;
        }

        // ------------------------------------------------------------------------
        // Create modbus header for write action
        private byte[] CreateWriteHeader(ushort id, byte unit, ushort startAddress, ushort numData, ushort numBytes, byte function)
        {
            byte[] data = new byte[numBytes + 11];

            byte[] _id = BitConverter.GetBytes((short)id);
            data[0] = _id[1];				// Slave id high byte
            data[1] = _id[0];				// Slave id low byte
            byte[] _size = BitConverter.GetBytes((short)IPAddress.HostToNetworkOrder((short)(5 + numBytes)));
            data[4] = _size[0];				// Complete message size in bytes
            data[5] = _size[1];				// Complete message size in bytes
            data[6] = unit;					// Slave address
            data[7] = function;				// Function code
            byte[] _adr = BitConverter.GetBytes((short)IPAddress.HostToNetworkOrder((short)startAddress));
            data[8] = _adr[0];				// Start address
            data[9] = _adr[1];				// Start address
            //if (function >= fctWriteMultipleCoils)
            //{
            //    byte[] _cnt = BitConverter.GetBytes((short)IPAddress.HostToNetworkOrder((short)numData));
            //    data[10] = _cnt[0];			// Number of bytes
            //    data[11] = _cnt[1];			// Number of bytes
            //    data[12] = (byte)(numBytes - 2);
            //}
            return data;
        }

        // ------------------------------------------------------------------------
        // Create modbus header for read/write action
        //private byte[] CreateReadWriteHeader(ushort id, byte unit, ushort startReadAddress, ushort numRead, ushort startWriteAddress, ushort numWrite)
        //{
        //    byte[] data = new byte[numWrite * 2 + 17];

        //    byte[] _id = BitConverter.GetBytes((short)id);
        //    data[0] = _id[1];						// Slave id high byte
        //    data[1] = _id[0];						// Slave id low byte
        //    byte[] _size = BitConverter.GetBytes((short)IPAddress.HostToNetworkOrder((short)(11 + numWrite * 2)));
        //    data[4] = _size[0];						// Complete message size in bytes
        //    data[5] = _size[1];						// Complete message size in bytes
        //    data[6] = unit;							// Slave address
        //    data[7] = fctReadWriteMultipleRegister;	// Function code
        //    byte[] _adr_read = BitConverter.GetBytes((short)IPAddress.HostToNetworkOrder((short)startReadAddress));
        //    data[8] = _adr_read[0];					// Start read address
        //    data[9] = _adr_read[1];					// Start read address
        //    byte[] _cnt_read = BitConverter.GetBytes((short)IPAddress.HostToNetworkOrder((short)numRead));
        //    data[10] = _cnt_read[0];				// Number of bytes to read
        //    data[11] = _cnt_read[1];				// Number of bytes to read
        //    byte[] _adr_write = BitConverter.GetBytes((short)IPAddress.HostToNetworkOrder((short)startWriteAddress));
        //    data[12] = _adr_write[0];				// Start write address
        //    data[13] = _adr_write[1];				// Start write address
        //    byte[] _cnt_write = BitConverter.GetBytes((short)IPAddress.HostToNetworkOrder((short)numWrite));
        //    data[14] = _cnt_write[0];				// Number of bytes to write
        //    data[15] = _cnt_write[1];				// Number of bytes to write
        //    data[16] = (byte)(numWrite * 2);

        //    return data;
        //}
    }

    //// Протокол SNMP
    //public class ProtocolSnmp : IProtocol
    //{
    //    // МЕТОДЫ:

    //    // Запрос данных по протоколу SNMP
    //    public virtual List<string> GetData(IInterface protocolInterface, int deviceAddress, List<int> protocolParams);
    //}

    /// /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// 
    /// Параметры протокола
    /// 
    /// /////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public interface IProtocolParams
    {
    }

    public interface IProtocolParamsForOperation
    {
    }

    /// /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// 
    /// Параметры протокола Modbus
    /// 
    /// /////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public class ModbusProtocolParams : IProtocolParams
    {
        public bool SetAnalog(int type, int regAddress, int regCount)
        {
            if (((type == UNSIGNED_TYPE || type == SIGNED_TYPE) && (regCount >= 0 && regCount <= 2)) ||
                 ((type == FLOAT_TYPE || type == FLOAT_SWAP_TYPE) && regCount == 2)
               )
            {
                m_ParamType = type;
                ParamVariant paramVariant = new ParamVariant();
                paramVariant.m_RegAddress = regAddress;
                paramVariant.m_RegCount = regCount;
                m_ParamVariants.Add(paramVariant);

                return true;
            }

            return false;
        }

        public void SetDiscrete(int logicOperation = AND_LOGIC)
        {
            m_LogicOperation = logicOperation;
            m_ParamType = DISCRETE_TYPE;
        }

        public bool AddParamVariantToDiscrete(int regAddress, int regCount)
        {
            if (m_ParamType == DISCRETE_TYPE)
            {
                ParamVariant paramVariant = new ParamVariant();
                paramVariant.m_RegAddress = regAddress;
                paramVariant.m_RegCount = regCount;
                m_ParamVariants.Add(paramVariant);

                return true;
            }

            return false;
        }

        public bool AddDiscreteValueToParamVariant(ushort[] masks = null, int value = DEFAULT_VALUE, int operation = AND_LOGIC)
        {
            if (m_ParamType == DISCRETE_TYPE)
            {
                ParamVariant.DiscreteValue discreteValue = new ParamVariant.DiscreteValue();
                discreteValue.m_Masks = masks;
                discreteValue.m_Value = value;
                discreteValue.m_Operation = operation;

                if (m_ParamVariants.Count > 0)
                {
                    m_ParamVariants[m_ParamVariants.Count - 1].m_DiscreteValues.Add(discreteValue);
                    return true;
                }
            }

            return false;
        }

        public bool SetDiscreteGroups(int groupsCount = 1, int groupsOffset = 0)
        {
            if (groupsCount > 1)
            {
                int currGroupsOffset = groupsOffset;
                int paramVariantsCount = m_ParamVariants.Count;

                for (int gr = 1; gr < groupsCount; gr++, currGroupsOffset+=groupsOffset)
                {
                    for (int var = 0; var < paramVariantsCount; var++)
                        m_ParamVariants.Add(m_ParamVariants[var].CloneWithAddressOffset(currGroupsOffset, gr));
                }
                m_GroupsCount = groupsCount;
            }

            return true;
        }

        public const int UNSIGNED_TYPE = 0;
        public const int SIGNED_TYPE = 1;
        public const int FLOAT_TYPE = 2;
        public const int FLOAT_SWAP_TYPE = 3;
        public const int DISCRETE_TYPE = 4;

        public const int DEFAULT_VALUE = -1;
        public const int AND_LOGIC = 0;
        public const int OR_LOGIC = 1;
        public const int EQUAL_OPERATION = 0;
        public const int UNEQUAL_OPERATION = 1;

        public class ParamVariant
        {
            public class DiscreteValue
            {
                public ushort[] m_Masks = null;
                public int m_Value = DEFAULT_VALUE;
                public int m_Operation = EQUAL_OPERATION;
            }
            public int m_RegAddress;
            public int m_RegCount;
            public List<DiscreteValue> m_DiscreteValues = new List<DiscreteValue>();
            public bool m_IsAddedToModbusPars = false;
            public int m_ModbusParsRef;
            public int m_GroupIndex = 0;

            public ParamVariant CloneWithAddressOffset(int addressOffset, int groupIndex)
            { 
                ParamVariant paramVariant = new ParamVariant();

                paramVariant.m_RegAddress = m_RegAddress + addressOffset;
                paramVariant.m_RegCount = m_RegCount;
                paramVariant.m_IsAddedToModbusPars = false;
                paramVariant.m_ModbusParsRef = m_ModbusParsRef;
                paramVariant.m_GroupIndex = groupIndex;

                for (int i = 0; i < m_DiscreteValues.Count; i++)
                {
                    ParamVariant.DiscreteValue discreteValue = new ParamVariant.DiscreteValue();
                    discreteValue.m_Value = m_DiscreteValues[i].m_Value;
                    discreteValue.m_Operation = m_DiscreteValues[i].m_Operation;

                    if (m_DiscreteValues[i].m_Masks != null)
                    {
                        discreteValue.m_Masks = new ushort[m_DiscreteValues[i].m_Masks.Length];
                        for (int j = 0; j < m_DiscreteValues[i].m_Masks.Length; j++)
                            discreteValue.m_Masks[j] = m_DiscreteValues[i].m_Masks[j];
                    }

                    paramVariant.m_DiscreteValues.Add(discreteValue);
                }

                return paramVariant;
            }
        }

        public int m_ParamType;
        public int m_LogicOperation = AND_LOGIC;
        public int m_GroupsCount = 1;
        //public int m_GroupsOffset = 0;
        public List<ParamVariant> m_ParamVariants = new List<ParamVariant>();
    }

    public class ModbusProtocolParamsForOperation : IProtocolParamsForOperation
    {
        public ushort m_RegAddress;
        public ushort m_RegCount;
    }

    /// /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// 
    /// Параметры оборудования с описание и значениями параметров протокола (например если это параметр Modbus-устройства, то
    /// значения параметров протокола: m_ProtocolParams[0] - адрес Modbus-регистра, m_ProtocolParams[1] - количество Modbus-регистров )
    /// 
    /// /////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // Значение параметра
    //public class Value
    //{
    //    // ДАННЫЕ:

    //    // Значение дискретного параметра
    //    public int DisceteValue;

    //    // Значение аналогового параметра в физ. единицах
    //    public double AnalogValue;

    //    // Признак корректности параметра
    //    public bool IsValidValue; 
    //}

    public class Parameter : INotifyPropertyChanged
    {
        // МЕТОДЫ:
        public Parameter(SyncForParameterValue syncForParameterValue)
        {
            m_SyncForParameterValue = syncForParameterValue;
        }

        //public void SetSyncForParameterValue(SyncForParameterValue syncForParameterValue)
        //{
        //    m_SyncForParameterValue = syncForParameterValue;
        //}

        public event PropertyChangedEventHandler PropertyChanged;

        // This method is called by the Set accessor of each property.
        // The CallerMemberName attribute that is applied to the optional propertyName
        // parameter causes the property name of the caller to be substituted as an argument.
        public void NotifyPropertyChanged()
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("Value"));
            }
        }

        // Свойство Value
        public string Value
        {
            get
            {
                string sValue;
                lock (m_SyncObj)
                {
                    sValue = m_Value;
                }
                return sValue;
            }

            set
            {
                bool isUpdate = false;
                lock (m_SyncObj)
                {
                    if (m_Value != value)
                    {
                        isUpdate = true;
                        m_Value = value;
                    }
                }

                if (isUpdate)
                    m_SyncForParameterValue.UpdateParameter(this);
            }
        }     

        // ДАННЫЕ:

        // Константы размерности для аналогового параметра
        public const int AMPER = 0;
        public const int VOLT = 1;
        public const int DEGREE_CELS = 2;
        public const int HOUR = 3;

        // Константы тревожгости для дискретного параметра
        public const int MESSAGE = 0;
        public const int WARNING = 1;
        public const int ALARM = 2;

        // Константы типа
        public const int DISCRETE = 0;
        public const int ANALOG = 1;

        // Наименование параметра
        public string m_Name;

        // Описание параметра
        public string m_Description;

        // Размерность параметра AMPER, VOLT, ...
        public int m_Dimension;

        // Сообщение, предупреждение, тревога
        public int m_DiscreteType;

        // Тип параметра DISCRETE, ANALOG
        public int m_Type;

        public const string UNDEF_VALUE = "???";
        public const string DISCRETE_TRUE_VALUE = "1";
        public const string DISCRETE_FALSE_VALUE = "0";

        // Текущее значение параметра типа Value
        public string m_Value = UNDEF_VALUE; // текущее значение параметра типа Value (любой тип данных хранится в виде строки)

        // Признак корректности параметра
        public bool m_IsValidValue = false;

        // Время обновления параметра (через сколько мсек должен запрашиваться этот парметер из устройства)
        public uint m_RefrashTime;

        // Коэффициент K для получения значения параметра в физ. велечине: Value_физ. = K * Value + B
        public float m_CoefK = 1.0f;

        // Коэффициент B для получения значения параметра в физ. велечине: Value_физ. = K * Value + B
        public float m_CoefB = 0.0f;

        // Значениями параметров протокола (например если это параметр Modbus-устройства, то
        // значения параметров протокола: m_ProtocolParams[0] - адрес Modbus-регистра, m_ProtocolParams[1] - количество Modbus-регистров )
        public IProtocolParams m_ProtocolParams = null;

        private object m_SyncObj = new object();

        private SyncForParameterValue m_SyncForParameterValue;
    }

    /// /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// 
    /// Типовые устройства: панель PW2.0/2.1, контроллер ПН4/8
    /// 
    /// /////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // Базовый interface для всех типов устройст, который задаёт общие свойства и типы поведения для всех типов устройств
    public interface IDevice
    {
        // МЕТОДЫ:

        // Чтение параметров устройства
        bool ReadParameters(Connection connection);

        // СВОЙСТВА:
        int Number { get; set; }

        int Address { get; set; }

        // Параметры интерфейса специфичные для данного типа устройства, например, таймаут ответа устройств
        List<int> ParamsForInterface { get; set; }

        // Cвойство readonly для binding с ViewsModel
        ObservableCollection<Parameter> Params { get; }
    }

    // Базовый класс для всех типов устройств с протоколом Modbus
    public abstract class ModbusDevice : IDevice
    {
        class VARIANT_RESULT
        {
            public VARIANT_RESULT(int group, bool result)
            {
                m_Group = group;
                m_Result = result;
            }

            public int m_Group = 0;
            public bool m_Result = false;
        };

        protected void CreateModbusPars()
        {
            // Задаём параметры интерфейса специфичные для данного типа устройства, например, таймаут ответа устройств

            // Таймаут ответа устройства по умолчанию
            m_ParamsForInterface.Add(500);

            // Таймаут между запросами к устройству по умолчанию
            m_ParamsForInterface.Add(50);

            // Таймаут между 2 байтами ответа устройства по умолчанию
            m_ParamsForInterface.Add(50);

            const int MAX_MODBUS_RERS_COUNT = 127;

            // Создаём список List<ModbusPars> m_ModbusPars
            while (true)
            {
                bool isNewParameter = true;
                int refrashTime = 0;
                int regAddress = 0;
                int regCount = 0;
                List<int> parametersRef = null;
                bool isAllAddedToModbusPars = true;

                for (int par = 0; par < m_Parameters.Count; par++)
                {
                    Parameter parameter = m_Parameters[par];
                    bool isParameterRefExists = false;
                    ModbusProtocolParams modbusProtocolParams = (ModbusProtocolParams)parameter.m_ProtocolParams;

                    for (int prot = 0; prot < modbusProtocolParams.m_ParamVariants.Count; prot++)
                    {
                        ModbusProtocolParams.ParamVariant paramVariant = modbusProtocolParams.m_ParamVariants[prot];
                        if (!paramVariant.m_IsAddedToModbusPars)
                        {
                            isAllAddedToModbusPars = false;

                            if (isNewParameter)
                            {
                                isNewParameter = false;
                                refrashTime = (int)parameter.m_RefrashTime;
                                regAddress = paramVariant.m_RegAddress;
                                regCount = paramVariant.m_RegCount;
                                parametersRef = new List<int>();
                                parametersRef.Add(par);
                                isParameterRefExists = true;

                                paramVariant.m_ModbusParsRef = m_ModbusPars.Count;
                                paramVariant.m_IsAddedToModbusPars = true;
                            }
                            else
                            {
                                if ((int)parameter.m_RefrashTime == refrashTime)
                                {
                                    if (paramVariant.m_RegAddress >= regAddress && paramVariant.m_RegAddress <= regAddress + regCount-1 &&
                                        (paramVariant.m_RegCount + paramVariant.m_RegAddress - regAddress) <= MAX_MODBUS_RERS_COUNT
                                       ) 
                                    {
                                        if (paramVariant.m_RegCount + paramVariant.m_RegAddress - regAddress > regCount)
                                            regCount = paramVariant.m_RegCount + paramVariant.m_RegAddress - regAddress;

                                        if (!isParameterRefExists)
                                        {
                                            parametersRef.Add(par);
                                            isParameterRefExists = true;
                                        }

                                        paramVariant.m_ModbusParsRef = m_ModbusPars.Count;
                                        paramVariant.m_IsAddedToModbusPars = true;
                                    }
                                    else if (paramVariant.m_RegAddress == regAddress + 1 &&
                                             (regCount + paramVariant.m_RegCount) <= MAX_MODBUS_RERS_COUNT
                                            )
                                    {
                                        regCount += paramVariant.m_RegCount;
                                        if (!isParameterRefExists)
                                        {
                                            parametersRef.Add(par);
                                            isParameterRefExists = true;
                                        }

                                        paramVariant.m_ModbusParsRef = m_ModbusPars.Count;
                                        paramVariant.m_IsAddedToModbusPars = true;
                                    }
                                }
                            }
                        }
                    }
                    
                    if (par == m_Parameters.Count-1 && !isAllAddedToModbusPars)
                    {
                        ModbusPars modbusPars = new ModbusPars(refrashTime, regAddress, regCount, parametersRef);
                        m_ModbusPars.Add(modbusPars);
                    }
                }

                if (isAllAddedToModbusPars)
                    break;
            }
        }

        protected void  UpdateParameterFromModbus(int parameter_num)
        {
            if (parameter_num < m_Parameters.Count)
            {
                Parameter parameter = m_Parameters[parameter_num];

                parameter.m_IsValidValue = false;
                //parameter.Value = Parameter.UNDEF_VALUE;

                ModbusProtocolParams modbusProtocolParams = (ModbusProtocolParams)parameter.m_ProtocolParams;

                int logicOperation = modbusProtocolParams.m_LogicOperation;

                if (modbusProtocolParams.m_GroupsCount <= 0)
                    modbusProtocolParams.m_GroupsCount = 1;

                bool[] discreteParamVariantResults = new bool[modbusProtocolParams.m_ParamVariants.Count];

                for (int var = 0; var < modbusProtocolParams.m_ParamVariants.Count; var++)
                {
                    ushort reg_address = (ushort)modbusProtocolParams.m_ParamVariants[var].m_RegAddress;
                    ushort reg_count = (ushort)modbusProtocolParams.m_ParamVariants[var].m_RegCount;
                    int modbusParsRef = modbusProtocolParams.m_ParamVariants[var].m_ModbusParsRef;

                    int groupIndex = modbusProtocolParams.m_ParamVariants[var].m_GroupIndex;
                    if (groupIndex < 0 || groupIndex >= modbusProtocolParams.m_GroupsCount)
                        groupIndex = ModbusProtocolParams.DEFAULT_VALUE;

                    if (modbusParsRef < m_ModbusPars.Count)
                    {
                        ModbusPars modbusPars = m_ModbusPars[modbusParsRef];
                        if (modbusPars.m_IsValidData)
                        {
                            ushort[] regData = new ushort[reg_count];
                            int addressOffset = reg_address - modbusPars.m_RegAddress;
                            if (addressOffset + reg_count <= modbusPars.m_RegCount)
                            {
                                for (int i = 0; i < reg_count; i++)
                                    regData[i] = (ushort)modbusPars.m_RegData[i + addressOffset];

                                if (modbusProtocolParams.m_ParamType == ModbusProtocolParams.DISCRETE_TYPE)
                                {
                                    bool discreteValueResult = false;

                                    for (int val = 0; val < modbusProtocolParams.m_ParamVariants[var].m_DiscreteValues.Count; val++)
                                    {
                                        ModbusProtocolParams.ParamVariant.DiscreteValue discreteValue = modbusProtocolParams.m_ParamVariants[var].m_DiscreteValues[val];

                                        // Применить маску
                                        if (discreteValue.m_Masks.Length > 0 && discreteValue.m_Masks.Length <= reg_count)
                                        {
                                            for (int mask = 0; mask < discreteValue.m_Masks.Length; mask++)
                                                regData[mask] &= discreteValue.m_Masks[mask];
                                        }

                                        // Применить значение
                                        if (discreteValue.m_Value != ModbusProtocolParams.DEFAULT_VALUE)
                                        {
                                            // Подсчитываем количество 0-й от начала до первой 1-ы в discreteValuе
                                            int begMaskZeroCount = 0;
                                            for (int i = 0; i < discreteValue.m_Masks.Length; i++)
                                            {
                                                bool isBreak = false;
                                                for (int j = 0; j < 16; j++)
                                                {
                                                    ushort one = (ushort)(Math.Pow(2, j));
                                                    if ((~discreteValue.m_Masks[i] & one) != 0)
                                                        begMaskZeroCount++;
                                                    else
                                                    {
                                                        isBreak = true;
                                                        break;
                                                    }
                                                }
                                                if (isBreak)
                                                    break;
                                            }

                                            int value = 0;
                                            if (reg_count == 1)
                                                value = (int)regData[0];
                                            else
                                            {
                                                int tmp = (int)regData[1];
                                                tmp = tmp << 16;
                                                value = ((int)regData[0] | tmp);
                                            }
                                            if (begMaskZeroCount > 0)
                                                value = value >> begMaskZeroCount;

                                            if ((discreteValue.m_Value == value && discreteValue.m_Operation == ModbusProtocolParams.EQUAL_OPERATION) ||
                                                (discreteValue.m_Value != value && discreteValue.m_Operation == ModbusProtocolParams.UNEQUAL_OPERATION))
                                            {
                                                discreteValueResult = true;
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            bool isBreak = false;
                                            for (int i = 0; i < reg_count; i++)
                                            {
                                                if ((regData[i] != 0x0 && discreteValue.m_Operation == ModbusProtocolParams.EQUAL_OPERATION) ||
                                                    (regData[i] == 0x0 && discreteValue.m_Operation == ModbusProtocolParams.UNEQUAL_OPERATION))
                                                { 
                                                    discreteValueResult = true;
                                                    isBreak = true;
                                                }
                                                if (isBreak)
                                                    break;
                                            }
                                        }
                                    }

                                    parameter.m_IsValidValue = true;

                                    if (discreteValueResult && groupIndex >= 0)
                                        discreteParamVariantResults[var] = true;
                                }
                                else
                                {
                                    float fValue = 0;
                                    if (modbusProtocolParams.m_ParamType == ModbusProtocolParams.UNSIGNED_TYPE)
                                    {
                                        if (modbusProtocolParams.m_ParamVariants.Count > 0)
                                        {
                                            if (modbusProtocolParams.m_ParamVariants[0].m_RegCount == 1)
                                            {
                                                parameter.m_IsValidValue = true;
                                                fValue = regData[0];
                                            }
                                            else if (modbusProtocolParams.m_ParamVariants[0].m_RegCount == 2)
                                            {
                                                uint tmp = (uint)regData[0];
                                                tmp = tmp << 16;
                                                uint value = ((uint)(ushort)regData[1] | tmp);

                                                parameter.m_IsValidValue = true;
                                                fValue = value;
                                            }
                                        }
                                    }
                                    else if (modbusProtocolParams.m_ParamType == ModbusProtocolParams.SIGNED_TYPE)
                                    {
                                        if (modbusProtocolParams.m_ParamVariants.Count > 0)
                                        {
                                            if (modbusProtocolParams.m_ParamVariants[0].m_RegCount == 1)
                                            {
                                                parameter.m_IsValidValue = true;
                                                fValue = regData[0];
                                            }
                                            else if (modbusProtocolParams.m_ParamVariants[0].m_RegCount == 2)
                                            {
                                                int tmp = (int)regData[0];
                                                tmp = tmp << 16;
                                                int value = ((int)(ushort)regData[1] | tmp);

                                                parameter.m_IsValidValue = true;
                                                fValue = value;
                                            }
                                        }
                                    }
                                    else if (modbusProtocolParams.m_ParamType == ModbusProtocolParams.FLOAT_TYPE)
                                    {
                                        byte[] bytes = new byte[4];
                                        bytes[0] = (byte)regData[1];
                                        bytes[1] = (byte)(regData[1] >> 8);
                                        bytes[2] = (byte)regData[0];
                                        bytes[3] = (byte)(regData[0] >> 8);

                                        parameter.m_IsValidValue = true;
                                        fValue = BitConverter.ToSingle(bytes, 0);
                                    }
                                    else if (modbusProtocolParams.m_ParamType == ModbusProtocolParams.FLOAT_SWAP_TYPE)
                                    {
                                        byte[] bytes = new byte[4];
                                        bytes[0] = (byte)regData[0];
                                        bytes[1] = (byte)(regData[0] >> 8);
                                        bytes[2] = (byte)regData[1];
                                        bytes[3] = (byte)(regData[1] >> 8);

                                        parameter.m_IsValidValue = true;
                                        fValue = BitConverter.ToSingle(bytes, 0);
                                    }

                                    if (parameter.m_IsValidValue)
                                    {
                                        if (parameter.m_CoefK != 1.0 || parameter.m_CoefB != 0.0)
                                        {
                                            parameter.Value = Convert.ToString(fValue * parameter.m_CoefK + parameter.m_CoefB);
                                        }
                                        else
                                            parameter.Value = Convert.ToString(fValue);
                                    }
                                }
                            }
                        }
                    }
                }

                if (parameter.m_IsValidValue && parameter.m_Type == Parameter.DISCRETE)
                {
                    bool[] results = new bool[modbusProtocolParams.m_GroupsCount];

                    for (int gr = 0; gr < modbusProtocolParams.m_GroupsCount; gr++)
                    {
                        if (logicOperation == ModbusProtocolParams.AND_LOGIC)
                        {
                            results[gr] = true;
                            for (int i = 0; i < discreteParamVariantResults.Length; i++)
                            {
                                if (modbusProtocolParams.m_ParamVariants[i].m_GroupIndex == gr && !discreteParamVariantResults[i])
                                {
                                    results[gr] = false;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            results[gr] = false;
                            for (int i = 0; i < discreteParamVariantResults.Length; i++)
                            {
                                if (modbusProtocolParams.m_ParamVariants[i].m_GroupIndex == gr && discreteParamVariantResults[i])
                                {
                                    results[gr] = true;
                                    break;
                                }
                            }
                        }
                    }

                    parameter.Value = Parameter.DISCRETE_FALSE_VALUE;
                    for (int gr = 0; gr < modbusProtocolParams.m_GroupsCount; gr++)
                    {
                        if (results[gr])
                        {
                            parameter.Value = Parameter.DISCRETE_TRUE_VALUE;
                            break;
                        }     
                    }
                }

                if (!parameter.m_IsValidValue)
                    parameter.Value = Parameter.UNDEF_VALUE;
            }
        }

        // Чтение параметров из устройства по протоколу Modbus
        public virtual bool ReadParameters(Connection connection)
        {
            int passedTime;
            bool res = false;

            // Устанавливаем параметры интерфейса специфичные для данного типа устройства, например, таймаут ответа устройств
            connection.m_Interface.SetParamsByDevice(m_ParamsForInterface);

            for (int par = m_LastModbusPars; par < m_ModbusPars.Count; par++)
            {
                ModbusPars modbusPars = m_ModbusPars[par];

                passedTime = (int)(GetMilliseconds() - modbusPars.m_LastUpdateMsec);
                if (passedTime < modbusPars.m_RefrashTime && modbusPars.m_LastUpdateMsec > 0)
                    continue;

                // Таймаут между запросами к устройству
                if (m_ParamsForInterface.Count >= 2 && m_ParamsForInterface[1] > 0 && m_LastTimeBeetwenReq > 0)
                {
                    passedTime = (int)(GetMilliseconds() - m_LastTimeBeetwenReq);
                    if (passedTime < m_ParamsForInterface[1])
                    {
                        Task delay = Task.Delay(m_ParamsForInterface[1] - passedTime);
                        delay.Wait();
                    }
                }
                m_LastTimeBeetwenReq = GetMilliseconds();
         
                modbusPars.m_LastUpdateMsec = GetMilliseconds(); 
      
                ModbusProtocolParamsForOperation modbusProtocolParams = new ModbusProtocolParamsForOperation();
                modbusProtocolParams.m_RegAddress = (ushort)modbusPars.m_RegAddress;
                modbusProtocolParams.m_RegCount = (ushort)modbusPars.m_RegCount;

                short[] values = m_Protocol.GetData(connection.m_Interface, m_Address, modbusProtocolParams);
                if (values != null)
                {
                    modbusPars.m_RegData = values;
                    modbusPars.m_IsValidData = true;
                    res = true;
                }
                else
                {
                    modbusPars.m_RegData = null;
                    modbusPars.m_IsValidData = false;
                }

                for (int i = 0; i < modbusPars.m_ParametersRef.Count; i++)
                {
                    UpdateParameterFromModbus(modbusPars.m_ParametersRef[i]);
                }
                
                if (m_LastModbusPars == m_ModbusPars.Count - 1)
                    m_LastModbusPars = 0;
                else
                    m_LastModbusPars = par + 1;
                break;
            }

            return res;
        }

        public static double GetMilliseconds()
        {
            DateTime baseDate = new DateTime(1970, 1, 1);
            TimeSpan diff = DateTime.Now - baseDate;

            return diff.TotalMilliseconds;
        }

        // СВОЙСТВА:
        public int Number
        {
            get { return m_Number; }
            set { m_Number = value; }
        }

        public int Address
        {
            get { return m_Address; }
            set { m_Address = value; }
        }

        // Cвойство readonly для binding с ViewsModel
        public ObservableCollection<Parameter> Params
        {
            get
            {
                return m_Parameters;
            }
        }

        // Параметры интерфейса специфичные для данного типа устройства, например, таймаут ответа устройств
        public List<int> ParamsForInterface
        {
            get { return m_ParamsForInterface; }
            set { m_ParamsForInterface = value; }
        }

        // Протокол устройства ProtocolModbus
        public  IProtocol<short[]> Protocol
        {
            get { return m_Protocol; }
            set { m_Protocol = value; }
        }

        // ДАННЫЕ:

        // Порядковый номер устройства 
        protected int m_Number;

        // Адрес устройства для запросов по протоколу Modbus-RTU
        protected int m_Address;

        // Cписок параметров устройства в виде ObservableCollection
        protected ObservableCollection<Parameter> m_Parameters = new ObservableCollection<Parameter>();

        // Параметры интерфейса специфичные для данного типа устройства, например, таймаут ответа устройств
        protected List<int> m_ParamsForInterface = new List<int>();

        // Протокол устройства ProtocolModbus
        protected IProtocol<short[]> m_Protocol = null; //  new ProtocolModbusRTU();

        // Группа из Modbus-регистров, с одним временем обновления, не более 127
        class ModbusPars
        {
            public ModbusPars(int refrashTime, int regAddress, int regCount, List<int> parametersRef)
            {
                m_RefrashTime = refrashTime;
                m_RegAddress = regAddress;
                m_RegCount = regCount;
                m_ParametersRef = parametersRef;
            }

            public int m_RefrashTime = 500;

            // Время последнего обновления параметра в мсек с 01.01.1970
            public double m_LastUpdateMsec = 0;

            // Счётчик последнего обновления параметра
            //private long m_LastUpdateCount = 0;

            public int m_RegAddress;

            public int m_RegCount;

            public bool m_IsValidData = false;
            public short[] m_RegData;
            public List<int> m_ParametersRef = null; // new List<int>();

            public List<int> ParametersRef
            {
                get { return m_ParametersRef; }
            }
        }

        //private ParameterFromModbus[] m_ParametersFromModbus;
        private List<ModbusPars> m_ModbusPars = new List<ModbusPars>();
        private int m_LastModbusPars = 0;

        private double m_LastTimeBeetwenReq = 0;
    }

    // Тип устройства: Панель PW2.0/2.1
    public class DevicePW : ModbusDevice
    {
        // МЕТОДЫ:

        // Констуктор класса с параметром
        public DevicePW(SyncForParameterValue syncForParameterValue, bool isPW2_1 = true)
        {
            ModbusProtocolParams protocolParams;
            Parameter parameter;

            //////////////////////////////////////////////////
            // Параметр Uacc
            //////////////////////////////////////////////////
            parameter = new Parameter(syncForParameterValue);
            parameter.m_Name = "Uacc";
            parameter.m_Dimension = Parameter.AMPER;
            parameter.m_Type = Parameter.ANALOG;
            parameter.m_RefrashTime = 500;
            // Y = CoefK*(protocolParams.SetAnalog(ModbusProtocolParams.UNSIGNED_TYPE, 201, 1);) + CoefB
            parameter.m_CoefK = 0.05f;
            parameter.m_CoefB = 0.0f;

            protocolParams = new ModbusProtocolParams();

            // Установить аналог: тип данных, Моdbus-регистр,  количество Моdbus-регистров:
            // ModbusProtocolParams.UNSIGNED_TYPE, ModbusProtocolParams.SIGNED_TYPE, ModbusProtocolParams.FLOAT_TYPE
            protocolParams.SetAnalog(ModbusProtocolParams.UNSIGNED_TYPE, 201, 1);
            parameter.m_ProtocolParams = protocolParams;
            m_Parameters.Add(parameter);

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //  Дискреты: AlarmPresent0, AlarmActive0, AlarmPresent1, AlarmActive1, ...
            //            где AlarmPresent - авария присутстувует в данный момент 
            //                AlarmActive - авария уже отсутстувует в данный момент, но ещё не заквитированна
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            // Event Registers - register count: 14, begin address 1034 (20 or 40 events):
            // Register 2:0 = Last Timestamp
            // Register 5:3 = First Timestamp
            // Register 7:6 = Last Hourmeter
            // Register 9:8 = First Hourmeter
            // Register 10 = Flags/Count: Bits 15:12 UNUSED Bits 11:8 Event Status: Bits 0000 = Inactive, 0100 = Active, 0101 = Present, 1111 = Unavailable Bits 7:0 Occurrence Count
            // Register 12:11 = SPN/FMI
            // Register 13 = Log Entry Index

            // Здесь заполняем массивы pwFMIs и pwSPNs соответствующими парами FMI/SPN всех событий
            // Здесь заполнено только первые значения, нужно заполнить полный список
            int[] pwSPNs  = new int[] { 38, 38, 38, 38, 38, 38 };
            int[] pwFMIs  = new int[] {  0,  1,  3,  4, 15, 17 };
            
            const int PW_EVENT_BASE_ADDRESS = 0;// 1034;

            if (pwFMIs.Length == pwSPNs.Length)
            {
                 for (int ev = 0; ev < pwFMIs.Length; ev++)
                 {
                    for (int pr = 0; pr < 2; pr++)
                    {
                        parameter = new Parameter(syncForParameterValue);
                        parameter.m_Name = "Alarm";
                        parameter.m_Type = Parameter.DISCRETE;
                        parameter.m_RefrashTime = 500;

                        protocolParams = new ModbusProtocolParams();
                        protocolParams.SetDiscrete();

                        // В этом варианте параметра проверяется Register 10 = Flags/Count на наличие Bits 11:8 Event Status: Bits 0101 = Present или 0100 = Active
                        // Добавить вариант параметра:начальный адрес Моdbus-регистра, количество Моdbus-регистров
                        protocolParams.AddParamVariantToDiscrete(PW_EVENT_BASE_ADDRESS + 10, 1);

                        if (pr == 0)
                        {
                            // Добавить одно из возможных значений варианта параметра: маска, значение, операция сравнения: равно - 0; неравно - 1
                            protocolParams.AddDiscreteValueToParamVariant(new ushort[] { 0x0F00 }, 0x05); // Register 10 Flags/Count: Bits 11:8 Event Status: Bits 0101 = Present
                            parameter.m_Name = parameter.m_Name + "Present" + ev;
                        }
                        else
                        {
                            // Добавить одно из возможных значений варианта параметра: маска, значение, операция сравнения: равно - 0; неравно - 1
                            protocolParams.AddDiscreteValueToParamVariant(new ushort[] { 0x0F00 }, 0x04); // Register 10 Flags/Count: Bits 11:8 Event Status: Bits 0100 = Active
                            parameter.m_Name = parameter.m_Name + "Active" + ev;
                        }

                        // В этом значении варианта параметра проверяется Register 11 = Bits 4:0 = FMI на наличие нужного FMI
                        // Добавить вариант параметра:начальный адрес Моdbus-регистра, количество Моdbus-регистров
                        protocolParams.AddParamVariantToDiscrete(PW_EVENT_BASE_ADDRESS + 11, 1);

                        // Добавить одно из возможных значений варианта параметра: маска, значение, операция сравнения: равно - 0; неравно - 1
                        protocolParams.AddDiscreteValueToParamVariant(new ushort[] { 0x001F }, pwFMIs[ev]); // Register 11 Bits 4:0 = FMI

                        // В этом значении варианта параметра проверяется Register 12:11 = Bits 23:5 = SPN на наличие нужного SPN
                        // Добавить вариант параметра:начальный адрес Моdbus-регистра, количество Моdbus-регистров
                        protocolParams.AddParamVariantToDiscrete(PW_EVENT_BASE_ADDRESS + 11, 2);

                        // Добавить одно из возможных значений варианта параметра: маска, значение, операция сравнения: равно - 0; неравно - 1
                        protocolParams.AddDiscreteValueToParamVariant(new ushort[] { 0xFFE0, 0x00FF }, pwSPNs[ev]); // Register 12:11 Bits 23:5 = SPN

                        // Добавляем дискретные группы, объединённые логической ИЛИ: 40/20 групп со смещением 14 регистров
                        protocolParams.SetDiscreteGroups(isPW2_1? 40: 20, 14);

                        parameter.m_ProtocolParams = protocolParams;
                        m_Parameters.Add(parameter);
                    }
                }
            }

            // ...
            CreateModbusPars();
        }
    }

    // Тип устройства: Контроллер ПН-8
    public class DevicePN : ModbusDevice
    {
        // МЕТОДЫ:

        // Констуктор класса с параметром
        public DevicePN(SyncForParameterValue syncForParameterValue)
        {
            ModbusProtocolParams protocolParams;
            Parameter parameter;

            //////////////////////////////////////////////////
            //  Пример Дискрет: Авария №0 ПН-9
            //////////////////////////////////////////////////
            parameter = new Parameter(syncForParameterValue);
            parameter.m_Name = "Discrete0";
            parameter.m_Type = Parameter.DISCRETE;
            parameter.m_RefrashTime = 500;

            protocolParams = new ModbusProtocolParams();
            protocolParams.SetDiscrete();

            // Состояние аварии №0
            // Добавить вариант параметра:начальный адрес Моdbus-регистра, количество Моdbus-регистров
            protocolParams.AddParamVariantToDiscrete(1, 1);

            // Добавить одно из возможных значений варианта параметра: маска, значение, операция сравнения: равно - 0; неравно - 1
            protocolParams.AddDiscreteValueToParamVariant(new ushort[] { 0x0001 });

            // Настройка аварии №0
            // Добавить вариант параметра: Моdbus-регистр, количество Моdbus-регистров
            protocolParams.AddParamVariantToDiscrete(9001, 1);

            // Добавить одно из возможных значений варианта параметра: маска, значение, операция сравнения: равно - 0; неравно - 1
            protocolParams.AddDiscreteValueToParamVariant(new ushort[] { 0x007F }, 3);
            protocolParams.AddDiscreteValueToParamVariant(new ushort[] { 0x007F }, 4);
            protocolParams.AddDiscreteValueToParamVariant(new ushort[] { 0x007F }, 5);

            parameter.m_ProtocolParams = protocolParams;
            m_Parameters.Add(parameter);

            //////////////////////////////////////////////////
            // Параметр Моточасы
            //////////////////////////////////////////////////
            parameter = new Parameter(syncForParameterValue);
            parameter.m_Name = "Mhour";
            parameter.m_Dimension = Parameter.HOUR;
            parameter.m_Type = Parameter.ANALOG;
            parameter.m_RefrashTime = 500;
            // Y = CoefK*(protocolParams.SetAnalog(ModbusProtocolParams.UNSIGNED_TYPE, 201, 1);) + CoefB
            //parameter.m_CoefK = 1.0f;
            //parameter.m_CoefB = 0.0f;

            protocolParams = new ModbusProtocolParams();

            // Установить аналог: тип данных, Моdbus-регистр,  количество Моdbus-регистров:
            // ModbusProtocolParams.UNSIGNED_TYPE, ModbusProtocolParams.SIGNED_TYPE, ModbusProtocolParams.FLOAT_TYPE
            protocolParams.SetAnalog(ModbusProtocolParams.FLOAT_TYPE, (3001+(4*5)), 2);
            parameter.m_ProtocolParams = protocolParams;
            m_Parameters.Add(parameter);

            // ...
            CreateModbusPars();
        }
    }



    /// /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// 
    /// Точки подключения к устройствам
    /// 
    /// /////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public class Connection
    {
        // МЕТОДЫ:

        // Открыть соединение (установить подключение по COM-порту, TCP-IP, ...)
        public bool Open()
        {
            if (m_Interface.IsOpen())
            {
                if (!m_Interface.Close())
                    return false;
            }
            return m_Interface.Open();
        }

        // Закрыть соединение 
        public bool Close()
        {
            return m_Interface.Close();
        }

        // Чтение параметров из устройств, подключенных к данной точки подключения (например, устройства на шине RS-485)
        public void ReadParametesFromDevices()
        {
            foreach (IDevice device in m_Devices)
            {
                if (!m_Interface.IsOpen())
                    Open();
                device.ReadParameters(this /*передаём ссылку на данное Connection*/);
            }
        }

        // ДАННЫЕ:

        // Список устройств, подключенных к данной точки подключения (например, устройства на шине RS-485)
        public ObservableCollection<IDevice> m_Devices = new ObservableCollection<IDevice>();

        // Интерфейс подключения RS-485, TCP-IP
        public IInterface m_Interface;

        public string m_Name;
    }

    /// /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// 
    /// Класс DataModel для взаимодействия с ViewsModel
    /// 
    /// /////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public class DataModel
    {
        // МЕТОДЫ:

        // Добавить точку подключения
        public void AddConnection(Connection connection)
        {
            m_Connections.Add(connection);
        }

        // Подключиться к устройствам через точки подключения
        private Task OpenConnectionsTask()
        {
            return Task.Run(() =>
            {
                SetDevicesByNumbers();

                foreach (Connection connection in m_Connections)
                {
                    connection.Open();
                }
            });
        }

        public void OpenConnections()
        {
            Task openTask = OpenConnectionsTask();
        }

        // Отключиться от устройств
        public void CloseConnections()
        {
            foreach (Connection connection in m_Connections)
            {
                connection.Close();
            }
        }

        protected void SetDevicesByNumbers()
        {
            IDevice deviceByNumber = null;

            for (int number = 0; number < 32; number++)
            {
                bool isBreak = false;
                foreach (Connection connection in m_Connections)
                {
                    foreach (IDevice device in connection.m_Devices)
                    {
                        if (device.Number == number)
                        {
                            deviceByNumber = device;
                            isBreak = true;
                            break;
                        }
                    }

                    if (isBreak)
                        break;
                }

                if (deviceByNumber != null)
                {
                    m_DevicesByNumbers.Add(deviceByNumber);
                    deviceByNumber = null;
                }
            }
        }

        private Task ReadParametesFromAllDevices()
        {
            return Task.Run(() =>
            {
                while (true)
                {
                    m_isReadTaskStarted = false;
                    foreach (Connection connection in m_Connections)
                    {
                        connection.ReadParametesFromDevices();
                    }

                    //UpdateDataInDevicesByNumbers();
                    //Task delay = Task.Delay(1000);
                    //delay.Wait();

                    if (m_TerminateEvent.WaitOne(0))
                    {
                        m_WaitTerminateEvent.Set();
                        m_isReadTaskStarted = false;
                        break;
                    }
                }

            });
        }

        // Запуск циклического чтения параметров из всех устройств
        public void StartReadParametesFromAllDevices()
        {
            Task readTask = ReadParametesFromAllDevices();
        }

        // Останов циклического чтения параметров из всех устройств
        public void StopReadParametesFromAllDevices()
        {
            if (m_isReadTaskStarted)
            {
                m_TerminateEvent.Set();
                m_WaitTerminateEvent.WaitOne();
            }

            CloseConnections();
        }

        //// Обновить m_DevicesByNumbers для binding
        //protected void UpdateDataInDevicesByNumbers()
        //{
        //}

        // CВОЙСТВА:

        // Свойство readonly для ViewsModel
        public ObservableCollection<IDevice> Devices
        {
            get
            { 
                return m_DevicesByNumbers;
            }
        }

        public List<Connection> Connections
        {
            get
            {
                return m_Connections;
            }
        }

        // ДАННЫЕ:

        // Точки подключения
        protected List<Connection> m_Connections = new List<Connection>();

        // Устройства, отсортированные по порядковому номеру устройства
        protected ObservableCollection<IDevice> m_DevicesByNumbers = new ObservableCollection<IDevice>();

        // События синхронизации для завеошения задачи ReadParametesFromAllDevices
        protected static AutoResetEvent m_TerminateEvent = new AutoResetEvent(false);
        protected static AutoResetEvent m_WaitTerminateEvent = new AutoResetEvent(false);
        protected bool m_isReadTaskStarted = false;
    }

    public class SyncForParameterValue
    {
        public SyncForParameterValue(SynchronizationContext syncContext, SendOrPostCallback updateParameterFunc)
        {
            m_SyncContext = syncContext;
            m_SyncUpdateParamerFunc = updateParameterFunc;
        }

        public void UpdateParameter(Parameter parameter)
        {
            // говорим что в UI потоке нужно выполнить метод UpdateParameter 
            // и передать ему в качестве аргумента ссылку на параметр
            m_SyncContext.Post(m_SyncUpdateParamerFunc, parameter);
        }

        private SynchronizationContext m_SyncContext;
        private SendOrPostCallback m_SyncUpdateParamerFunc;
    }

    ///// /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///// 
    ///// Класс ViewsModel
    ///// 
    ///// /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///// 
    public class ViewsMode
    {
        public DataModel DM;

        public void Destroy()
        {
            DM.StopReadParametesFromAllDevices();
        }

        // Заполняем DataModel данными
        public void FillDevices(SyncForParameterValue syncForParameterValue)
        {
            //const string RS_485 = "RS485";
            //const string TCP_IP = "TCPIP";
            //const string PN_PANEL = "PN";
            //const string PW_PANEL = "PW";

            // Создание объекта, для работы с файлом
            //     INIManager manager = new INIManager("config.ini");
            //INIManager manager = new INIManager("../../../../config.ini");

            //     int conn = 0;
            //string section = "connections";
            //bool isRs485 = false;

            // Создаём DataModel и добавляем точку подключения в DataModel
            DM = new DataModel();

            RootObjectForConfig.LoadConfigSystemFromFile(DM, syncForParameterValue);
            ////////while (true)
            ////////{
            ////////    string type = manager.GetPrivateString(section, "type" + conn);
            ////////    if (type == RS_485)
            ////////    {
            ////////        isRs485 = true;
            ////////        int com = manager.GetPrivateInt(section, "com" + conn);
            ////////        int baud = manager.GetPrivateInt(section, "baud" + conn);
            ////////        int stopbits = manager.GetPrivateInt(section, "stopbits" + conn);
            ////////        int databits = manager.GetPrivateInt(section, "databits" + conn);
            ////////        int parity = manager.GetPrivateInt(section, "parity" + conn);

            ////////        if (com > 0 && baud >= 0 && stopbits >= 0 && databits >= 0 && parity >= 0)
            ////////        {
            ////////            // Создаём точку подключения через COM по шине RS-485
            ////////            Connection connection = new Connection();

            ////////            // Создаём экземпляр класса InterfaceRS485 для работы с COM
            ////////            connection.m_Interface = new InterfaceRS485();

            ////////            connection.m_Interface.InterfaceParams.Add(com); // номер COM-порта
            ////////            connection.m_Interface.InterfaceParams.Add(baud); // скорость
            ////////            connection.m_Interface.InterfaceParams.Add(databits); // бит данных
            ////////            connection.m_Interface.InterfaceParams.Add(parity); // паритет
            ////////            connection.m_Interface.InterfaceParams.Add(stopbits); // стоп бит

            ////////            DM.AddConnection(connection);
            ////////        }
            ////////    }
            ////////    else if (type == TCP_IP)
            ////////    {
            ////////        string ip = manager.GetPrivateString(section, "ip" + conn);
            ////////        int port = manager.GetPrivateInt(section, "port" + conn);
            ////////        int[] ipAsInt = new int[4];
            ////////        bool isValidIP = false;

            ////////        IPAddress _ip;
            ////////        if (IPAddress.TryParse(ip, out _ip) == true)
            ////////        {
            ////////            byte[] bytes = _ip.GetAddressBytes();
            ////////            for (int i = 0; i < ipAsInt.Length; i++)
            ////////                ipAsInt[i] = bytes[i];
            ////////            isValidIP = true;
            ////////        }

            ////////        if (port > 0 && isValidIP)
            ////////        {
            ////////            // Создаём точку подключения через TCP/IP по шине RS-485
            ////////            Connection connection = new Connection();

            ////////            // Создаём экземпляр класса InterfaceTcpIP
            ////////            connection.m_Interface = new InterfaceTcpIP();

            ////////            connection.m_Interface.InterfaceParams.Add(ipAsInt[0]); // IP:0
            ////////            connection.m_Interface.InterfaceParams.Add(ipAsInt[1]); // IP:1
            ////////            connection.m_Interface.InterfaceParams.Add(ipAsInt[2]); // IP:2
            ////////            connection.m_Interface.InterfaceParams.Add(ipAsInt[3]); // IP:3
            ////////            connection.m_Interface.InterfaceParams.Add(port); // TCP-порт

            ////////            DM.AddConnection(connection);
            ////////        }
            ////////    }
            ////////    else if (type == "")
            ////////        break;

            ////////    conn++;
            ////////}

            // Создаём экземпляр класса ProtocolModbusRTU для работы с ModbusRTU
            //IProtocol protocolModbusRTU = null;
            //if (isRs485)
            //    protocolModbusRTU = new ProtocolModbusRTU();

            //int device = 0;
            //section = "devices";

            //while (true)
            //{
            //    int number = manager.GetPrivateInt(section, "number" + device, -1);
            //    if (number >= 0)
            //    {
            //        int address = manager.GetPrivateInt(section, "address" + device);
            //        int conn_ = manager.GetPrivateInt(section, "conn" + device);
            //        int timeout0 = manager.GetPrivateInt(section, "timeout0_" + device);
            //        int timeout1 = manager.GetPrivateInt(section, "timeout1_" + device);
            //        int timeout2 = manager.GetPrivateInt(section, "timeout2_" + device);
            //        string type = manager.GetPrivateString(section, "type" + device);

            //        IDevice device_ = null;
            //        if (type == PW_PANEL)
            //        {
            //            // Создаём экземпляр класса DevicePW для панели PowerWizard
            //            device_ = new DevicePW(syncForParameterValue);
            //        }
            //        else if (type == PN_PANEL)
            //        {
            //            // Создаём экземпляр класса DevicePW для панели PowerWizard
            //            device_ = new DevicePN(syncForParameterValue);
            //        }

            //        if (device_ != null)
            //        {
            //            device_.Number = number; // Порядковый номер устройства
            //            device_.Address = address; // Адрес устройства 

            //            device_.ParamsForInterface.Clear();
            //            device_.ParamsForInterface.Add(timeout0); // Таймаут ответа устройства
            //            device_.ParamsForInterface.Add(timeout1); // Таймаут между запросами к устройству
            //            device_.ParamsForInterface.Add(timeout2); // Таймаут между 2 байтами ответа устройства

            //            if (conn_ < DM.Connections.Count)
            //            {
            //                if ((DM.Connections[conn_].m_Interface as InterfaceRS485) != null)
            //                    ((ModbusDevice)device_).Protocol = new ProtocolModbusRTU();
            //                else if ((DM.Connections[conn_].m_Interface as InterfaceTcpIP) != null)
            //                    ((ModbusDevice)device_).Protocol = new ProtocolModbusTCP();

            //                if (((ModbusDevice)device_).Protocol != null)
            //                    DM.Connections[conn_].m_Devices.Add(device_);
            //            }
            //        }
            //    }
            //    else if (number == -1)
            //        break;

            //    device++;
            //}

            // Подключаемся к устройствам через точки подключения
            DM.OpenConnections();

            // Запуск чтения параметров из всех устройств
            DM.StartReadParametesFromAllDevices();

            // Пример получения параметров для binding
            // string value0 = DM.Devices[0].Params[0].Value;
            // string value1 = DM.Devices[0].Params[1].Value;
        }
    }
}

