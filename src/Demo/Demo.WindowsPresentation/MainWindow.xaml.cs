using System.Windows;
using GMap.NET.Caching.Oracle;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;

namespace GMap.NET.Cache.Oracle.Demo.WindowsPresentation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly GMapControl _mainMap;

        public MainWindow()
        {
            InitializeComponent();
            _mainMap = new GMapControl
            {
                MinZoom = 0,
                MaxZoom = 24,
                MapProvider = GMapProviders.BingSatelliteMap,
                Position = new PointLatLng(34.2250209512632, 108.880734443665)
            };
            Content = _mainMap;

            _mainMap.Manager.PrimaryCache =
                new OraclePureImageCache(OracleDbHelper.ConnectionString, GMapImageProxy.Instance);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _mainMap.Zoom = 10;
        }
    }
}