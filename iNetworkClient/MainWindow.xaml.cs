using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using GroupLab.iNetwork;
using GroupLab.iNetwork.Tcp;
using Phidgets;
using Phidgets.Events;
using System.Diagnostics;
using System.Windows.Threading;

namespace iNetworkClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Connection _connection;
        private string _ipAddress = "127.0.0.1";
        private int _port = 12345;

        private InterfaceKit _interfaceKit;
        private Servo _servo;

        private bool _tapStarted = false;

        private bool _lightSensorLEDOn;
        public bool LightSensorLEDOn
        {
            get { return _lightSensorLEDOn; }
            set
            {
                _lightSensorLEDOn = value;
                _interfaceKit.outputs[0] = value;

                if (LightSensorLEDOn)
                    lightSensorRectangle.Fill = Brushes.Red;
                else
                    lightSensorRectangle.Fill = Brushes.White;
            }
        }

        private TimeSpan totalTripTime;

        private int _servoMin = 30;
        private int _servoMax = 120;

        private int _ledArrayCount = 6;
        private int _ledArrayStart = 2;

        private double _progressPercent;
        public double ProgressPercent
        {
            get { return _progressPercent; }
            set
            {
                if (value > 1)
                    _progressPercent = 1;
                else if (value < 0)
                    _progressPercent = 0;
                else
                    _progressPercent = value;


                int numberToLight = (int)(ProgressPercent * _ledArrayCount);

                this.Dispatcher.Invoke((Action)(() =>
                {
                    LightLEDs(_interfaceKit.outputs, _ledArrayStart, numberToLight);
                    LightScreenLEDs(ledStackPanel, numberToLight);

                    _servo.servos[0].Position = NormalizeServoAngle(ProgressPercent, _servoMin, _servoMax);

                    progressSlider.Value = _progressPercent;
                }));
            }
        }


        /// <summary>
        /// Under this amount will be considered a tap
        /// </summary>
        private int _tapThreshold = 75;

        /// <summary>
        /// The time (in ms) that has to be held down to be considered a hold
        /// </summary>
        private int _holdThresholdTime = 500;

        private Stopwatch _stopWatch;

        #region iNetwork Methods

        private void InitializeConnection()
        {
            // connect to the server
            this._connection = new Connection(this._ipAddress, this._port);
            this._connection.Connected += new ConnectionEventHandler(OnConnected);
            this._connection.Start();
        }

        void OnConnected(object sender, ConnectionEventArgs e)
        {
            this._connection.MessageReceived += new ConnectionMessageEventHandler(OnMessageReceived);
        }

        private void OnMessageReceived(object sender, Message msg)
        {
            try
            {
                if (msg != null)
                {
                    switch (msg.Name)
                    {
                        default:
                            // don't do anything
                            break;
                        case "InitialTime":
                            // do something here
                            totalTripTime = TimeSpan.Parse(msg.GetStringField("timespan"));
                            break;

                        case "RemainingTime":
                            TimeSpan remainingTimeSpan = TimeSpan.Parse(msg.GetStringField("timespan"));
                            ProgressPercent = ((totalTripTime.TotalSeconds - remainingTimeSpan.TotalSeconds) / totalTripTime.TotalSeconds);

                            break;

                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "\n" + e.StackTrace);
            }

        }
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            InitializeConnection();

            _stopWatch = new Stopwatch();
        }

        private void InitializeServo()
        {
            _servo = new Servo();
            _servo.open();
        }

        private void InitializeInterfaceKit()
        {
            _interfaceKit = new InterfaceKit();
            _interfaceKit.open();
            _interfaceKit.waitForAttachment();
            _interfaceKit.SensorChange += delegate(object sender, SensorChangeEventArgs e)
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    LightSensorChange(e.Value);
                }));
            };
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeInterfaceKit();
            InitializeServo();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            for (int i = 0; i < _interfaceKit.outputs.Count; i++)
                _interfaceKit.outputs[i] = false;

            _interfaceKit.close();
            _servo.close();
        }

      
        public void LightSensorChange(int value)
        {
            if (value < _tapThreshold)
            {
                if (_tapStarted)
                {
                    // Continue holding
                }
                else
                {
                    // Tap Started
                    _tapStarted = true;
                    _stopWatch.Restart();

                    LightSensorLEDOn = true;
                    //Console.WriteLine("Started");

                }
            }
            else
            {
                if (_tapStarted)
                {
                    // End of tap
                    _tapStarted = false;
                    LightSensorLEDOn = false;

                    if (_stopWatch.ElapsedMilliseconds > _holdThresholdTime)
                    {
                        // Hold
                    }
                    else
                    {
                        // Tap
                        Message msg = new Message("Tap");
                        this._connection.SendMessage(msg);
                    }

                }
                else
                {
                    // Nothing really happened
                }
            }
        }

        private void lightSensorButton_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            LightSensorChange(500);
        }

        private void lightSensorButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            LightSensorChange(50);
        }

        private void progressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ProgressPercent = e.NewValue;
        }

        public double NormalizeServoAngle(double percentage, int start, int end)
        {
            return end - ((end - start) * percentage);
        }

        public void LightLEDs(InterfaceKitDigitalOutputCollection array, int start, int number)
        {
            for (int i = start; i < array.Count; i++)
            {
                if (i < number + start)
                    array[i] = true;
                else
                    array[i] = false;
            }
        }

        public void LightScreenLEDs(StackPanel stackPanel, int number)
        {
            for (int i = 0; i < stackPanel.Children.Count; i++)
            {
                if (i < number)
                    stackPanel.Children[i].Visibility = System.Windows.Visibility.Visible;
                else
                    stackPanel.Children[i].Visibility = System.Windows.Visibility.Hidden;
            }

        }
    }
}
