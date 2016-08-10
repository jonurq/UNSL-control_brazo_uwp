using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Networking.Sockets;
using System.Collections.ObjectModel;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using System.Threading.Tasks;
using System.Threading;
using System.Text;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ControlBrazoBluetooth
{
    /// <summary>
    /// The Main Page for the app
    /// </summary>
    public sealed partial class MainPage : Page
    {
        string Title = "Control Brazo Robótico Bluetooth";
        private Windows.Devices.Bluetooth.Rfcomm.RfcommDeviceService _service;
        private StreamSocket _socket;
        private DataWriter dataWriterObject;
        private DataReader dataReaderObject;
        ObservableCollection<PairedDeviceInfo> _pairedDevices;
        private CancellationTokenSource ReadCancellationTokenSource;


        public MainPage()
        {
            this.InitializeComponent();
            MyTitle.Text = Title;
            InitializeRfcommDeviceService();
        }


        async void InitializeRfcommDeviceService()
        {
            try
            {
                DeviceInformationCollection DeviceInfoCollection = await DeviceInformation.FindAllAsync(RfcommDeviceService.GetDeviceSelector(RfcommServiceId.SerialPort));


                var numDevices = DeviceInfoCollection.Count();

                // By clearing the backing data, we are effectively clearing the ListBox
                _pairedDevices = new ObservableCollection<PairedDeviceInfo>();
                _pairedDevices.Clear();

                if (numDevices == 0)
                {
                    MessageDialog md = new MessageDialog("No hay dispositivos pareados", Title);
                    await md.ShowAsync();
                    System.Diagnostics.Debug.WriteLine("InitializeRfcommDeviceService: No se encontraron dispositivos pareados.");
                }
                else
                {
                    // Found paired devices.
                    foreach (var deviceInfo in DeviceInfoCollection)
                    {
                        _pairedDevices.Add(new PairedDeviceInfo(deviceInfo));
                    }
                }
                PairedDevices.Source = _pairedDevices;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("InitializeRfcommDeviceService: " + ex.Message);
            }
        }

        async private void ConnectDevice_Click(object sender, RoutedEventArgs e)
        {
            
            DeviceInformation DeviceInfo; 
            PairedDeviceInfo pairedDevice = (PairedDeviceInfo)ConnectDevices.SelectedItem;
            DeviceInfo = pairedDevice.DeviceInfo;

            bool success = true;
            try
            {
                _service = await RfcommDeviceService.FromIdAsync(DeviceInfo.Id);

                if (_socket != null)
                {
                    // Desligo el socket
                    _socket.Dispose();
                }

                _socket = new StreamSocket();
                try { 
                    
                    await _socket.ConnectAsync(_service.ConnectionHostName, _service.ConnectionServiceName);
                }
                catch (Exception ex)
                {
                        success = false;
                        System.Diagnostics.Debug.WriteLine("Connect: " + ex.Message);
                        MessageDialog md = new MessageDialog("No se pudo establecer la conexión", Title);
                        await md.ShowAsync();
                }
                // Si la conexion fue exitosa
                if (success)
                {
                    //Habilito los botones del joystick
                    this.buttonDisconnect.IsEnabled = true;
                    this.LeftDownButton.IsEnabled = true;
                    this.LeftUpButton.IsEnabled = true;
                    this.LeftRightButton.IsEnabled = true;
                    this.LeftLeftButton.IsEnabled = true;
                    this.RightDownButton.IsEnabled = true;
                    this.RightUpButton.IsEnabled = true;
                    this.RightRightButton.IsEnabled = true;
                    this.RightLeftButton.IsEnabled = true;
                    this.CenterLeftButton.IsEnabled = true;
                    this.CenterRightButton.IsEnabled = true;
                    this.Slider.IsEnabled = true;
                    string msg = String.Format("Connectado a {0}!", _socket.Information.RemoteAddress.DisplayName);
                    System.Diagnostics.Debug.WriteLine(msg);
                    //Se inicia el timer para interrumpir cada 15ms
                    Timer1=new DispatcherTimer();
                    Timer1.Tick += dispatcherTimer_Tick;
                    Timer1.Interval= TimeSpan.FromMilliseconds(15);
                    Timer1.Start();
                
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Overall Connect: " +ex.Message);
                _socket.Dispose();
                _socket = null;
            }
        }

        


        private void ConnectDevices_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            PairedDeviceInfo pairedDevice = (PairedDeviceInfo)ConnectDevices.SelectedItem;
            this.TxtBlock_SelectedID.Text = pairedDevice.ID;
            this.textBlockBTName.Text = pairedDevice.Name;
            ConnectDevice_Click(sender, e);
        }
        
        private async void button_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            if (button != null)
            {
                switch ((string)button.Content)
                {
                    case "Desconectar":
                        this.textBlockBTName.Text = "";
                        this.TxtBlock_SelectedID.Text = "";
                        this.buttonDisconnect.IsEnabled = false;
                        this.LeftDownButton.IsEnabled = false;
                        this.LeftUpButton.IsEnabled = false;
                        this.LeftRightButton.IsEnabled = false;
                        this.LeftLeftButton.IsEnabled = false;
                        this.RightDownButton.IsEnabled = false;
                        this.RightUpButton.IsEnabled = false;
                        this.RightRightButton.IsEnabled = false;
                        this.RightLeftButton.IsEnabled = false;
                        this.CenterLeftButton.IsEnabled = false;
                        this.CenterRightButton.IsEnabled = false;
                        this.Slider.IsEnabled = false;
                        Timer1.Stop();
                        Timer2 = new DispatcherTimer();
                        Timer2.Tick += dispatcherTimer2_Tick;
                        Timer2.Interval = TimeSpan.FromMilliseconds(15);
                        Timer2.Start();
                        break;
                    case "Actualizar":
                        InitializeRfcommDeviceService();
                        break;
                }
            }
        }

        


        public async void Send(byte[] msg)
        {
            try
            {
                if (_socket.OutputStream != null)
                {
                    // Crea el objeto DataWriter y lo adjunta a OutputStream
                    dataWriterObject = new DataWriter(_socket.OutputStream);

                    //Lanza la tarea WriteAsync
                    await WriteAsync(msg);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Send(): " + ex.Message);
            }
            finally
            {
                // Limpia al terminar
                if (dataWriterObject != null)
                {
                    dataWriterObject.DetachStream();
                    dataWriterObject = null;
                }
            }
        }

        
        private async Task WriteAsync(byte[] msg)
        {
            Task<UInt32> storeAsyncTask;

            
            if (msg.Length != 0)
            {
                // Escribe los Bytes
                dataWriterObject.WriteBytes(msg);

                
                storeAsyncTask = dataWriterObject.StoreAsync().AsTask();

                UInt32 bytesWritten = await storeAsyncTask;
                if (bytesWritten > 0)
                {
                    string status_Text = msg[0].ToString() + ", ";
                    status_Text += bytesWritten.ToString();
                    status_Text += " bytes escritos correctamente!";
                    System.Diagnostics.Debug.WriteLine(status_Text);
                }
            }
            
        }


        
        public class PairedDeviceInfo
        {
            internal PairedDeviceInfo(DeviceInformation deviceInfo)
            {
                this.DeviceInfo = deviceInfo;
                this.ID = this.DeviceInfo.Id;
                this.Name = this.DeviceInfo.Name;
            }

            public string Name { get; private set; }
            public string ID { get; private set; }
            public DeviceInformation DeviceInfo { get; private set; }
        }

        //Posiciones iniciales
        private const byte init_posA = 90;
        private const byte init_posB = 135;
        private const byte init_posD = 80;
        private const byte init_posE = 75;
        private const byte init_posF = 85;
        private const double init_posG = 70;

        //Variables para almacenar las posiciones a enviar mediante bluetooth
        //Posiciones iniciales que coinciden con las posiciones seteadas por defecto en arduino
        private byte posA = init_posA; //Servo A1 y A2
        private byte posB = init_posB; //Servo B y C
        private byte posD = init_posD;
        private byte posE = init_posE;
        private byte posF = init_posF;
        private double posG = init_posG;

        private DispatcherTimer Timer1; //Timer para interrumpir cada 15ms
        private DispatcherTimer Timer2; //Timer para interrumpir cada 15ms para volver a posicion por defecto

        //Funcion que se ejecuta cada 15ms  
        private void dispatcherTimer_Tick(object sender, object e)
        {
            // Verifica que botones estan presionados y aumenta o disminuye 
            //de acuerdo a los botones presionados y a los limites
            if (LeftUpButton.IsPressed)
            {
                if (posB + 2 <= 135)
                    posB += 2;
            }
            if (LeftLeftButton.IsPressed)
            {
                if (posA - 1 >= 0)
                    posA -= 1;
            }
            if (LeftRightButton.IsPressed)
            {
                if (posA + 1 <= 180)
                    posA += 1;
            }
            if (LeftDownButton.IsPressed)
            {
                if (posB - 2 >= 0)
                    posB -= 2;
            }
            if (RightUpButton.IsPressed)
            {
                if (posE + 2 <= 145)
                    posE += 2;
            }
            if (RightLeftButton.IsPressed)
            {
                if (posD - 2 >= 20)
                    posD -= 2;
            }
            if (RightRightButton.IsPressed)
            {
                if (posD + 2 <= 145)
                    posD += 2;
            }
            if (RightDownButton.IsPressed)
            {
                if (posE - 2 >= 5)
                    posE -= 2;
            }
            if(CenterLeftButton.IsPressed)
            {
                if (posF - 2 >= 5)
                    posF -= 2;
            }
            if (CenterRightButton.IsPressed)
            {
                if (posF + 2 <= 165)
                    posF += 2;
            }

            //Lleno el stream de bytes a enviar
            byte[] str = new byte[6];
            str[0] = posA;
            str[1] = posB;
            str[2] = posD;
            str[3] = posE;
            str[4] = posF;
            str[5] = (byte)posG;
            Send(str);
        }
        private async void dispatcherTimer2_Tick(object sender, object e)
        {
            if (posA < init_posA)
            {
                posA++;
            }
            else if (posA > init_posA)
            {
                posA--;
            }
            if (posB < init_posB)
            {
                posB++;
            }
            else if (posB > init_posB)
            {
                posB--;
            }
            if (posD < init_posD)
            {
                posD++;
            }
            else if (posD > init_posD)
            {
                posD--;
            }
            if (posE < init_posE)
            {
                posE++;
            }
            else if (posE > init_posE)
            {
                posE--;
            }
            if (posF < init_posF)
            {
                posF++;
            }
            else if (posF > init_posF)
            {
                posF--;
            }
            if (posG < init_posG)
            {
                posG++;
            }
            else if (posG > init_posG)
            {
                posG--;
            }
            

            //Lleno el stream de bytes a enviar
            byte[] str = new byte[6];
            str[0] = posA;
            str[1] = posB;
            str[2] = posD;
            str[3] = posE;
            str[4] = posF;
            str[5] = (byte)posG;
            Send(str);

            //
            if (posA == init_posA && posB == init_posB && posD == init_posD && posE == init_posE && posF == init_posF && posG == init_posG)
            {
                await this._socket.CancelIOAsync();
                _socket.Dispose();
                _socket = null;
                Timer2.Stop();
            }
        }


    }
}
