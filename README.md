![robot][1]

# Workstation.UaClient
*New!* Supports Universal Windows Platform (UWP) and Windows Presentation Framework (WPF) applications.

Build a free HMI using OPC Unified Architecture and Visual Studio. With this library, your app can browse, read, write and subscribe to the live data published by the OPC UA servers on your network.

Get the companion Visual Studio extension 'Workstation.UaBrowser' and you can:
- Browse OPC UA servers directly from the Visual Studio IDE.
- Drag and drop the variables, methods and events onto your view model.
- Use XAML bindings to connect your UI elements to live data.

### Main Types
- UaTcpSessionClient - A client for browsing, reading, writing and subscribing to nodes of your OPC UA server. Connects and reconnects automatically. 100% asynchronous.
- Subscription - A base class for your view models. Works with UaTcpSessionClient to automatically create and delete subscriptions on the server. Delivers data change and event notifications to properties. Implements INotifyPropertyChanged.
- MonitoredItemAttribute - An attribute for properties that indicates the property will receive data change or event notifications from the server.

```
    public class AppDescription : ApplicationDescription
    {
        public AppDescription()
        {
            this.ApplicationName = "Workstation.RobotHmi";
            this.ApplicationUri = $"urn:{System.Net.Dns.GetHostName()}:Workstation.RobotHmi";
            this.ApplicationType = ApplicationType.Client;
        }
    }

    public class PLC1Service : UaTcpSessionClient
    {
        public PLC1Service(AppDescription description)
            : base(description, description.GetCertificate(), null, "opc.tcp://localhost:26543")
        {
        }
    }
    
    public class MyViewModel : Subscription
    {
        public MyViewModel(PLC1Service session)
            : base(session)
        {
        }

        /// <summary>
        /// Gets the value of ServerStatusCurrentTime.
        /// </summary>
        [MonitoredItem(nodeId: "i=2258")]
        public DateTime ServerStatusCurrentTime
        {
            get { return this.serverStatusCurrentTime; }
            private set { this.SetProperty(ref this.serverStatusCurrentTime, value); }
        }

        private DateTime serverStatusCurrentTime;

    }
```
### Releases
v1.2.0 Client, Subscription and Channel class constructors have new optional arguments. Corresponding property setters are removed to prevent changes after construction. Fixed some threading issues: Subscription's publish on thread pool, viewmodel's update on dispatcher thread. 
v1.1.0 Simplified Subscription base class to automatically subscribe for data change and event notifications when constructed, re-subscribe if the server reboots, and un-subscribe when garbage collected.   
v1.0.0 First commit. Includes UaTcpSessionChannel for 'opc.tcp' servers. Supports security up to Basic256Sha256. Automatically creates self-signed X509Certificate with 2048bit key.

[1]: robot6.jpg  