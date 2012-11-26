/*!
* Copyright 2012, Sérgio Costa
* http://www.sergiocosta.me
*
* Please feel free to use, adapt, distribute and do whatever you wish with the code below.
* If this code helps you build something cool, a link to my blog (www.sergiocosta.me) somewhere on the credits would be nice, though.
*/

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
using Ibt.Ortc.Mobile.Extensibility; // ORTC reference
using Ibt.Ortc.Mobile.Api; // ORTC reference

namespace RealtimePercLoader
{
    public partial class MainPage : PhoneApplicationPage
    {
        #region Variables
            private IOrtcClient client; // ORTC client
            private string serverURL = "http://ortc-developers.realtime.co/server/2.1"; // ORTC Server URL
            private string channel = "PercentageChannel"; // ORTC Channel
            private string appKey = "ENTER_YOUR_KEY_HERE"; // Your Realtime Application Key (get your free key at www.realtime.co)
            private string privateKey = "YOUR_PRIVATE_KEY"; // Your Realtime Private Key
            private string authToken = "AUTHENTICATION_TOKEN"; // Authorization token. Not necessary if you're using a free developer account. Can be whatever you want (a GUID, for example)
            private double curVal = 0; // Current slider value (control variable to prevent sending the same value repeatedly)
        #endregion

        #region Methods
        
            /// <summary>
            /// Constructor
            /// </summary>
            public MainPage()
            {
                InitializeComponent();
                this.Loaded += new RoutedEventHandler(MainPage_Loaded); // To make sure we start processing only after we have successfuly loaded our app
            }

            /// <summary>
            /// Called when the app is loaded
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            void MainPage_Loaded(object sender, RoutedEventArgs e)
            {
                txtConnStatus.Text = "Authenticating on ORTC"; // Log

                // Add permissions to our dictionary
                var permissions = new Dictionary<String, ChannelPermissions>();
                permissions.Add(channel, ChannelPermissions.Write); // Not actually needed for a free account

                // Tries to authenticate
                Ibt.Ortc.Mobile.Api.Ortc.SaveAuthenticationAsync(serverURL, true, authToken, false, appKey, 3600, privateKey, permissions, (error, authenticated) =>
                {
                    if (error != null)
                    {
                        // Something went wrong. Throw an error.
                        throw error;
                    }
                    else
                    {
                        // Authentication OK -> proceed
                        txtConnStatus.Text = String.Format("Authenticated"); // Log
                        InitializeClient(); // Initializes our client
                        Connect(); // Connects us to the ORTC server
                    }
                });
            }

            /// <summary>
            /// Initializes our client
            /// </summary>
            private void InitializeClient()
            {
                var ortc = new Ibt.Ortc.Mobile.Api.Ortc();
                var factory = ortc.LoadOrtcFactory("IbtRealTimeSJ");
                client = factory.CreateClient();
            
                // Sets the actions for the various handlers

                // OnConnected
                client.OnConnected += (s) =>
                {
                    txtConnStatus.Text = String.Format("Connected to {0}", ((IOrtcClient)s).Url); // Sets the connection status
                    SubscribeChannel(); // Subscribes us to the ORTC channel
                };

                // OnDisconnected
                client.OnDisconnected += (s) =>
                {
                    txtConnStatus.Text = String.Format("Disconnected"); // Sets the connection status
                };

                // OnException
                client.OnException += (s, error) =>
                {
                    txbLog.Text += String.Format("Error: {0}{1}", error.Message, Environment.NewLine); // Log
                };

                // OnReconnecting
                client.OnReconnecting += (s) =>
                {
                    txtConnStatus.Text = String.Format("Disconnected"); // Sets the connection status
                    txbLog.Text = String.Format("Reconnecting"); // Log
                };

                client.OnReconnected += (s) =>
                {
                    txtConnStatus.Text = String.Format("Reconnected to {0}", ((IOrtcClient)s).Url); // Sets the connection status
                };

                client.OnSubscribed += (s, channel) =>
                {
                    txbLog.Text += String.Format("Channel {0} subscribed.{1}", channel, Environment.NewLine); // Log
                };

                client.OnUnsubscribed += (s, channel) =>
                {
                    txbLog.Text += String.Format("Channel {0} unsubscribed.{1}", channel, Environment.NewLine); // Log
                };
            }

            /// <summary>
            /// Connects us to the ORTC server
            /// </summary>
            private void Connect()
            {
                txtConnStatus.Text = String.Format("Connecting");
                client.ClusterUrl = serverURL;
                client.Connect(appKey, authToken);
            }

            /// <summary>
            /// Subscribes us to the ORTC channel
            /// </summary>
            private void SubscribeChannel()
            {
                client.Subscribe(channel, true, (s, chan, message) =>
                {
                    if (!message.Contains("xrtml")) // very basic (and rather stupid) way to verify if the message we received should be processed
                    {
                        sldSlider.Value = Convert.ToDouble(message); // Updates the slider value
                    }
                    txbLog.Text += String.Format("Message on channel {0}: {1}{2}", channel, message, Environment.NewLine); // Log
                });
            }

            /// <summary>
            /// Called the slider value is changed
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void sldSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            {
                txtPercentage.Text = string.Format("{0}%", Math.Round(sldSlider.Value, 0)); // Sets the percentage text
            }

            /// <summary>
            /// Called whenever we move the slider with the mouse
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void sldSlider_MouseMove(object sender, MouseEventArgs e)
            {
                // Checks our control variable to see if we need to process and send the new value
                if (Math.Round(sldSlider.Value, 0) != Math.Round(curVal, 0))
                {
                    // We do -> proceed
                    sldSlider.Value = Math.Round(sldSlider.Value, 0); // Updates the slider
                    string message = string.Format(@"{{""xrtml"":{{""t"":""perc"",""a"":""delegate"",""d"":{{""v"":{0}}}}}}}", sldSlider.Value); // Creates the xRTML message
                    txbLog.Text += string.Format("Sending message {0}{1}", message, Environment.NewLine); // Log
                    client.Send(channel, message); // Sends the xRTML message to our channel
                    curVal = sldSlider.Value; // Updates our control variable
                }
            }

        #endregion
    }
}