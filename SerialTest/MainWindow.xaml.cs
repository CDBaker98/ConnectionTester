using System;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace SerialTest {

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        #region "Global Variables"

        static SerialPort _serialPort = new SerialPort();
        static Socket _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private readonly BoundProperties _BoundProperties = new BoundProperties();

        #endregion

        public MainWindow() {
            InitializeComponent();

            // Set the data context to the BoundProperties class
            DataContext = _BoundProperties;

            // Populate the Port Name combobox
            string[] validPorts = SerialPort.GetPortNames();
            foreach (string s in validPorts) {
                cmbPortName.Items.Add(s);
            }
        }

        /// <summary>
        /// Make sure all ports/sockets are closed on window closing.
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            if (_serialPort.IsOpen) {
                _serialPort.Close();
            }
            if (_socket.Connected) {
                _socket.Dispose();
            }
        }

        #region "Serial Connection"

        /// <summary>
        /// Connect to the COM port given using the settings selected by the user.
        /// </summary>
        private void SerialConnection_Main(object sender, RoutedEventArgs e) {
            // Create a new SerialPort object
            _serialPort.Dispose();

            string stopBits = "";
            switch (cmbStopBits.Text) {
                case "0":
                    stopBits = "None";
                    break;
                case "1":
                    stopBits = "One";
                    break;
                case "1.5":
                    stopBits = "OnePointFive";
                    break;
                case "2":
                    stopBits = "Two";
                    break;
            }

            try {
                _serialPort = new SerialPort {
                    PortName = cmbPortName.Text,
                    BaudRate = int.Parse(cmbBaudRate.Text),
                    Parity = (Parity)Enum.Parse(typeof(Parity), cmbParity.Text),
                    DataBits = int.Parse(cmbDataBits.Text),
                    StopBits = (StopBits)Enum.Parse(typeof(StopBits), stopBits),
                    Handshake = (Handshake)Enum.Parse(typeof(Handshake), cmbHandshake.Text),
                    RtsEnable = cmbRtsEnable.Text == "True",

                    // User will never need to change these
                    ReadTimeout = 2000,
                    WriteTimeout = 2000
                };
            }
            catch (Exception ex) {
                MessageBox.Show("One or more provided values are bad:\r\n" + ex.Message, "Command Error", MessageBoxButton.OK);
                return;
            }

            try {
                // DataReceived listens on the port and fires the event NewSerialData when new data arrives
                _serialPort.DataReceived += new SerialDataReceivedEventHandler(NewSerialData);

                _serialPort.Open();
                LogSerial("Serial Port openned successfully!");
            }
            catch (Exception ex) {
                MessageBox.Show("Failed to open serial port:\r\n" + ex.Message, "Command Error", MessageBoxButton.OK);
            }
        }

        private void NewSerialData(object sender, SerialDataReceivedEventArgs e) {
            System.Threading.Thread.Sleep(50);
            LogSerial(_serialPort.ReadExisting().Trim());
        }

        private void BtnSendSerial_Click(object sender, RoutedEventArgs e) {
            if (txtCommand.Text == "") {
                MessageBox.Show("Make sure you enter in a command!", "Command Error", MessageBoxButton.OK);
                return;
            }

            string PostFix = "";
            if ((bool)rbCRSerial.IsChecked) { PostFix = "\r"; }
            else if ((bool)rbLFSerial.IsChecked) { PostFix = "\n"; }
            else if ((bool)rbCRLFSerial.IsChecked) { PostFix = "\r\n"; }

            // Regex allows the user to enter escape character into the text box
            byte[] CommandBytes = Encoding.ASCII.GetBytes(Regex.Unescape(txtCommand.Text) + PostFix);

            try {
                _serialPort.Write(CommandBytes, 0, CommandBytes.Length);
            }
            catch (Exception ex) {
                MessageBox.Show("Failed to send command!\r\n" + ex.Message, "Command Error", MessageBoxButton.OK);
            }

            if ((bool)cbResetCommand.IsChecked) {
                txtCommand.Text = "";
            }
        }

        private void BtnKillConnection_Click(object sender, RoutedEventArgs e) {
            if (_serialPort.IsOpen) {
                _serialPort.Close();
                LogSerial("Serial Port closed.");
            }
            else {
                MessageBox.Show("No Serial Port is currently open.");
            }
        }

        private void LogSerial(string message) {
            _BoundProperties.SerialResponse += message + Environment.NewLine;
        }

        private void txtResponseSerial_TextChanged(object sender, TextChangedEventArgs e) {
            txtResponseSerial.ScrollToEnd();
        }

        #endregion

        #region "Socket Connection"

        /// <summary>
        /// Connect to the IP address and Port number given.
        /// Creates different type of socket based on whether TCP or UDP was selected.
        /// </summary>
        private void BtnConnect_Click(object sender, RoutedEventArgs e) {
            _socket.Dispose();

            // Open certain type of port based on whether TCP or UDP was selected
            if ((bool)rbTCPSocket.IsChecked) {
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) {
                    ReceiveTimeout = 500,
                    SendTimeout = 500
                };
            }
            else if ((bool)rbUDPSocket.IsChecked) {
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp) {
                    ReceiveTimeout = 500,
                    SendTimeout = 500
                };
            }
            // Display a message and quit the function if neither were selected
            else {
                MessageBox.Show("Please select a Socket Type.");
                return;
            }

            // Create the connection
            try {
                _socket.Connect(new IPEndPoint(IPAddress.Parse(txtIP.Text), int.Parse(txtPort.Text)));
            }
            catch (Exception ex) {
                MessageBox.Show("Failed to connect to endpoint!\r\n" + ex.Message, "Connection Error", MessageBoxButton.OK);
                return;
            }

            LogSocket("Socket Connected!");
            GetData();
        }

        /// <summary>
        /// Send data over the socket connection.
        /// </summary>
        private void BtnSendSocket_Click(object sender, RoutedEventArgs e) {
            // Error out if there is no connection
            if (!_socket.Connected) {
                MessageBox.Show("Please connect first!\r\n", "Connection Error", MessageBoxButton.OK);
                return;
            }

            // Reset postfix and add it to the end of the command
            string PostFix = "";
            if ((bool)rbCRSocket.IsChecked) { PostFix = "\r"; }
            else if ((bool)rbLFSocket.IsChecked) { PostFix = "\n"; }
            else if ((bool)rbCRLFSocket.IsChecked) { PostFix = "\r\n"; }

            byte[] SendBytes = Encoding.ASCII.GetBytes(Regex.Unescape(txtCommandSocket.Text) + PostFix);

            //Try to send it
            try {
                _socket.Send(SendBytes);
                GetData();
            }
            catch (SocketException ex) { //Hopefully catch the exception...
                MessageBox.Show("Failed to send!\r\n" + ex.Message, "Connection Error", MessageBoxButton.OK);
            }
        }

        /// <summary>
        /// Output the response from the socket connection to the UI.
        /// </summary>
        private void GetData() {
            byte[] ReturnBytes = new byte[1024];

            try {
                int NumberOfBytesReceived = _socket.Receive(ReturnBytes);

                // Convert from bytes to ASCII characters
                string DataString = Encoding.ASCII.GetString(ReturnBytes, 0, NumberOfBytesReceived);

                // Add the response to the end of the currently displayed text
                LogSocket(DataString.Trim());
           }
            catch (Exception ex) {
                MessageBox.Show("Failed to receive!\r\n" + ex.Message, "Connection Error", MessageBoxButton.OK);
            }
        }

        /// <summary>
        /// Clear the socket response textbox.
        /// </summary>
        private void BtnClear_Click(object sender, RoutedEventArgs e) {
            _BoundProperties.SocketResponse = "";
        }

        private void LogSocket(string message) {
            _BoundProperties.SocketResponse += message + Environment.NewLine;
        }

        private void txtResponseSocket_TextChanged(object sender, TextChangedEventArgs e) {
            txtResponseSocket.ScrollToEnd();
        }

        #endregion

    }
}
