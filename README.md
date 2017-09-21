![robot][1]

# opc-ua-client
Install package 'Workstation.UaClient' from Nuget to get the latest release for your hmi project.

Supports Universal Windows Platform (UWP), Windows Presentation Framework (WPF) and Xamarin applications.

Build a HMI using OPC Unified Architecture and Visual Studio. With this library, your app can browse, read, write and subscribe to the live data published by the OPC UA servers on your network.

Get the companion Visual Studio extension 'Workstation.UaBrowser' and you can:
- Browse OPC UA servers directly from the Visual Studio IDE.
- Drag and drop the variables, methods and events onto your view model.
- Use XAML bindings to connect your UI elements to live data.

### Main Types
- UaTcpSessionChannel - A channel for sending requests to your OPC UA server using the UaTcp binary protocol. Supports security up to Basic256Sha256. 100% asynchronous.
- UaApplication - A service for managing the channels to your OPC UA servers. Connects and reconnects automatically. 100% asynchronous.
- SubscriptionBase - A base class for view models that receive data change or event notifications from the server.
- SubscriptionAttribute - An attribute for your view models to specify the OPC UA server and subscription properties.
- MonitoredItemAttribute - An attribute for your properties that indicates the property will receive data change or event notifications from the server.

```
    public partial class App : Application
    {
        private UaApplication application;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Build and run an OPC UA application instance.
            this.application = new UaApplicationBuilder()
                .SetApplicationUri($"urn:{Dns.GetHostName()}:Workstation.StatusHmi")
                .SetDirectoryStore($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\Workstation.StatusHmi\\pki")
                .SetIdentity(this.ShowSignInDialog)
                .ConfigureLoggerFactory(o => o.AddDebug(LogLevel.Trace))
                .Build();

            this.application.Run();

            // Create and show the main view.
            var view = new MainView();
            view.Show();
        }
		...
    }
    
    [Subscription(endpointUrl: "opc.tcp://localhost:26543", publishingInterval: 500, keepAliveCount: 20)]
    public class MainViewModel : SubscriptionBase
    {
        /// <summary>
        /// Gets the value of ServerServerStatus.
        /// </summary>
        [MonitoredItem(nodeId: "i=2256")]
        public ServerStatusDataType ServerServerStatus
        {
            get { return this.serverServerStatus; }
            private set { this.SetValue(ref this.serverServerStatus, value); }
        }

        private ServerStatusDataType serverServerStatus;
    }
```
[1]: robot6.jpg  
