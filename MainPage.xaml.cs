using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace RFIDReader
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        private SerialDevice serialPort;
        private DataReader dataReader;
        private ObservableCollection<DeviceInformation> deviceList;
        private CancellationTokenSource readCancellationTokenSource;

        public MainPage()
        {
            this.InitializeComponent();

            this.serialPort = null;
            this.dataReader = null;
            this.deviceList = new ObservableCollection<DeviceInformation>();
            this.readCancellationTokenSource = null;

            this.ListAvailablePort();
        }

        private async void ListAvailablePort()
        {
            try
            {
                string devSel = SerialDevice.GetDeviceSelector();
                var dis = await DeviceInformation.FindAllAsync(devSel);
                Debug.WriteLine(dis.Count);

                if (dis.Count > 0)
                {
                    DeviceInformation entry = (DeviceInformation)dis[0];
                    this.serialPort = await SerialDevice.FromIdAsync(entry.Id);
                    this.serialPort.ReadTimeout = TimeSpan.FromMilliseconds(1000);
                    this.serialPort.BaudRate = 9600;
                    this.serialPort.DataBits = 8;
                    this.serialPort.StopBits = SerialStopBitCount.One;
                    this.serialPort.Parity = SerialParity.None;
                    this.serialPort.Handshake = SerialHandshake.None;
                    this.readCancellationTokenSource = new CancellationTokenSource();

                    this.Listen();
                }
            } catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        private async void Listen()
        {
            Debug.WriteLine("Listen");
            Debug.WriteLine(this.serialPort);
            if (this.serialPort == null)
            {
                return;
            }

            try
            {
                dataReader = new DataReader(this.serialPort.InputStream);
                while (true)
                {
                    Debug.WriteLine("while true");
                    await this.ReadAsync(this.readCancellationTokenSource.Token);
                }
            } catch(Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            } finally
            {
                if (dataReader != null)
                {
                    dataReader.DetachStream();
                    dataReader = null;
                }
            }
        }

        private async Task ReadAsync(CancellationToken cancellationToken)
        {
            try
            {
                Task<UInt32> loadAsyncTask;
                uint ReadBufferLength = 1024;

                cancellationToken.ThrowIfCancellationRequested();

                dataReader.InputStreamOptions = InputStreamOptions.Partial;

                // create a task that wait for the data on the input stream
                loadAsyncTask = dataReader.LoadAsync(ReadBufferLength).AsTask(cancellationToken);

                // it actually waiting for the data to be emitted from the reader
                UInt32 bytesRead = await loadAsyncTask;
                Debug.WriteLine("ReadAsync");
                Debug.WriteLine(bytesRead);
                if (bytesRead > 0)
                {
                    Debug.WriteLine(dataReader.ReadString(bytesRead));
                }
            } catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
    }
}
