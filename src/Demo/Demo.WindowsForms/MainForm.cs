using System.Windows.Forms;
using GMap.NET.Caching.Oracle;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;

namespace GMap.NET.Cache.Oracle.Demo.WindowsForms
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            if (!GMapControl.IsDesignerHosted)
            {
                MainMap.MapProvider = GMapProviders.BingSatelliteMap;
                MainMap.Position = new PointLatLng(34.2250209512632, 108.880734443665);

                MainMap.Manager.PrimaryCache =
                    new OraclePureImageCache(OracleDbHelper.ConnectionString, GMapImageProxy.Instance);
            }
        }
    }
}
