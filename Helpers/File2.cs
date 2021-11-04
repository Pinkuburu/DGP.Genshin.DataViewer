using System.IO;

namespace DGP.Genshin.DataViewer.Helpers
{
    public class File2
    {
        public File2(string fullPath)
        {
            FullPath = fullPath;
        }

        public string FullPath { get; set; }
        public string FullFileName => Path.GetFileNameWithoutExtension(FullPath);
        public string FileName => Path.GetFileNameWithoutExtension(FullPath)
            .Replace("Excel", "").Replace("Config", "").Replace("Data", "");
        public override string ToString()
        {
            return FileName;
        }

        public string Read()
        {
            string str;
            using (StreamReader sr = new(FullPath))
            {
                str = sr.ReadToEnd();
            }
            return str;
        }
    }
}
