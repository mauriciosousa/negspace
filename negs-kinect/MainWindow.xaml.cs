
namespace Microsoft.Samples.Kinect.ColorBasics
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;
    using System.Windows.Shapes;
    using System.Collections.Generic;
    using System.Collections;
    using System.Linq;
    using System.Threading;

    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        private readonly int bytesPerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;

        private KinectSensor kinectSensor = null;

        private CoordinateMapper coordinateMapper = null;

        private MultiSourceFrameReader multiFrameSourceReader = null;

        private WriteableBitmap colorBitmap = null;

        private ushort[] depthFrameData = null;

        private byte[] colorFrameData = null;

        private byte[] bodyIndexFrameData = null;

        private ColorSpacePoint[] colorPoints = null;

        private CameraSpacePoint[] cameraPoints = null;

        private Body[] bodies = null;

        private byte[] displayFrame = null;

        private string statusText = null;

        private ConfigFile _configFile;
        private List<Vector3> pointsToDepth;
        private List<CameraSpacePoint> surfacePoints;

        private FrameCounter _frameCounter;

        private TcpSender _tcp;

        private string configFilename = "../../../config.txt";

        private Line _connectionLine;

        private string MachineName;

        private List<byte> __AvatarData__;
        private byte __index0__;
        private byte __index1__;
        private byte __ZDepth0__;
        private byte __ZDepth1__;
        private byte __Blue__;
        private byte __Green__;
        private byte __Red__;

        public MainWindow()
        {
            MachineName = Environment.MachineName;

            this.kinectSensor = KinectSensor.GetDefault();

            this.multiFrameSourceReader = this.kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Depth | FrameSourceTypes.Color | FrameSourceTypes.Body | FrameSourceTypes.BodyIndex);

            this.multiFrameSourceReader.MultiSourceFrameArrived += MultiFrameSourceReader_MultiSourceFrameArrived;

            this.coordinateMapper = this.kinectSensor.CoordinateMapper;

            FrameDescription depthFrameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            int depthWidth = depthFrameDescription.Width;
            int depthHeight = depthFrameDescription.Height;

            this.depthFrameData = new ushort[depthWidth * depthHeight];
            this.bodyIndexFrameData = new byte[depthWidth * depthHeight];
            this.colorPoints = new ColorSpacePoint[depthWidth * depthHeight];
            this.cameraPoints = new CameraSpacePoint[depthWidth * depthHeight];

            FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.FrameDescription;

            int colorWidth = colorFrameDescription.Width;
            int colorHeight = colorFrameDescription.Height;

            this.colorFrameData = new byte[colorWidth * colorHeight * this.bytesPerPixel];
            this.displayFrame = new byte[depthWidth * depthHeight * this.bytesPerPixel];

            this.colorBitmap = new WriteableBitmap(depthWidth, depthHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

            pointsToDepth = new List<Vector3>();
            surfacePoints = new List<CameraSpacePoint>();


            this.InitializeComponent();

            _configFile = new ConfigFile();
            _loadConfig(_configFile);

            _tcp = new TcpSender();

            _frameCounter = new FrameCounter();
            _frameCounter.PropertyChanged += (o, e) => this.StatusText = String.Format("FPS = {0:N1} / CPU = {1:N6}; Streaming = {2}", _frameCounter.FramesPerSecond, _frameCounter.CpuTimePerFrame, _tcp.Connected ? "Connected" : "Not Connected");
            

            this.kinectSensor.Open();
            this.DataContext = this;

            _connectionLine = null;
            _drawConnectionLine();

        }

        private void MultiFrameSourceReader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs ex)
        {
            __AvatarData__ = new List<byte>();

            int depthWidth = 0;
            int depthHeight = 0;

            int colorWidth = 0;
            int colorHeight = 0;

            int bodyIndexWidth = 0;
            int bodyIndexHeight = 0;

            bool multiSourceFrameProcessed = false;
            bool colorFrameProcessed = false;
            bool depthFrameProcessed = false;
            bool bodyIndexFrameProcessed = false;
            bool bodyFrameProcessed = false;

            MultiSourceFrame multiSourceFrame = ex.FrameReference.AcquireFrame();

            if (multiSourceFrame != null)
            {
                using (_frameCounter.Increment())
                {
                    using (DepthFrame depthFrame = multiSourceFrame.DepthFrameReference.AcquireFrame())
                    {
                        using (ColorFrame colorFrame = multiSourceFrame.ColorFrameReference.AcquireFrame())
                        {
                            using (BodyIndexFrame bodyIndexFrame = multiSourceFrame.BodyIndexFrameReference.AcquireFrame())
                            {
                                if (depthFrame != null)
                                {
                                    FrameDescription depthFrameDescription = depthFrame.FrameDescription;
                                    depthWidth = depthFrameDescription.Width;
                                    depthHeight = depthFrameDescription.Height;

                                    if ((depthWidth * depthHeight) == this.depthFrameData.Length)
                                    {
                                        depthFrame.CopyFrameDataToArray(this.depthFrameData);

                                        depthFrameProcessed = true;
                                    }

                                    if (colorFrame != null)
                                    {
                                        FrameDescription colorFrameDescription = colorFrame.FrameDescription;
                                        colorWidth = colorFrameDescription.Width;
                                        colorHeight = colorFrameDescription.Height;

                                        if ((colorWidth * colorHeight * this.bytesPerPixel) == this.colorFrameData.Length)
                                        {
                                            if (colorFrame.RawColorImageFormat == ColorImageFormat.Bgra)
                                            {
                                                colorFrame.CopyRawFrameDataToArray(this.colorFrameData);
                                            }
                                            else
                                            {
                                                colorFrame.CopyConvertedFrameDataToArray(this.colorFrameData, ColorImageFormat.Bgra);
                                            }

                                            colorFrameProcessed = true;
                                        }
                                    }

                                    if (bodyIndexFrame != null)
                                    {
                                        FrameDescription bodyIndexFrameDescription = bodyIndexFrame.FrameDescription;
                                        bodyIndexWidth = bodyIndexFrameDescription.Width;
                                        bodyIndexHeight = bodyIndexFrameDescription.Height;

                                        if ((bodyIndexWidth * bodyIndexHeight) == this.bodyIndexFrameData.Length)
                                        {
                                            bodyIndexFrame.CopyFrameDataToArray(this.bodyIndexFrameData);

                                            bodyIndexFrameProcessed = true;
                                        }
                                    }
                                    multiSourceFrameProcessed = true;
                                }
                            }

                            using (BodyFrame bodyFrame = multiSourceFrame.BodyFrameReference.AcquireFrame())
                            {
                                if (bodyFrame != null)
                                {
                                    if (this.bodies == null)
                                    {
                                        this.bodies = new Body[bodyFrame.BodyCount];
                                    }
                                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                                    bodyFrameProcessed = true;
                                }
                            }
                        }
                    }
                }

                if (multiSourceFrameProcessed && depthFrameProcessed && colorFrameProcessed && bodyIndexFrameProcessed && bodyFrameProcessed)
                {
                    _connectionLine.Stroke = _tcp.Connected ? Brushes.Green : Brushes.Red;

                    this.displayFrame = new byte[depthWidth * depthHeight * this.bytesPerPixel];

                    this.coordinateMapper.MapDepthFrameToColorSpace(this.depthFrameData, this.colorPoints);
                    this.coordinateMapper.MapDepthFrameToCameraSpace(this.depthFrameData, this.cameraPoints);

                    Array.Clear(displayFrame, 0, displayFrame.Length);

                    int step = 1;

                    int depthWidthIndex = 0;
                    int depthHeightIndex = 0;
                    for (int depthIndex = 0; depthIndex < depthFrameData.Length; depthIndex+=step)
                    {
                        byte player = this.bodyIndexFrameData[depthIndex];

                        bool? c = OnlyPlayersMenuItem.IsChecked;
                        bool val = c != null ? (bool)c : false;
                        if (!val || player != 0xff)
                        //if (!val || player == 0)
                        {
                            CameraSpacePoint p = this.cameraPoints[depthIndex];
                            ColorSpacePoint colorPoint = this.colorPoints[depthIndex];

                            // make sure the depth pixel maps to a valid point in color space
                            int colorX = (int)Math.Floor(colorPoint.X + 0.5);
                            int colorY = (int)Math.Floor(colorPoint.Y + 0.5);

                            // set source for copy to the color pixel
                            int displayIndex = depthIndex * this.bytesPerPixel;

                            if ((colorX >= 0) && (colorX < colorWidth) && (colorY >= 0) && (colorY < colorHeight) && p.Z > 0)
                            {
                                // calculate index into color array
                                int colorIndex = ((colorY * colorWidth) + colorX) * this.bytesPerPixel;

                                this.displayFrame[displayIndex] = this.colorFrameData[colorIndex]; // B
                                this.displayFrame[displayIndex + 1] = this.colorFrameData[colorIndex + 1]; // G
                                this.displayFrame[displayIndex + 2] = this.colorFrameData[colorIndex + 2]; // R
                                this.displayFrame[displayIndex + 3] = this.colorFrameData[colorIndex + 3]; // A


                                if (player != 0xff && (!(Double.IsInfinity(p.X)) && !(Double.IsInfinity(p.Y)) && !(Double.IsInfinity(p.Z))))
                                {
                                    #region Void Compression Algorithm

                                    //Int16 depth = Convert.ToInt16(Math.Round((Decimal)(p.Z * 1000.0f), 0));
                                    ushort depth = depthFrameData[depthIndex];
                                    __ZDepth0__ = (byte)(depth % 256);
                                    __ZDepth1__ = (byte)(depth / 256);
                                    __Blue__ = this.colorFrameData[colorIndex];
                                    __Green__ = this.colorFrameData[colorIndex + 1];
                                    __Red__ = this.colorFrameData[colorIndex + 2];

                                    //Int16 d1 = (Int16)__ZDepth0__;
                                    //Int16 d2 = (Int16)(__ZDepth1__ * 256);
                                    //int d3 = d1 + d2;

                                    //if (shouldwrite)
                                    {
                                        __index0__ = (byte)(depthWidthIndex % 256);
                                        __index1__ = (byte)(depthWidthIndex / 256);
                                        __AvatarData__.Add(__index0__);
                                        __AvatarData__.Add(__index1__);
                                    }
                                    __AvatarData__.Add(__Blue__); 
                                    __AvatarData__.Add(__Green__);
                                    __AvatarData__.Add(__Red__);
                                    __AvatarData__.Add(__ZDepth0__);
                                    __AvatarData__.Add(__ZDepth1__);
                                    #endregion

                                    //shouldwrite = false;
                                }
                                else
                                {
                                    //shouldwrite = true;
                                }
                            }
                            else
                            {
                                this.displayFrame[displayIndex] = 0;
                                this.displayFrame[displayIndex + 1] = 0;
                                this.displayFrame[displayIndex + 2] = 0;
                                this.displayFrame[displayIndex + 3] = 100;
                            }
                        }


                        //
                        if (++depthWidthIndex == depthWidth)
                        {
                            depthWidthIndex = 0;
                            ++depthHeightIndex;

                            int newline = 5000;
                            byte nl0 = (byte)(newline % 256);
                            byte nl1 = (byte)(newline / 256);
                            __AvatarData__.Add(nl0);
                            __AvatarData__.Add(nl1);
                        }

                    }//


                    int numOfElements = __AvatarData__.Count;
                    if (numOfElements > 0)
                    {
                        if (_tcp.Connected)
                        {
                            new Thread(() =>
                            {
                                Thread.CurrentThread.IsBackground = true;

                                try
                                {

                                    byte[] s = BitConverter.GetBytes(numOfElements); // [4]
                                    for (int i = s.Length - 1; i >= 0; i--)
                                    {
                                        __AvatarData__.Insert(0, s[i]);
                                    }
                                    _tcp.write(__AvatarData__.ToArray());
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e.Message);
                                    Console.WriteLine(e.StackTrace);
                                }
                            }).Start();
                        }
                    }

                    colorBitmap.WritePixels(
                    new Int32Rect(0, 0, depthWidth, depthHeight),
                    this.displayFrame,
                    depthWidth * bytesPerPixel,
                    0);
                }
            }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;

        public ImageSource ImageSource
        {
            get
            {
                return this.colorBitmap;
            }
        }

        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            this.multiFrameSourceReader.Dispose();
            this.multiFrameSourceReader = null;

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }

        private void _drawConnectionLine()
        {
            if (_connectionLine == null)
            {
                _connectionLine = new Line();
                _connectionLine.Stroke = Brushes.Red;
                _connectionLine.X1 = 0;
                _connectionLine.X2 = 512;
                _connectionLine.Y1 = 2;
                _connectionLine.Y2 = 2;
                _connectionLine.HorizontalAlignment = HorizontalAlignment.Left;
                _connectionLine.VerticalAlignment = VerticalAlignment.Center;
                _connectionLine.StrokeThickness = 4;
                canvas.Children.Add(_connectionLine);
            }
        }

        private void _drawLine(int X1, int Y1, int X2, int Y2)
            
        {
            Line myLine = new Line();
            myLine.Stroke = Brushes.Violet;
            myLine.X1 = X1;
            myLine.X2 = X2;
            myLine.Y1 = Y1;
            myLine.Y2 = Y2;
            myLine.HorizontalAlignment = HorizontalAlignment.Left;
            myLine.VerticalAlignment = VerticalAlignment.Center;
            myLine.StrokeThickness = 1;
            canvas.Children.Add(myLine);
        }

        private void _drawLine(DepthSpacePoint a, DepthSpacePoint b)
        {
            _drawLine((int)a.X, (int)a.Y, (int)b.X, (int)b.Y);
        }

        private void drawEllipse(double x, double y)
        {
            Ellipse ellipse = new Ellipse
            {
                Fill = Brushes.LimeGreen,
                Width = 4,
                Height = 4
            };

            Canvas.SetLeft(ellipse, x - ellipse.Width / 2);
            Canvas.SetTop(ellipse, y - ellipse.Height / 2);
            canvas.Children.Add(ellipse);
        }

        private void _loadConfig(ConfigFile _configFile)
        {
            if (!_configFile.Load(configFilename))
            {
                Console.WriteLine("no such config file");
            }
            else
            {
                Console.WriteLine("Config File:");
                Console.WriteLine("tcp.port: " + _configFile.TcpPort);
                Console.WriteLine("tcp.address: " + _configFile.TcpAddress);

            }
        }

        private void ReloadConfigFile_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            _loadConfig(_configFile);
        }

        private void Image_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // CLICK

            if (_tcp.Connected)
            {
                _tcp.close();
                this.Title = "Not Connected";
            }
            else
            {
                _loadConfig(_configFile);
                _tcp.connect(_configFile.TcpAddress, _configFile.TcpPort);
                if (_tcp.Connected)
                {
                    this.Title = "Connected to " + _configFile.TcpAddress + ":" + _configFile.TcpPort;
                }
                else
                    this.Title = "Cannot reach " + _configFile.TcpAddress + ":" + _configFile.TcpPort;
            }
        }

        private void _drawSurface(DepthSpacePoint a, DepthSpacePoint b, DepthSpacePoint c, DepthSpacePoint d)
        {
            _drawLine(a, b);
            _drawLine(b, c);
            _drawLine(c, d);
            _drawLine(d, a);
        }

        private void SaveConfigFile(object sender, RoutedEventArgs e)
        {
            _configFile.Save(configFilename);
        }

        private void BroadcastSurfaceInfo_MenuItem_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
