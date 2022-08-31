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
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WinForms = System.Windows.Forms;
using System.Windows.Forms.Integration;
using LiveCharts;
using LiveCharts.Wpf;
using Ookii.Dialogs.Wpf;
using System.Reflection.Emit;
using System.ComponentModel;
using System.Timers;
using System.Windows.Threading;
using LiveCharts.Defaults;

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
        List<TextBox> textBoxList;
        string currentFileName = "";
        string currentFilePath="";
        string currentFolderPath="";
        FileStream currentFile;
        bool isReset = true;
        private readonly BackgroundWorker worker = new BackgroundWorker();
        String[] dataArray;
        DispatcherTimer timer;
        Stopwatch stopWatch = new Stopwatch();
        
        public SeriesCollection SeriesCollection { get; set; }
        public string[] BarLabels { get; set; }
        public Func<double, string> BarFormatter { get; set; }
        double timeCount = 0.0;
        public MainWindow()
        {
            InitializeComponent();
            InitializeSerialPorts();
            InitializeGUI();
            textBoxList = new List<TextBox>();
            textBoxList.Add(data1TextBox);
            textBoxList.Add(data2TextBox);
            textBoxList.Add(data3TextBox);
            Init_Graph();
        }

        private void UpdateGraph(string dat)
        {
            timeCount += 0.5;
            SeriesCollection[0].Values.Add(new ObservablePoint(timeCount, Double.Parse(dat)));

            if (SeriesCollection[0].Values.Count > 50)
            {
                SeriesCollection[0].Values.RemoveAt(0);
            }
        }

        private void Init_Graph()
        {
            SeriesCollection = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Series 1",
                    Values = new ChartValues<ObservablePoint> {new ObservablePoint(0,0)},
                },
            };
            BarFormatter = value => value.ToString();
            DataContext = this;
            
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

        private void InitializeGUI()
        {
            resetbtn.IsEnabled = false;
            folderPathTxtBox.Text = "No Folder Choosen";
        }

        private bool createNewFile()
        {
            string timeStamp = DateTime.Now.ToString("HH-mm-sstt,dd-MM-yyyy");
            currentFileName = timeStamp + ".csv";
            currentFilePath = folderPathTxtBox.Text + '\\' + currentFileName;
            if (currentFolderPath == "")
            {
                MessageBox.Show("Choose a Folder to store data files in", "Choose a Folder", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }
            else
            {
                try
                {
                    FileStream fs = File.Create(currentFilePath);
                    fs.Close();
                    return true;
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), "Error While Creating File", MessageBoxButton.OK, MessageBoxImage.Stop);
                    return false;
                }
            }
        }

        private bool OpenFile()
        {
            if (isReset)
            {
                if(!createNewFile()) return false;
            }
            try
            {
                currentFile = File.Open(currentFilePath, FileMode.Append);
                isReset = false;
                resetbtn.IsEnabled = false;
                folderPathTxtBox.IsEnabled = false;
                browseBtn.IsEnabled = false;
                return true;
            }
            catch(DirectoryNotFoundException)
            {
                MessageBox.Show("The File Path Could Not be Found!", "Not Found", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("Unable to open the specified file due to authorization issues", "UnAuthorized", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }
            catch(Exception e)
            {
                MessageBox.Show(e.ToString());
                return false;
            }
            
        }

        private void CloseFile()
        {
            currentFile.Close();
            resetbtn.IsEnabled = true;
        }

        private void logDataInCSV(string data)
        {
            data = data.Replace(' ', ',');
            Byte[] info = new UTF8Encoding(true).GetBytes(data + '\n');
            currentFile.Write(info, 0, info.Length);
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
                refreshBtn.IsEnabled = false;
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
            serialPort.DataReceived -= DataReceivedHandler;
            serialPort.Close();
            startBtn.Content = "Start";
            isConnectedToCOM = false;
            portNamesCmbBox.IsEnabled = true;
            refreshBtn.IsEnabled = true;
        }

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            if (!serialPort.IsOpen) return;
            SerialPort sp = (SerialPort)sender;
            string data = "";
            char c = '.';
            byte[] buffer = new byte[1];
            try
            {
                while (true)
                {
                    sp.Read(buffer, 0, 1);
                    c = Convert.ToChar(buffer[0]);
                    if (c == '\n')
                    {
                        break;
                    }
                    data += c;

                }
            }
            catch (Exception ex)
            {
                return;
            }
            dataArray = data.Split(' ');
            if (dataArray.Length != 3) return;
            if(stopWatch.ElapsedMilliseconds > 100)
            {
                stopWatch.Restart();
                Thread newThread = new Thread(() => UpdateGraph(dataArray[0]));
                newThread.Start();
            }

            logDataInCSV(data);
            Dispatcher.BeginInvoke(new Action(() =>
            {
                int a = dataArray.Length;
                if (a > 2)
                {
                    textBoxList[0].Text = dataArray[0];
                    textBoxList[1].Text = dataArray[1];
                    textBoxList[2].Text = dataArray[2];
                }
            }));
        }

        private void refreshBtn_Click(object sender, RoutedEventArgs e)
        {
            InitializeSerialPorts();
        }

        private void startBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnectedToCOM)
            {
                bool success = false;
                success = OpenFile();
                if (success)
                {
                    ConnectToCOMPort();
                    //Thread.Sleep(5000);
                    stopWatch.Start();
                }
            }
            else
            {
                DisconnectFromCOMPort();
                CloseFile();
                stopWatch.Stop();
            }
        }

        private void resetbtn_Click(object sender, RoutedEventArgs e)
        {
            isReset = true;
            browseBtn.IsEnabled = true;
            folderPathTxtBox.IsEnabled = true;
        }

        private void browseBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            dialog.Description = "Choose the folder to store data file";
            dialog.UseDescriptionForTitle = true;
            dialog.ShowNewFolderButton = true;
            bool? success = dialog.ShowDialog();
            if(success == true)
            {
                currentFolderPath = dialog.SelectedPath;
                folderPathTxtBox.Text = currentFolderPath;
            }
        }
    }
}
