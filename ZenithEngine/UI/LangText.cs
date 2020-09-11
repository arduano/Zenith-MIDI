using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ZenithEngine.UI
{
    public class LangText : DynamicResourceExtension
    {
        public LangText(string key) : base("lang." + key)
        { }
    }
}
