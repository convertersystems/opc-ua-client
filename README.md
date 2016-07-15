# Workstation.UaClient
*New!* Supports Universal Windows (UWP) and Windows Presentation Framework (WPF) applications.

Build a free HMI using OPC Unified Architecture and Visual Studio. With this library, your app can browse, read, write and subscribe to the live data published by the OPC UA servers on your network.

Get the companion Visual Studio extension 'Workstation.UaBrowser' and you can:
- Browse OPC UA servers directly from the Visual Studio IDE.
- Drag and drop the variables, methods and events onto your view model.
- Use XAML bindings to connect your UI elements to live data.


### Main Types
- UaTcpSessionClient - A client for browsing, reading, writing and subscribing to nodes of your OPC UA server. Connects and reconnects automatically. 100% asynchronous.
- Subscription - A base class for your view models. Works with UaTcpSessionClient to automatically create and delete subscriptions on the server. Delivers data change and event notifications to properties. Implements INotifyPropertyChanged.
- MonitoredItemAttribute - An attribute for properties that indicates the property will receive data change or event notifications from the server.

