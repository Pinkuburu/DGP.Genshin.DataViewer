using System.Windows;

namespace DGP.Genshin.DataViewer
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();
            DirectoryView.ExcelSplitView = ExcelDataView;
        }

    }
}
