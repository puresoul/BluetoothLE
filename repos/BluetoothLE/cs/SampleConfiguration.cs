using System;
using System.Collections.Generic;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Windows.UI.Xaml.Controls;

namespace SDKTemplate
{
    public partial class MainPage : Page
    {
        public const string FEATURE_NAME = "Bluetooth Low Energy C# Sample";

        List<Scenario> scenarios = new List<Scenario>
        {
          new Scenario() { Title="Server: Publish foreground", ClassType=typeof(MainPage) },
        };

        public string SelectedBleDeviceId = "BluetoothLE#BluetoothLE68:17:29:f9:ae:3d-18:7a:93:0e:79:31";
        public string SelectedBleDeviceName = "Chipsea-BLE";

    }

    public class Scenario
    {
        public string Title { get; set; }
        public Type ClassType { get; set; }
    }
}
