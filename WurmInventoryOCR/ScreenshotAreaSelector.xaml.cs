using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Image = System.Drawing.Image;
using Size = System.Windows.Size;

namespace WurmInventoryOCR
{
    public class PropertyChangedBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
            }));
        }
    }
    public enum Shapes
    {
        Round = 1,
        Square = 2,
        AnyOther
    }

    public class ROI : PropertyChangedBase
    {
        private Shapes _shape;
        public Shapes Shape
        {
            get { return _shape; }
            set
            {
                _shape = value;
                OnPropertyChanged("Shape");
            }
        }

        private double _scaleFactor;
        public double ScaleFactor
        {
            get { return _scaleFactor; }
            set
            {
                _scaleFactor = value;
                OnPropertyChanged("ScaleFactor");
                OnPropertyChanged("ActualX");
                OnPropertyChanged("ActualY");
                OnPropertyChanged("ActualHeight");
                OnPropertyChanged("ActualWidth");
            }
        }

        private double _x;
        public double X
        {
            get { return _x; }
            set
            {
                _x = value;
                OnPropertyChanged("X");
                OnPropertyChanged("ActualX");
            }
        }

        private double _y;
        public double Y
        {
            get { return _y; }
            set
            {
                _y = value;
                OnPropertyChanged("Y");
                OnPropertyChanged("ActualY");
            }
        }

        private double _height;
        public double Height
        {
            get { return _height; }
            set
            {
                _height = value;
                OnPropertyChanged("Height");
                OnPropertyChanged("ActualHeight");
            }
        }

        private double _width;
        public double Width
        {
            get { return _width; }
            set
            {
                _width = value;
                OnPropertyChanged("Width");
                OnPropertyChanged("ActualWidth");
            }
        }

        public double ActualX { get { return X * ScaleFactor; } }
        public double ActualY { get { return Y * ScaleFactor; } }
        public double ActualWidth { get { return Width * ScaleFactor; } }
        public double ActualHeight { get { return Height * ScaleFactor; } }
    }








    /// <summary>
    /// Interaktionslogik für ScreenshotAreaSelector.xaml
    /// </summary>
    public partial class ScreenshotAreaSelector : Window, INotifyPropertyChanged
    {
        private readonly Action<ROI> _resultCallback;
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
            }));
        }


        public ScreenshotAreaSelector(Image image, List<ROI> predefinedROIs, Action<ROI> resultCallback)
        {
            _resultCallback = resultCallback;
            DataContext = this;

            InitializeComponent();

            MemoryStream ms = new MemoryStream();
            
            image.Save(ms, ImageFormat.Bmp);
            byte[] buffer = ms.GetBuffer();
            MemoryStream bufferPasser = new MemoryStream(buffer);
            
            
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = bufferPasser;
            bitmap.EndInit();

            //var ImageElement = scr.FindName("ImageElement") as System.Windows.Controls.Image;
            //ImageElement.Source = bitmap;
            ImageSource = bitmap;
            OnPropertyChanged("ImageSource");

            //CanvasElement.Width = bitmap.SourceRect.Width;
            //CanvasElement.Height = bitmap.SourceRect.Height;

            if (predefinedROIs.Count == 0)
                ROIs.Add(new ROI() { ScaleFactor = ScaleFactor, X = 20, Y = 20, Height = 200, Width = 300, Shape = Shapes.Square });

            foreach (var predefinedROI in predefinedROIs)
            {
                ROIs.Add(predefinedROI);
            }
            

        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            _resultCallback(ROIs.First());
        }

        private static T RecursiveVisualChildFinder<T>(DependencyObject rootObject) where T : class
        {
            var child = VisualTreeHelper.GetChild(rootObject, 0);
            if (child == null) return null;

            return child.GetType() == typeof(T) ? child as T : RecursiveVisualChildFinder<T>(child);
        }


        private double _offsetX;
        public double OffsetX
        {
            get { return _offsetX; }
            set
            {
                _offsetX = value;
                OnPropertyChanged("OffsetX");
            }
        }

        private double _offsetY;
        public double OffsetY
        {
            get { return _offsetY; }
            set
            {
                _offsetY = value;
                OnPropertyChanged("OffsetY");
            }
        }

        private double _scaleFactor = 1;
        public double ScaleFactor
        {
            get { return _scaleFactor; }
            set
            {
                _scaleFactor = value;
                OnPropertyChanged("ScaleFactor");
                ROIs.ToList().ForEach(x => x.ScaleFactor = value);
            }
        }

        public ImageSource ImageSource { get; set; }



        private ObservableCollection<ROI> _rois;
        public ObservableCollection<ROI> ROIs
        {
            get { return _rois ?? (_rois = new ObservableCollection<ROI>()); }
        }


        private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            //TODO: Detect whether a ROI is being resized / dragged and prevent Panning if so.
            IsPanning = true;
            //OffsetX = (OffsetX + (((e.HorizontalChange / 10) * -1) * ScaleFactor));
            //OffsetY = (OffsetY + (((e.VerticalChange / 10) * -1) * ScaleFactor));
            //
            //scr.ScrollToVerticalOffset(OffsetY);
            //scr.ScrollToHorizontalOffset(OffsetX);

            IsPanning = false;
        }

        private bool IsPanning { get; set; }

        private void ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (!IsPanning)
            {
                OffsetX = e.HorizontalOffset;
                OffsetY = e.VerticalOffset;
            }
        }












    }
}
