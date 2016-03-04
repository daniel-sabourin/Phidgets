using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;

using System.Device.Location;
using Microsoft.Phone.Controls.Maps;
using Microsoft.Phone.Tasks;

using GroupLab.iNetwork;
using GroupLab.iNetwork.Tcp;
using Microsoft.Devices;
using System.Threading;


namespace iNetworkPhoneClient
{
    public partial class MainPage : PhoneApplicationPage
    {
        private Connection _connection;
        private string _ipAddress = "192.168.1.71";
        private int _port = 12345;

        private GeoCoordinateWatcher gps = null;
        private Pushpin locationPin;
        private Pushpin clickedPin;

        private TimeSpan totalTime;
        private Timer remainingTimer;
        private double remainingTime;

        // Our current gps location
        public GeoCoordinate mostRecentPosition { get; set; }

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
                        case "Tap":
                            // do something here

                            BuzzPhone();
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message + "\n" + e.StackTrace);
            }

        }

        #endregion

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            InitializeConnection();
            InitializeGPS();
        }

        public void BuzzPhone()
        {
            VibrateController vibrate = VibrateController.Default;
            vibrate.Start(TimeSpan.FromSeconds(0.1));
        }

        private void InitializeGPS()
        {
            if (gps == null)
            {
                gps = new GeoCoordinateWatcher(GeoPositionAccuracy.High);

                // Register for position change events (i.e., when we move) and 
                // status changed events
                gps.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(gps_PositionChanged);
            }

            // Start the GPS sensor
            gps.Start();
        }

        bool firstGPS = true;
        void gps_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            mostRecentPosition = e.Position.Location;

            if (firstGPS)
            {
                locationPin = new Pushpin();
                locationPin.Background = new SolidColorBrush(Colors.Green);
                
                myMap.Children.Add(locationPin);

                firstGPS = false;

                this.myMap.Center = mostRecentPosition;
                this.myMap.ZoomLevel = 16;
            }

            locationPin.Location = mostRecentPosition;
        }

        private void startStopButton_Click(object sender, RoutedEventArgs e)
        {
            StartStop();
        }

        public void StartStop()
        {
            if (startStopButton.Content.ToString().Equals("Start"))
            {
                // Start
                startStopButton.Content = "Stop";

                if (totalTime != null)
                {
                    remainingTimer = new Timer(delegate(object state)
                    {
                        remainingTime -= 1;
                        TimeSpan timeSpan = TimeSpan.FromSeconds(remainingTime);

                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            if (timeSpan.Ticks > 0)
                                etaTextBlock.Text = "ETA " + new DateTime(timeSpan.Ticks).ToString("mm'm' ss's'");
                        });

                        Message msg = new Message("RemainingTime");
                        msg.AddField("timespan", timeSpan.ToString());
                        this._connection.SendMessage(msg);

                        if (remainingTime <= 0)
                        {
                            Deployment.Current.Dispatcher.BeginInvoke(() =>
                            {

                                StartStop();
                            });
                        }

                    }, null, 1000, 1000);
                }
            }
            else
            {
                // Stop
                startStopButton.Content = "Start";
                remainingTimer.Dispose();
            }
        }

        private void myMap_Hold(object sender, GestureEventArgs e)
        {
            PlacePinAtTapLocation(e);
        }

        private void myMap_Tap(object sender, GestureEventArgs e)
        {
            PlacePinAtTapLocation(e);
        }

        private void PlacePinAtTapLocation(GestureEventArgs e)
        {
            GeoCoordinate pinPosition = this.myMap.ViewportPointToLocation(e.GetPosition(this.myMap));

            if (clickedPin == null)
            {
                clickedPin = new Pushpin();
                clickedPin.Background = new SolidColorBrush(Colors.Red);

                myMap.Children.Add(clickedPin);
            }

            clickedPin.Location = pinPosition;

            double d = MapFunctions.GetDistanceFromLatLon(pinPosition, mostRecentPosition);
            TimeSpan timeSpan = MapFunctions.CalculateTimeToTravelDistance(d, 60);
            totalTime = timeSpan;
            remainingTime = timeSpan.TotalSeconds;
            etaTextBlock.Text = "ETA " + new DateTime(timeSpan.Ticks).ToString("mm'm' ss's'");

            Message msg = new Message("InitialTime");
            msg.AddField("timespan", timeSpan.ToString());
            this._connection.SendMessage(msg);
        }

    }
}