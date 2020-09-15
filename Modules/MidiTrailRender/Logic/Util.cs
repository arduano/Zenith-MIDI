using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MIDITrailRender.Logic
{
    static class Util
    {
        static Assembly assembly = Assembly.GetExecutingAssembly();

        public static Stream OpenEmbedStream(string name)
        {
            return assembly.GetManifestResourceStream(name);
        }

        public static string ReadEmbed(string name)
        {
            using (var stream = OpenEmbedStream(name))
            using (var reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }
    }
}
