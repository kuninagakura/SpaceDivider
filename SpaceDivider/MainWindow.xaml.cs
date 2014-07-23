using System.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Media3D;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit;
using Noise_Removal;
using FlowMap;
using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Math;
using System.Drawing.Imaging;
using Utilities;
 
namespace Test_PCL
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        GeometryModel3D[] points = new GeometryModel3D[340 * 240];
        PerspectiveCamera cam = new PerspectiveCamera();
        private MapCopy hmap;
        DirectionalLight lighting = new DirectionalLight();
        private Queue<PointF> trails = new Queue<PointF>();
        GeometryModel3D firstpoint = new GeometryModel3D();
        Bitmap plan;
        List<PlaneModel> planes = new List<PlaneModel>();
        List<System.Windows.Media.Color> _colors = new List<System.Windows.Media.Color>();
        Model3DGroup modelGroup = new Model3DGroup();
        System.Windows.Media.Color planeColor = System.Windows.Media.Colors.Aqua;
        System.Windows.Point initP = new System.Windows.Point();
        private short[] pixelData;

        int lastSpace = 0;
        int startCount = 0;
        int planeCount = 0;
        double rotationspeed = 1f / 500f;
        
        int step = 2;
        KinectSensor sensor;
        int height = 240;
        int width = 340;
        public Noise_Removal.Remover nr;

        private SpaceModel backgroundModel;
        
        public MainWindow()
        {
            InitializeComponent();
            this.hmap = new MapCopy((int)mw.width, (int)mw.height, 30);
            this.plan = new Bitmap(this.width, this.height);

       }


        private BitmapSource detectPersonBlob(short[] depthArray, int dist, int width, int height)
        {
            BlobCounter bc = new BlobCounter();
            using (Bitmap arg = this.hmap.RenderPersonBlobMap(depthArray, dist, width, height))
            {
                BitmapData bmpd = arg.LockBits(new System.Drawing.Rectangle(0, 0, arg.Width, arg.Height), ImageLockMode.ReadWrite, arg.PixelFormat);
                bc.FilterBlobs = true;
                bc.MinHeight = 5;
                bc.MinWidth = 5;
                bc.ProcessImage(bmpd);
                arg.UnlockBits(bmpd);
            }
            Blob[] blobs = bc.GetObjectsInformation();
            foreach (Blob b in blobs)
            {
                if (b.Area > 4000)
                {
                    if (trails.Count < 20)
                    {
                        trails.Enqueue(new PointF(b.CenterOfGravity.X, b.CenterOfGravity.Y));
                    }
                    else
                    {
                        trails.Dequeue();
                        trails.Enqueue(new PointF(b.CenterOfGravity.X, b.CenterOfGravity.Y));
                    }
                }
            }
           
            using (Graphics g = Graphics.FromImage(this.plan))
            {
                foreach(PointF p in this.trails)
                    g.DrawEllipse(new System.Drawing.Pen(System.Drawing.Color.Blue), p.X, p.Y, 5, 5);
            }
            return this.nr.BitmapSourceFromBitmap(this.plan);
        }
        private GeometryModel3D getplane(Point3DCollection _points, System.Windows.Media.Color color)
        {
            GeometryModel3D model = new GeometryModel3D();
            MeshGeometry3D mesh = new MeshGeometry3D();
            mesh.Positions = _points;

            Int32Collection Tris = new Int32Collection();
            Tris.Add(0);
            Tris.Add(1);
            Tris.Add(2);
            Tris.Add(0);
            Tris.Add(2);
            Tris.Add(3);
            mesh.TriangleIndices = Tris;

            model.Geometry = mesh;
            model.Material = new DiffuseMaterial(new SolidColorBrush(color));
            return model;


        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.nr = new Remover();
            this.backgroundModel = new SpaceModel(this.width, this.height);

            colorPicker0.Items.Add("White");
            colorPicker0.Items.Add("Black");
            colorPicker0.Items.Add("Red");
            colorPicker0.Items.Add("Green");
            colorPicker0.Items.Add("Aqua");
            colorPicker0.Items.Add("Violet");

            this.lighting.Color = Colors.White;
            this.lighting.Direction = new Vector3D(1, 1, 1);

            this.cam.FarPlaneDistance = 8000;
            this.cam.NearPlaneDistance = 100;
            this.cam.FieldOfView = 10;

            //where virtual camera is positioned
            this.cam.Position = new Point3D(160, 120, -1000);
            this.cam.LookDirection = new Vector3D(0, 0, 1);
            this.cam.UpDirection = new Vector3D(0, 1, 0);

            using (Graphics g = Graphics.FromImage(plan))
            {
                System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, this.plan.Width, this.plan.Height);
                g.FillRectangle(System.Drawing.Brushes.White, rect);
            }

            SolidColorBrush pcColor = new SolidColorBrush(Colors.White);
            int i = 0;
            for (int y = 0; y < this.height; y += step)
            {
                for (int x = 0; x < this.width; x += step)
                {
                    this.points[i] = util.meshShape(x, y, step, pcColor);
                    this.points[i].Transform = new TranslateTransform3D(0, 0, 0);
                    this.modelGroup.Children.Add(this.points[i]);
                    i++;
                }
            }

            this.modelGroup.Children.Add(this.lighting);

            ModelVisual3D modelVisual = new ModelVisual3D();
            modelVisual.Content = this.modelGroup;
            Viewport3D viewport = new Viewport3D();
            viewport.IsHitTestVisible = false;
            viewport.Camera = cam;
            viewport.Children.Add(modelVisual);

            Canvas1.Children.Add(viewport);
            viewport.Height = Canvas1.Height;
            viewport.Width = Canvas1.Width;
            Canvas.SetTop(viewport, 0);
            Canvas.SetLeft(viewport, 0);

            sensor = KinectSensor.KinectSensors[0];
            sensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
            sensor.DepthFrameReady += sensor_DepthFrameReady;
            sensor.Start();
        }

        void sensor_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame f = e.OpenDepthImageFrame())
            {
                if (f != null)
                {
                    this.startCount++;
                    this.pixelData = new short[f.PixelDataLength];
                    //this.background_accum = new int[320 * 240];
                    f.CopyPixelDataTo(pixelData);
                    pixelData = nr.applyPixelFilter(pixelData, 320, 240);

                    if(this.lastSpace!=0)
                        spaceData.Source = this.backgroundModel.showSpaceFacts(this.lastSpace);

                    byte[] colorFrame = new byte[f.Width * f.Height * 4];

                    int stride = f.Width * f.BytesPerPixel;
                    int temp = 0;
                    int i = 0;
                    for (int y = 0; y < this.height; y += step)
                    {
                        for (int x = 0; x < this.width; x += step)
                        {
                            temp = ((ushort)pixelData[x + y * 320]) >> 3;
                            ((TranslateTransform3D)this.points[i].Transform).OffsetZ = temp;

                            if (this.startCount == 30)
                            {
                               // if (temp > 1524 && temp > 0)
                                    this.backgroundModel.setBackground(x, y, temp);
                            }
                            i++;

                        }
                    }

                    if (this.startCount > 30)
                    {
                        Debug.Text = this.backgroundModel.checkWithMap(this.hmap.coordinatesofPeople(pixelData, 95, f.Width, f.Height)).ToString();
                       // Debug.Text = this.backgroundModel.check(pixelData).ToString();
                        //this.hmap.detectPersonBlob(pixelData, 50, f.Width, f.Height);
                    }
                    depthImage.Source = detectPersonBlob(pixelData, 95, f.Width, f.Height);
                   // depthImage.Source = BitmapSource.Create(f.Width, f.Height, 96, 96, PixelFormats.Gray16, null, pixelData, stride);
                }
            }
        
        }

        private void Canvas1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Point mousepoint = e.GetPosition(depthImage);
            int x = (int) mousepoint.X / step;
            int y = (int)mousepoint.Y / step;

            //this.firstpoint = markershape(x, y, step, new SolidColorBrush(Colors.YellowGreen));
            
            
            ((TranslateTransform3D) this.firstpoint.Transform).OffsetX = mousepoint.X;
            ((TranslateTransform3D) this.firstpoint.Transform).OffsetY = mousepoint.Y;
            ((TranslateTransform3D)this.firstpoint.Transform).OffsetZ = 0; // this.pixelData[x + y * 320];
            
           // ((TranslateTransform3D) this.firstpoint.Transform).OffsetZ = ((TranslateTransform3D) this.points[x + y*320].Transform).OffsetZ ;

        }

 
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            

            foreach (PlaneModel removePlane in this.planes)
            {
                this.modelGroup.Children.Remove(removePlane.plane);
            }

            this.backgroundModel.clearDivisions();
            this.planes.Clear();
            //get colors
            this._colors = new List<System.Windows.Media.Color>();
            foreach (ComboBox _colorpicker in FindVisualChilren<ComboBox>(mw))
            {
                this._colors.Add(parseColor((String) _colorpicker.SelectedItem));
            }
            int c = 0;
            foreach (Grid grid in FindVisualChilren<Grid>(mw))
            {
                if (!grid.Name.Equals("mainGrid"))
                {
                    Point3DCollection _points = new Point3DCollection();
                    foreach (TextBox txt in FindVisualChilren<TextBox>(grid))
                    {
                        int[] point = Array.ConvertAll(txt.Text.Split(','), s => int.Parse(s));
                        _points.Add(new Point3D(point[0] + 160, point[1] +120, 2000 ));
                        _points.Add(new Point3D(point[0] + 160, point[1] + 120, 1000));

                    }
                    if (_points.Count > 0)
                    {
                        for (int i = 1; i < _points.Count; i++) {
                        using (Graphics g = Graphics.FromImage(this.plan))
                        {
                            g.DrawLine(new System.Drawing.Pen(System.Drawing.Color.Red), new PointF((float)_points[i - 1].X, (float) _points[i-1].Y), new PointF((float)_points[i].X, (float)_points[i].Y)); 
                        }
                        }
                        
                        GeometryModel3D _plane = getplane(_points, this._colors.ElementAt(c));
                        _plane.Transform = new TranslateTransform3D(0, 0, 0);
                        PlaneModel _planeModel = new PlaneModel(_plane, this._colors.ElementAt(c), _points);
                        this.planes.Add(_planeModel);
                        this.backgroundModel.addDivision(_planeModel);
                        c++;
                    }
                }
            }

            foreach (PlaneModel addPlane in this.planes)
            {
                this.modelGroup.Children.Add(addPlane.plane);

            }
            this.backgroundModel.setVirtualSpaces();
           // this.modelGroup.Children.Add(this.lighting);

            
        }

        public static IEnumerable<T> FindVisualChilren<T>(DependencyObject obj) where T : DependencyObject 
        {
            if (obj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }
                    foreach (T childOfChild in FindVisualChilren <T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.planeCount++;
            Grid grid = new Grid();
            grid.Name = "grid" + this.planeCount;
            grid.Background = new SolidColorBrush(Colors.White);
            grid.Margin = new Thickness(929 + this.planeCount*115 ,270,1063 - this.planeCount*115 ,274);

            TextBox _firstPoint = new TextBox();
            _firstPoint.Name = "point" + this.planeCount + "1";
            _firstPoint.RenderSize = new System.Windows.Size(88, 25);
            _firstPoint.Text = "60, 100";
            _firstPoint.Margin = new Thickness(0, 7, 0, 0);
            grid.Children.Add(_firstPoint);

            TextBox _secondPoint = new TextBox();
            _secondPoint.Name = "point" + this.planeCount + "2";
            _secondPoint.RenderSize = new System.Windows.Size(88, 25);
            _secondPoint.Text = "60, -100";
            _secondPoint.Margin = new Thickness(0, 35, 0, 0);
            grid.Children.Add(_secondPoint);

            //TextBox _thirdPoint = new TextBox();
            //_thirdPoint.Name = "point" + this.planeCount + "3";
            //_thirdPoint.RenderSize = new System.Windows.Size(88, 25);
            //_thirdPoint.Text = "-100, 0, 0";
            //_thirdPoint.Margin = new Thickness(0, 63, 0, 0);
            //grid.Children.Add(_thirdPoint);

            //TextBox _fourthPoint = new TextBox();
            //_fourthPoint.Name = "point" + this.planeCount + "4";
            //_fourthPoint.RenderSize = new System.Windows.Size(88, 25);
            //_fourthPoint.Text = "100, 0, 0";
            //_fourthPoint.Margin = new Thickness(0, 91, 0, 0);
            //grid.Children.Add(_fourthPoint);

            ComboBox _colorPicker = new ComboBox();
            _colorPicker.Name = "ColorPicker" + this.planeCount;
           // _colorPicker.DropDownClosed += colorPicker_DropDownClosed;
            _colorPicker.Margin = new Thickness(0, 121, -12, 0);

            _colorPicker.Items.Add("White");
            _colorPicker.Items.Add("Black");
            _colorPicker.Items.Add("Red");
            _colorPicker.Items.Add("Green");
            _colorPicker.Items.Add("Aqua");
            _colorPicker.Items.Add("Violet");

            grid.Children.Add(_colorPicker);
            mainGrid.Children.Add(grid);
            
        }



        private System.Windows.Media.Color parseColor(String selected)
        {
            if (selected != null)
            {
                if (selected.Equals("White"))
                {
                    return Colors.White;
                }

                if (selected.Equals("Black"))
                {
                    return Colors.Black;
                }

                if (selected.Equals("Red"))
                {
                    return Colors.Red;
                }

                if (selected.Equals("Green"))
                {
                    return Colors.Green;
                }


                if (selected.Equals("Aqua"))
                {
                    return Colors.Aqua;
                }

                if (selected.Equals("Violet"))
                {
                    return Colors.Violet;
                }
            }
            return Colors.Aqua;
        }

        private void Canvas1_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Point endP = e.GetPosition(Canvas1);
            double distX = endP.X - this.initP.X;
            double distY = endP.Y - this.initP.Y;

            Vector3D right = Vector3D.CrossProduct(this.cam.LookDirection, this.cam.UpDirection);
            this.cam.Position = this.cam.Position - (right * distX);

            this.cam.Position = this.cam.Position + (cam.UpDirection * distY);
            

        }

        private void Canvas1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
           this.initP = e.GetPosition(Canvas1);
           return;
        }

        private void Canvas1_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            //Vector3D _lookdirection = this.cam.LookDirection;
            Vector3D _position = this.cam.LookDirection;

            double distX = this.rotationspeed * (e.GetPosition(Canvas1).X - this.initP.X);
            double distY = this.rotationspeed * (e.GetPosition(Canvas1).Y - this.initP.Y);


            double newX = _position.X + distX;
            double newY = _position.Y + distY;
            double newZ = _position.Z;

            Vector3D newDirection = new Vector3D(newX, newY, newZ);
            this.cam.LookDirection = newDirection;
            return;


        }

        private void Canvas1_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double dist = e.Delta;
            this.cam.Position = this.cam.Position + (this.cam.LookDirection * dist);
            return;
        }

        private void Canvas1_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.initP = e.GetPosition(Canvas1);
            return;
        }

        private void layerImage_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            int sp = this.backgroundModel.whichSpace((int) e.GetPosition(depthImage).X, (int) e.GetPosition(depthImage).Y);
            this.lastSpace = sp;
            layerImage.Source = this.backgroundModel.showSpace(sp);
            spaceData.Source = this.backgroundModel.showSpaceFacts(this.lastSpace);
        }
    }
}
