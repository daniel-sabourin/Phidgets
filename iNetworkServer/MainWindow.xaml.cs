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

namespace iNetworkServer
{
    public partial class MainWindow : Window
    {
        private Server _server;
        private List<Connection> _clients;

        #region iNetwork Methods

        public void InitializeServer()
        {
            this._clients = new List<Connection>();

            // Create a new server, add name and port number
            this._server = new Server("ServerName", 12345);
            this._server.IsDiscoverable = true;
            this._server.Connection += new ConnectionEventHandler(OnServerConnection);

            this._server.Start();

            // Display the server info on the label
            this.NetInfo.Content = "IP: " + this._server.Configuration.IPAddress.ToString() + ", Port: " + this._server.Configuration.Port.ToString();
        }

        private void OnServerConnection(object sender, ConnectionEventArgs e)
        {

            if (e.ConnectionEvent == ConnectionEvents.Connect)
            {
                // new client connected
                lock (this._clients)
                {
                    if (!(this._clients.Contains(e.Connection)))
                    {
                        this._clients.Add(e.Connection);
                        e.Connection.MessageReceived += new ConnectionMessageEventHandler(OnMessageReceived);

                        this.Dispatcher.Invoke(
                            new Action(
                                delegate()
                                {
                                    this.clientList.Items.Add(e.Connection.ToString());
                                }));
                    }
                }
            }
            else if (e.ConnectionEvent == ConnectionEvents.Disconnect)
            {
                // client disconnected
                lock (this._clients)
                {
                    if (this._clients.Contains(e.Connection))
                    {
                        this._clients.Remove(e.Connection);
                        e.Connection.MessageReceived -= new ConnectionMessageEventHandler(OnMessageReceived);

                        this.Dispatcher.Invoke(
                            new Action(
                                delegate()
                                {
                                    this.clientList.Items.Remove(e.Connection.ToString());
                                }));
                    }
                }
            }
        }

        private void OnMessageReceived(object sender, Message msg)
        {
            this.Dispatcher.Invoke(
                new Action(
                    delegate()
                    {
                        if (msg != null)
                        {
                            this.messages.Text += msg.Name + "\n";
                            switch (msg.Name)
                            {
                                default:
                                    // don't do anything
                                    break;
                                // add cases with the message names
                                case "Tap":
                                    // broadcast to all
                                    // this._server.BroadcastMessage(msg);
                                    // broadcast to all but server
                                    this._server.BroadcastMessage(msg, (Connection)sender);
                                    break;
                                case "InitialTime":
                                    this._server.BroadcastMessage(msg, (Connection)sender);
                                    break;
                                case "RemainingTime":
                                    this._server.BroadcastMessage(msg, (Connection)sender);
                                    break;
                            }
                        }
                    }));
        }

        #endregion

        public MainWindow()
        {
            InitializeComponent();
            InitializeServer();
            this.Closing += new System.ComponentModel.CancelEventHandler(OnClosing);
        }

        void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Ensure that the server terminates when the window is closed.
            if (this._server != null && this._server.IsRunning)
            {
                this._server.Stop();
            }
        }
    }
}
