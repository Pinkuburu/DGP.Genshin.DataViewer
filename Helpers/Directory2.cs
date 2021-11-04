using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DGP.Genshin.DataViewer.Helpers
{
    public class Directory2
    {
        public static IEnumerable<File2> GetFileExs(string path)
        {
            return Directory.GetFiles(path).Select(f => new File2(f));
        }
    }
}
