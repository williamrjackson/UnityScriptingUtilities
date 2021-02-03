using UnityEngine;
using System.IO.Ports;
using System.Collections.Generic;
using System.Diagnostics;
using System;
using System.Threading;

namespace Wrj
{
    public class ArduinoSerialCommunication : MonoBehaviour
    {

        [SerializeField]
        private BaudRates BaudRate = BaudRates._9600;
        [SerializeField]
        private string deviceName = "Arduino";

        private string rawData;
        public string RawData { get { return rawData; } }
        public delegate void SerialEvent(string data);
        public SerialEvent OnSerialEvent;

        private Dictionary<string, string> portAssignments = new Dictionary<string, string>();
        private string comPort;

        SerialPort port;
        Thread serialPortListenerThread;

        public enum BaudRates
        {
            _300 = 300, _600 = 600, _1200 = 1200, _2400 = 2400, _4800 = 4800, _9600 = 9600,
            _14400 = 14400, _19200 = 19200, _28800 = 28800, _38400 = 38400, _57600 = 57600, _115200 = 115200
        }

        readonly object lockObject = new object();

        /// Static Singleton behavior
        protected static ArduinoSerialCommunication _instance;
        public static ArduinoSerialCommunication Instance
        {
            get
            {
                if (_instance == null)
                {
                    UnityEngine.Debug.LogError("ArduinoSerialCommunication not instantiated!");
                }
                return _instance;
            }
        }
        void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else
            {
                Destroy(this);
            }
        }
        void Start()
        {
            PopulatePortList();
            if ((comPort = GetFirstPortMatchingName(deviceName)) != null)
            {
                Connect();
            }
            else
            {
                UnityEngine.Debug.LogWarning("Arduino Not Found");
            }
        }

        private void OnDestroy()
        {
            if (serialPortListenerThread != null)
            {
                serialPortListenerThread.Abort();
                ClosePort();
            }
        }
        private void OnDisable()
        {
            if (serialPortListenerThread != null)
            {
                serialPortListenerThread.Abort();
                ClosePort();
            }
        }

        void Connect()
        {
            if (serialPortListenerThread != null)
            {
                serialPortListenerThread.Abort();
                ClosePort();
            }

            Utils.SafeTry(() => InitializeArduino(comPort, (int)BaudRate));

            serialPortListenerThread = new Thread(RecieveDataInHelperThread);
            serialPortListenerThread.Start();
        }

        public void ClosePort()
        {
            UnityEngine.Debug.Log("close port");
            Utils.SafeTry(() => port.Close());
        }

        void InitializeArduino(string listeningPort, int baudRate)
        {
            UnityEngine.Debug.LogFormat("Connecting to Arduino on port {0} with a baudrate of: {1}.", listeningPort, baudRate);
            Utils.SafeTry(() =>
            {
                port = new SerialPort(listeningPort, baudRate);
                port.Parity = Parity.None;
                port.StopBits = StopBits.One;
                port.DataBits = 8;
                port.Handshake = Handshake.None;
                port.Open();
            });
        }

        void RecieveDataInHelperThread()
        {
            while (port.IsOpen)
            {
                String str = port.ReadLine();
                if (!string.IsNullOrWhiteSpace(str))
                {
                    rawData = str;
                    if (OnSerialEvent != null)
                    {
                        OnSerialEvent(str);
                    }
                }
            }
        }

        // Send text to the serial port.
        public void SendData(string str)
        {
            Utils.SafeTry(() =>
            {
                port.Write(str);
            });
        }
        public void SendDataAsLine(string str)
        {
            Utils.SafeTry(() =>
            {
                port.WriteLine(str);
            });
        }

        private void PopulatePortList()
        {
            // Remove existing port entries
            portAssignments.Clear();
            // Run PowerShell process with Get-WMIObject Win32_SerialPort
            // Request DeviceID (port) and Description (device name)
            Process process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.FileName = "powershell.exe";
            process.StartInfo.Arguments = "Get-WMIObject Win32_SerialPort | Select-Object DeviceID,Description";
            process.Start();
            // Read each line of the output
            string lineOut;
            while ((lineOut = process.StandardOutput.ReadLine()) != null)
            {
                // If it contains "COM" parse out the int for the port number
                // and save the remainder of the line as the description
                if (lineOut.Contains("COM"))
                {
                    int firstSpacePos = lineOut.IndexOf(' ');
                    string port = lineOut.Substring(0, firstSpacePos).Trim();
                    string desc = lineOut.Substring(firstSpacePos).Trim();
                    // Add to the port list
                    portAssignments.Add(desc, port);
                }
            }
        }

        // Return the first port found with a name that matches a
        // specified substring.
        private string GetFirstPortMatchingName(string contains)
        {
            foreach (KeyValuePair<string, string> entry in portAssignments)
            {
                if (entry.Key.Contains(contains))
                {
                    return entry.Value;
                }
            }
            return null;
        }
    }
}