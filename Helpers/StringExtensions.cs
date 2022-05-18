using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SBAST.UniversalIntegrator.Helpers
{
    public static class StringExtensions
    {
        public static bool IsNullOrEmpty(this string source) => string.IsNullOrEmpty(source);

        public static string UriCombine(this string path1, string path2) => Path.Combine(path1, path2).Replace("\\", "/");
        public static string StringJoin(this IEnumerable<string> source, string separator = "") => string.Join(separator, source);
        
    }
}
