using System;
using System.Collections.Generic;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;
using Windows.Storage.Pickers;
using Windows.Storage;



namespace SDKTemplate
{
    public sealed partial class MainPage : Page
    {
        public static MainPage Current;
        private ObservableCollection<BluetoothLEAttributeDisplay> ServiceCollection = new ObservableCollection<BluetoothLEAttributeDisplay>();
        private ObservableCollection<BluetoothLEAttributeDisplay> CharacteristicCollection = new ObservableCollection<BluetoothLEAttributeDisplay>();

        private BluetoothLEDevice bluetoothLeDevice = null;
        private GattCharacteristic selectedCharacteristic;
        private GattCharacteristic writeCharacteristic;
        private GattDeviceService selectedService;

        private GattCharacteristic registeredCharacteristic;
        private GattPresentationFormat presentationFormat;
        private ObservableCollection<BluetoothLEDeviceDisplay> KnownDevices = new ObservableCollection<BluetoothLEDeviceDisplay>();
        private List<DeviceInformation> UnknownDevices = new List<DeviceInformation>();
        private DeviceWatcher deviceWatcher;

        int caseSwitch = 1;
        int loaded = 0;
        int cnt = 1;

        #region Error Codes
        readonly int E_BLUETOOTH_ATT_WRITE_NOT_PERMITTED = unchecked((int)0x80650003);
        readonly int E_BLUETOOTH_ATT_INVALID_PDU = unchecked((int)0x80650004);
        readonly int E_ACCESSDENIED = unchecked((int)0x80070005);
        readonly int E_DEVICE_NOT_AVAILABLE = unchecked((int)0x800710df); // HRESULT_FROM_WIN32(ERROR_DEVICE_NOT_AVAILABLE)
        #endregion

        public MainPage()
        {
            InitializeComponent();

            Current = this;
            if (deviceWatcher == null)
            {
                StartBleDeviceWatcher();
                this.NotifyUser($"Device watcher started.", NotifyType.StatusMessage);
            }
            else
            {
                StopBleDeviceWatcher();
                this.NotifyUser($"Device watcher stopped.", NotifyType.StatusMessage);
            }
            this.Text.Text = "Mesure time\t\t\tMesured value\r\n--------------\t\t\t----------------\r\n";
            Connect();
            
        }

        #region Enumerating Services

        private async Task<bool> ClearBluetoothLEDeviceAsync()
        {
            if (subscribedForNotifications)
            {
                var result = await registeredCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
                if (result != GattCommunicationStatus.Success)
                {
                    return false;
                }
                else
                {
                    selectedCharacteristic.ValueChanged -= Characteristic_ValueChanged;
                    subscribedForNotifications = true;
                }
            }
            bluetoothLeDevice?.Dispose();
            bluetoothLeDevice = null;
            return true;

        }

        private DeviceInformation FindUnknownDevices(string id)
        {
            foreach (DeviceInformation bleDeviceInfo in UnknownDevices)
            {
                if (bleDeviceInfo.Id == id)
                {
                    return bleDeviceInfo;
                }
            }
            return null;
        }

        private BluetoothLEDeviceDisplay FindBluetoothLEDeviceDisplay(string id)
        {
            foreach (BluetoothLEDeviceDisplay bleDeviceDisplay in KnownDevices)
            {
                if (bleDeviceDisplay.Id == id)
                {
                    return bleDeviceDisplay;
                }
            }
            return null;
        }

        private async void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation deviceInfo)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                lock (this)
                {
                    //Debug.WriteLine(String.Format("Added {0}{1}", deviceInfo.Id, deviceInfo.Name));
                    if (sender == deviceWatcher)
                    {
                        if (FindBluetoothLEDeviceDisplay(deviceInfo.Id) == null)
                        {
                            if (deviceInfo.Name != string.Empty)
                            {
                                KnownDevices.Add(new BluetoothLEDeviceDisplay(deviceInfo));
                            }
                            else
                            {
                                UnknownDevices.Add(deviceInfo);
                            }
                        }

                    }
                }
            });
        }

        private async void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate deviceInfoUpdate)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                lock (this)
                {
                    //Debug.WriteLine(String.Format("Updated {0}{1}", deviceInfoUpdate.Id, ""));
                    if (sender == deviceWatcher)
                    {
                        BluetoothLEDeviceDisplay bleDeviceDisplay = FindBluetoothLEDeviceDisplay(deviceInfoUpdate.Id);
                        if (bleDeviceDisplay != null)
                        {
                            bleDeviceDisplay.Update(deviceInfoUpdate);
                            return;
                        }

                        DeviceInformation deviceInfo = FindUnknownDevices(deviceInfoUpdate.Id);
                        if (deviceInfo != null)
                        {
                            deviceInfo.Update(deviceInfoUpdate);
                            if (deviceInfo.Name != String.Empty)
                            {
                                KnownDevices.Add(new BluetoothLEDeviceDisplay(deviceInfo));
                                UnknownDevices.Remove(deviceInfo);
                            }
                        }
                    }
                }
            });
        }

        private async void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate deviceInfoUpdate)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                lock (this)
                {
                    //Debug.WriteLine(String.Format("Removed {0}{1}", deviceInfoUpdate.Id, ""));

                    if (sender == deviceWatcher)
                    {
                        BluetoothLEDeviceDisplay bleDeviceDisplay = FindBluetoothLEDeviceDisplay(deviceInfoUpdate.Id);
                        if (bleDeviceDisplay != null)
                        {
                            KnownDevices.Remove(bleDeviceDisplay);
                        }

                        DeviceInformation deviceInfo = FindUnknownDevices(deviceInfoUpdate.Id);
                        if (deviceInfo != null)
                        {
                            UnknownDevices.Remove(deviceInfo);
                        }
                    }
                }
            });
        }

        private async void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object e)
        {

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (sender == deviceWatcher)
                {
                    //this.NotifyUser($"{KnownDevices.Count} devices found. Enumeration completed.",
                    //  NotifyType.StatusMessage);
                }
            });
        }

        private async void DeviceWatcher_Stopped(DeviceWatcher sender, object e)
        {

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {

                if (sender == deviceWatcher)
                {
                    this.NotifyUser($"No longer watching for devices.",
                            sender.Status == DeviceWatcherStatus.Aborted ? NotifyType.ErrorMessage : NotifyType.StatusMessage);
                }
            });
        }

        private void StartBleDeviceWatcher()
        {

            string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected", "System.Devices.Aep.Bluetooth.Le.IsConnectable" };

            string aqsAllBluetoothLEDevices = "(System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\")";

            deviceWatcher =
                    DeviceInformation.CreateWatcher(
                        aqsAllBluetoothLEDevices,
                        requestedProperties,
                        DeviceInformationKind.AssociationEndpoint);


            deviceWatcher.Added += DeviceWatcher_Added;
            deviceWatcher.Updated += DeviceWatcher_Updated;
            deviceWatcher.Removed += DeviceWatcher_Removed;
            deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
            deviceWatcher.Stopped += DeviceWatcher_Stopped;

            KnownDevices.Clear();
            deviceWatcher.Start();
        }

        private void StopBleDeviceWatcher()
        {
            if (deviceWatcher != null)
            {
                deviceWatcher.Stop();
                deviceWatcher = null;
            }
        }
        #endregion

        #region Main Window

        /// <summary>
        /// Called whenever the user changes selection in the scenarios list.  This method will navigate to the respective
        /// sample scenario page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        public class ScenarioBindingConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, string language)
            {
                Scenario s = value as Scenario;
                return (MainPage.Current.Scenarios.IndexOf(s) + 1) + ") " + s.Title;
            }

            public object ConvertBack(object value, Type targetType, object parameter, string language)
            {
                return true;
            }
        }

        public List<Scenario> Scenarios
        {
            get { return this.scenarios; }
        }

        /// <summary>
        /// Display a message to the user.
        /// This method may be called from any thread.
        /// </summary>
        /// <param name="strMessage"></param>
        /// <param name="type"></param>
        public void NotifyUser(string strMessage, NotifyType type)
        {
            if (Dispatcher.HasThreadAccess)
            {
                UpdateStatus(strMessage, type);
            }
            else
            {
                var task = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => UpdateStatus(strMessage, type));
            }
        }

        private void UpdateStatus(string strMessage, NotifyType type)
        {
            switch (type)
            {
                case NotifyType.StatusMessage:
                    StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Green);
                    break;
                case NotifyType.ErrorMessage:
                    StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Red);
                    break;
            }

            StatusBlock.Text = strMessage;
            StatusBorder.Visibility = (StatusBlock.Text != String.Empty) ? Visibility.Visible : Visibility.Collapsed;
            if (StatusBlock.Text != String.Empty)
            {
                StatusBorder.Visibility = Visibility.Visible;
                StatusPanel.Visibility = Visibility.Visible;
            }
            else
            {
                StatusBorder.Visibility = Visibility.Collapsed;
                StatusPanel.Visibility = Visibility.Collapsed;
            }
        }

        async void Footer_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri(((HyperlinkButton)sender).Tag.ToString()));
        }

        public enum NotifyType
        {
            StatusMessage,
            ErrorMessage
        }

        #endregion

        private async void Connect()
        {

            int i = 0;
            if (!await ClearBluetoothLEDeviceAsync())
            {
                this.NotifyUser("Error: Unable to reset state, try again.", NotifyType.ErrorMessage);
                return;
            }

            while (bluetoothLeDevice == null || bluetoothLeDevice != null)
            {

                try
                {
                    bluetoothLeDevice = await BluetoothLEDevice.FromIdAsync(this.SelectedBleDeviceId);

                    if (bluetoothLeDevice == null)
                    {
                        continue;
                    }
                }
                catch (Exception ex) when (ex.HResult == E_DEVICE_NOT_AVAILABLE)
                {
                    this.NotifyUser("Bluetooth radio is not on.", NotifyType.ErrorMessage);
                }

                if (bluetoothLeDevice != null)
                {
                    GattDeviceServicesResult resultx = await bluetoothLeDevice.GetGattServicesAsync(BluetoothCacheMode.Uncached);

                    if (resultx.Status == GattCommunicationStatus.Success)
                    {
                        var services = resultx.Services;

                        foreach (var service in services)
                        {
                            //System.Diagnostics.Debug.WriteLine("service: " + service.Uuid.ToString());
                            if (service.Uuid.ToString() == "0000fff0-0000-1000-8000-00805f9b34fb")
                            {
                                selectedService = service;
                            }
                        }

                        break;

                    }
                    else
                    {
                        this.NotifyUser($"Device unreachable, let's try it for {i} time, again...", NotifyType.ErrorMessage);
                    }
                    if (i < 3)
                    {
                        await Task.Delay(1000);
                        i++;
                    }
                    else
                    {
                        this.NotifyUser("Ok, I give up. Maybe next time...", NotifyType.ErrorMessage);
                        return;
                    }
                }
            }
            var attributeInfoDisp = selectedService;

            CharacteristicCollection.Clear();

            IReadOnlyList<GattCharacteristic> characteristics = null;
            try
            {
                var accessStatus = await attributeInfoDisp.RequestAccessAsync();
                if (accessStatus == DeviceAccessStatus.Allowed)
                {
                    var resulty = await attributeInfoDisp.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
                    if (resulty.Status == GattCommunicationStatus.Success)
                    {
                        characteristics = resulty.Characteristics;
                    }
                }
                else
                {
                    this.NotifyUser("Error accessing service.", NotifyType.ErrorMessage);

                    characteristics = new List<GattCharacteristic>();

                }
            }
            catch (Exception ex)
            {
                this.NotifyUser("Restricted service. Can't read characteristics: " + ex.Message,
                    NotifyType.ErrorMessage);
                characteristics = new List<GattCharacteristic>();
            }

            foreach (GattCharacteristic c in characteristics)
            {
                if (c.Uuid.ToString() == "0000fff1-0000-1000-8000-00805f9b34fb")
                {
                    selectedCharacteristic = c;
                }
                if (c.Uuid.ToString() == "0000fff2-0000-1000-8000-00805f9b34fb")
                {
                    writeCharacteristic = c;
                }
                //System.Diagnostics.Debug.WriteLine("characteristic: " + c.Uuid.ToString());
            }
            var result = await selectedCharacteristic.GetDescriptorsAsync(BluetoothCacheMode.Uncached);
            if (result.Status != GattCommunicationStatus.Success)
            {
                this.NotifyUser("Descriptor read failure: " + result.Status.ToString(), NotifyType.ErrorMessage);
            }

            presentationFormat = null;
            if (selectedCharacteristic.PresentationFormats.Count > 0)
            {

                if (selectedCharacteristic.PresentationFormats.Count.Equals(1))
                {
                    presentationFormat = selectedCharacteristic.PresentationFormats[0];
                    this.NotifyUser(presentationFormat.ToString(), NotifyType.StatusMessage);
                }

            }

            ValueChangedSubscribeToggle();
            AddValueChangedHandler();
            HoldButton.IsEnabled = true;
            TareButton.IsEnabled = true;
            UnitsButton.IsEnabled = true;
            loaded = 1;
            return;
        }

        #region Buttons

        private async void Save(object sender, RoutedEventArgs e)
        {

            FileSavePicker savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("Plain Text", new List<string>() { ".txt" });
            savePicker.SuggestedFileName = "New Document";
            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                CachedFileManager.DeferUpdates(file);
                await FileIO.WriteTextAsync(file, Text.Text);       
             }
        }


        void Clear(object sender, RoutedEventArgs e)
        {
            this.Text.Text = "Mesure time\t\t\tMesured value\r\n--------------\t\t\t----------------\r\n";
            cnt = 1;
        }

        void AddMesure(object sender, RoutedEventArgs e)
        {
            string val = Text.Text + "\r\n" + cnt.ToString() + ".\t\t\t\t" + CharacteristicLatestValue.Text;
            this.Text.Text = val;
            cnt++;
        }

        async void Hold(object sender, RoutedEventArgs e)
        {

            if (!subscribedForNotifications)
            {
                AddValueChangedHandler();
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () => CharacteristicLatestValue.Foreground = new SolidColorBrush(Windows.UI.Colors.White));
            } else { 
                RemoveValueChangedHandler();
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () => CharacteristicLatestValue.Foreground = new SolidColorBrush(Windows.UI.Colors.Yellow));
            }   

        }

        void Reset(object sender, RoutedEventArgs e)
        {
            if (loaded == 0)
            {
                StopBleDeviceWatcher();
                StartBleDeviceWatcher();
                Connect();
            }
            else
            {
                HoldButton.IsEnabled = false;
                TareButton.IsEnabled = false;
                UnitsButton.IsEnabled = false;
                RemoveValueChangedHandler();
                InitializeComponent();
                Connect();
            }
        }

        void Tare(object sender, RoutedEventArgs e)
        {
            byte[] tare = new byte[] { 0xFA,0x03};

            var writer = new DataWriter();

            writer.WriteBytes(tare);
            this.NotifyUser("Scale tared", NotifyType.StatusMessage);
            try
            {
                var writeSuccessful = WriteBufferToSelectedCharacteristicAsync(writer.DetachBuffer());
            }
            catch (Exception ex)
            {
                this.NotifyUser(ex.Message, NotifyType.ErrorMessage);
            }
        }

        void Units(object sender, RoutedEventArgs e)
        {

            byte[] ml = new byte[] { 0xFA, 0x02, 0x01, 0x04 };
            byte[] oz = new byte[] { 0xFA, 0x02, 0x01, 0x06 };
            byte[] g = new byte[] { 0xFA, 0x02, 0x01, 0x03 };

            var writer = new DataWriter();


            switch (caseSwitch)
            {
                case 1:
                    writer.WriteBytes(ml);
                    caseSwitch++;
                    break;
                case 2:
                    writer.WriteBytes(oz);
                    caseSwitch++;
                    break;
                case 3:
                    writer.WriteBytes(g);
                    caseSwitch = 1;
                    break;
            }
            this.NotifyUser("Units changed", NotifyType.StatusMessage);
            try
            {
                var writeSuccessful = WriteBufferToSelectedCharacteristicAsync(writer.DetachBuffer());
            }
            catch (Exception ex)
            {
                this.NotifyUser(ex.Message, NotifyType.ErrorMessage);
            }
        }
        #endregion

        #region Provisioning
        private async Task<bool> WriteBufferToSelectedCharacteristicAsync(IBuffer buffer)
        {
            try
            {
                var result = await writeCharacteristic.WriteValueWithResultAsync(buffer);

                if (result.Status == GattCommunicationStatus.Success)
                {
                    return true;
                }
                else
                {
                    this.NotifyUser($"Write failed: {result.Status}", NotifyType.ErrorMessage);
                    return false;
                }
            }
            catch (Exception ex) when (ex.HResult == E_BLUETOOTH_ATT_INVALID_PDU)
            {
                this.NotifyUser(ex.Message, NotifyType.ErrorMessage);
                return false;
            }
            catch (Exception ex) when (ex.HResult == E_BLUETOOTH_ATT_WRITE_NOT_PERMITTED || ex.HResult == E_ACCESSDENIED)
            {
                this.NotifyUser(ex.Message, NotifyType.ErrorMessage);
                return false;
            }
        }
        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {
            var success = await ClearBluetoothLEDeviceAsync();
            if (!success)
            {
                this.NotifyUser("Error: Unable to reset app state", NotifyType.ErrorMessage);
            }
        }

        private void AddValueChangedHandler()
        {
            if (!subscribedForNotifications)
            {
                registeredCharacteristic = selectedCharacteristic;
                registeredCharacteristic.ValueChanged += Characteristic_ValueChanged;
                subscribedForNotifications = true;
            }
        }

        private void RemoveValueChangedHandler()
        {
            if (subscribedForNotifications)
            {
                registeredCharacteristic.ValueChanged -= Characteristic_ValueChanged;
                registeredCharacteristic = null;
                subscribedForNotifications = false;
            }
        }

        private bool subscribedForNotifications = false;
        private async void ValueChangedSubscribeToggle()
        {
            if (!subscribedForNotifications)
            {
                GattCommunicationStatus status = GattCommunicationStatus.Unreachable;
                var cccdValue = GattClientCharacteristicConfigurationDescriptorValue.None;
                try
                {
                    if (selectedCharacteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Indicate))
                    {
                        cccdValue = GattClientCharacteristicConfigurationDescriptorValue.Indicate;
                    }

                    else if (selectedCharacteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
                    {
                        cccdValue = GattClientCharacteristicConfigurationDescriptorValue.Notify;
                    }
                    try
                    {
                        status = await selectedCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(cccdValue);

                        if (status == GattCommunicationStatus.Success)
                        {
                            this.NotifyUser("Successfully subscribed for value changes", NotifyType.StatusMessage);
                        }
                        else
                        {
                            this.NotifyUser($"Error registering for value changes: {status}", NotifyType.ErrorMessage);
                        }
                    }
                    catch
                    {
                        this.NotifyUser($"Error registering for value changes: {status}", NotifyType.ErrorMessage);
                    }
                    
                }
                catch (UnauthorizedAccessException ex)
                {
                    this.NotifyUser(ex.Message, NotifyType.ErrorMessage);
                }
            }
            else
            {
                try
                {
                    var result = await selectedCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                                GattClientCharacteristicConfigurationDescriptorValue.None);
                
                    if (result == GattCommunicationStatus.Success)
                    {
                        subscribedForNotifications = false;
                        this.NotifyUser("Successfully un-registered for notifications", NotifyType.StatusMessage);
                    }
                    else
                    {
                        this.NotifyUser($"Error un-registering for notifications: {result}", NotifyType.ErrorMessage);
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    this.NotifyUser(ex.Message, NotifyType.ErrorMessage);
                }
            }
        }

        #endregion

        private async void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            byte[] data;
            CryptographicBuffer.CopyToByteArray(args.CharacteristicValue, out data);

            string val = "";
            string units = "";

            var k = Convert.ToString(data[5]);
            var g = Convert.ToString(data[6]);

            if (Convert.ToInt32(data[1]) == 4)
            {
              await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
              () => CharacteristicLatestValue.Foreground = new SolidColorBrush(Windows.UI.Colors.Red));
              
            } else {
                if (Convert.ToInt32(data[2]) == 2){
                    val = "-" + val;
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    () => CharacteristicLatestValue.Foreground = new SolidColorBrush(Windows.UI.Colors.Aquamarine));
                } else {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    () => CharacteristicLatestValue.Foreground = new SolidColorBrush(Windows.UI.Colors.White));
                }
            }

            if (Convert.ToInt32(data[3]) == 3)
            {
                if (Convert.ToInt32(data[5]) > 0)
                {
                    units = " Kg";
                    val = val + k + "," + g;
                }
                else
                {
                    units = " g";
                    val = val + g;
                }
            }
            if (Convert.ToInt32(data[3]) == 4)
            {
                units = " ml";
                val = val + Convert.ToString(data[6]);
            }

            if (Convert.ToInt32(data[3]) == 6)
            {
                units = " oz/lb";
                val = val + Convert.ToString(data[6]);
            }

            val = val + units;

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () => CharacteristicLatestValue.Text = Convert.ToString(val));

        }

        private string FormatValueByPresentation(IBuffer buffer, GattPresentationFormat format)
        {
            byte[] data;
            CryptographicBuffer.CopyToByteArray(buffer, out data);
            if (format != null)
            {
                if (format.FormatType == GattPresentationFormatTypes.UInt32 && data.Length >= 4)
                {
                    return BitConverter.ToInt32(data, 0).ToString();
                }
                else if (format.FormatType == GattPresentationFormatTypes.Utf8)
                {
                    try
                    {
                        return Encoding.UTF8.GetString(data);
                    }
                    catch (ArgumentException)
                    {
                        return "(error: Invalid UTF-8 string)";
                    }
                }
                else
                {
                    return "Unsupported format: " + CryptographicBuffer.EncodeToHexString(buffer);
                }
            }
            else if (data != null)
            {
                if (selectedCharacteristic.Uuid.Equals(GattCharacteristicUuids.BatteryLevel))
                {
                    try
                    {
                        return "Battery Level: " + data[0].ToString() + "%";
                    }
                    catch (ArgumentException)
                    {
                        return "Battery Level: (unable to parse)";
                    }
                }
                else if (selectedCharacteristic.Uuid.Equals(Constants.ResultCharacteristicUuid))
                {
                    return BitConverter.ToInt32(data, 0).ToString();
                }
                else if (registeredCharacteristic != null)
                {
                    if (registeredCharacteristic.Uuid.Equals(Constants.ResultCharacteristicUuid))
                    {
                        return BitConverter.ToInt32(data, 0).ToString();
                    }
                }
                else
                {
                    try
                    {
                        return "Unknown format: " + Encoding.UTF8.GetString(data);
                    }
                    catch (ArgumentException)
                    {
                        return Encoding.UTF8.GetString(data);
                    }
                }
            }
            else
            {
                return "Empty data received";
            }
            return Encoding.UTF8.GetString(data);
        }


        private void Main_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }
    }
}


