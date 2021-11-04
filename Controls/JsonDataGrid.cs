using System.Windows;
using System.Windows.Controls;

namespace DGP.Genshin.DataViewer.Controls
{
    public class JsonDataGrid : DataGrid
    {
        static JsonDataGrid()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(JsonDataGrid), new FrameworkPropertyMetadata(typeof(JsonDataGrid)));
        }
    }
}
