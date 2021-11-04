
using Ookii.Dialogs.Wpf;

namespace DGP.Genshin.DataViewer.Services
{
    internal static class WorkingFolderService
    {
        public static void SelectWorkingFolder()
        {
            VistaFolderBrowserDialog folder = new()
            {
                Description = "选择数据所在文件夹",
            };
            if (folder.ShowDialog() == true)
            {
                WorkingFolderPath = folder.SelectedPath;
            }
        }

        public static string? WorkingFolderPath { get; set; }
    }
}
