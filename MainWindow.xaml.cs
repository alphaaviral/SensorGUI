using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LiveCharts;
using LiveCharts.Wpf;

namespace sensorGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SerialPort serialPort;
        string[] serialPorts;
        bool isConnectedToCOM = false;
        public MainWindow()
        {
            InitializeComponent();
            InitializeSerialPorts();
        }

        private void GetVarNames()
        {

        }

        private void InitializeSerialPorts()
        {
            serialPorts = SerialPort.GetPortNames();
            portNamesCmbBox.Items.Clear();
            if (serialPorts.Count() != 0)
            {
                portNamesCmbBox.IsEnabled = true;
                startBtn.IsEnabled = true;
                foreach (string serial in serialPorts)
                {
                    portNamesCmbBox.Items.Add(serial);
                }
                portNamesCmbBox.SelectedItem = serialPorts[0];
            }
            else
            {
                portNamesCmbBox.Items.Add("No Serial Port Found");
                portNamesCmbBox.SelectedItem = "No Serial Port Found";
                portNamesCmbBox.IsEnabled = false;
                startBtn.IsEnabled = false;
            }
            if (isConnectedToCOM) portNamesCmbBox.IsEnabled = false;

        }

        private void ConnectToCOMPort()
        {
            try
            {
                string selectedSerialPort = portNamesCmbBox.SelectedItem.ToString();
                serialPort = new SerialPort(selectedSerialPort, 38400);              
                serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
                serialPort.Open();
                startBtn.Content = "Stop";
                isConnectedToCOM = true;
                portNamesCmbBox.IsEnabled = false;
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("The selected serial port is busy!", "Busy", MessageBoxButton.OK, MessageBoxImage.Stop);
            }
            catch (NullReferenceException)
            {
                MessageBox.Show("The selected serial port does not exist!", "Serial Port not found", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        private void DisconnectFromCOMPort()
        {
            serialPort.Close();
            startBtn.Content = "Start";
            isConnectedToCOM = false;
            portNamesCmbBox.IsEnabled = true;
        }

        private void refreshBtn_Click(object sender, RoutedEventArgs e)
        {
            InitializeSerialPorts();
        }

        private void startBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnectedToCOM) ConnectToCOMPort();
            else DisconnectFromCOMPort();
        }

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            if (!serialPort.IsOpen) return;
            SerialPort sp = (SerialPort)sender;
            //string indata = sp.ReadExisting();
            string data="";
            char c = '.';
            byte[] buffer = new byte[1];
            while (true)
            {
                sp.Read(buffer, 0, 1);
                c = Convert.ToChar(buffer[0]);
                if (c=='\n'){
                    break;
                }
                data += c;
                
            }

            //using (FileStream fs = File.Open("D:\\OneDrive - IIT Delhi\\Documents\\TK Gandhi Project\\file2.txt", FileMode.Append, FileAccess.Write, FileShare.None))
            //{
            //    Byte[] info = new UTF8Encoding(true).GetBytes(data+'\n');
            //    // Add some information to the file.
            //    fs.Write(info, 0, info.Length);
            //}
            String[] dataArray = data.Split(' ');
            Debug.Print("Data Received:");
            Debug.Print(data);
            Dispatcher.BeginInvoke(new Action(() =>
                    {
                        data1TextBox.Text = dataArray[0];
                        data2TextBox.Text = dataArray[1];
                        data3TextBox.Text = dataArray[2];
                    }));
        }

        private void portNamesCmbBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            portNamesCmbBox.Items.Remove("Select a COM Port");
        }

        //private void readData()
        //{
        //    try
        //    {
        //        while (isConnectedToCOM)
        //        {

        //            Dispatcher.BeginInvoke(new Action(() =>
        //            {

        //                dataTextBox.Text = dataTextBox.Text + Environment.NewLine + serialPort.ReadExisting();
        //                dataTextBox.ScrollToEnd();
        //            }));

        //            newThread.Join(10);

        //        }
        //    }
        //    catch (ThreadAbortException)
        //    {

        //    }
        //    catch(Exception e)
        //    {

        //    }
        //}
    }
}
