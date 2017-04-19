![robot][1]

# opc-ua-client
Install package 'Workstation.UaClient' from Nuget to get the latest release for your hmi project.

Supports Universal Windows Platform (UWP), Windows Presentation Framework (WPF) and now Xamarin Forms applications.

Build a free HMI using OPC Unified Architecture and Visual Studio. With this library, your app can browse, read, write and subscribe to the live data published by the OPC UA servers on your network.

Get the companion Visual Studio extension 'Workstation.UaBrowser' and you can:
- Browse OPC UA servers directly from the Visual Studio IDE.
- Drag and drop the variables, methods and events onto your view model.
- Use XAML bindings to connect your UI elements to live data.

### Main Types
- UaTcpSessionChannel - A channel for sending requests to your OPC UA server using the UaTcp binary protocol. Supports security up to Basic256Sha256. 100% asynchronous.
- UaApplication - A service for managing the channels to your OPC UA servers. Connects and reconnects automatically. 100% asynchronous.
- SubscriptionBase - A base class for view models that receive data change or event notifications from the server.
- SubscriptionAttribute - An attribute for your view models to specify the server and subscription properties.
- MonitoredItemAttribute - An attribute for your properties that indicates the property will receive data change or event notifications from the server.

```
    public partial class App : Application
    {
        private ILoggerFactory loggerFactory;
        private UaApplication application;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Setup a logger.
            this.loggerFactory = new LoggerFactory();
            this.loggerFactory.AddDebug(LogLevel.Trace);

            // Build and run an OPC UA application instance.
            this.application = new UaApplicationBuilder()
                .UseApplicationUri(@"urn:%COMPUTERNAME%:Workstation.StatusHmi")
                .UseDirectoryStore(@"%LOCALAPPDATA%\Workstation.StatusHmi\pki")
                .UseIdentityProvider(this.ShowSignInDialog)
                .UseLoggerFactory(this.loggerFactory)
                .AddEndpoint("PLC1", StatusHmi.Properties.Settings.Default.EndpointUrl, SecurityPolicyUris.None)
                .Build();

            this.application.Run();

            // Create and show the main view.
            var view = new MainView();
            view.Show();
            base.OnStartup(e);
        }
		...
    }
    
    [Subscription(endpointName: "PLC1", publishingInterval: 500, keepAliveCount: 20)]
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
### Releases

v2.0.0 Introduce SubscriptionBase and UaApplication. UaTcpSessionChannel now Publishes automatically.

v1.5.0 Added support for Xamarin Forms. Introduced ICertificateStore and DirectoryStore.

v1.4.0 UaTcpSessionClient now calls an asynchronous function you provide when connecting to servers that request a UserNameIdentity. Depreciated ISubscription and replaced with SubscriptionAttribute to specify Subscription parameters.  If ViewModelBase implements ISetDataErrorInfo and INotifyDataErrorInfo then it will record any error messages that occur when creating, writing or publishing a MonitoredItem. Diagnostics now use EventSource for logging. Added Debug, Console and File EventListeners. 

v1.3.0 Depreciated Subscription base class in favor of ISubscription interface to allow freedom to choose whatever base class you wish for your view models.
   
v1.2.0 Client, Subscription and Channel class constructors have new optional arguments. Corresponding property setters are removed to prevent changes after construction. Fixed some threading issues: Subscription's publish on thread pool, viewmodel's update on dispatcher thread. 

v1.1.0 Simplified Subscription base class to automatically subscribe for data change and event notifications when constructed, re-subscribe if the server reboots, and un-subscribe when garbage collected.   

v1.0.0 First commit. Includes UaTcpSessionChannel for 'opc.tcp' servers. Supports security up to Basic256Sha256. Automatically creates self-signed X509Certificate with 2048bit key.

[1]: robot6.jpg  
