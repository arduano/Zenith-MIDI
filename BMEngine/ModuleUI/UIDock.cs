using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ZenithEngine.ModuleUI
{
    public class UIDock<T> : DockPanel, IContainerItem<T>
    {
        public T items { get; private set; }
        T IContainerItem<T>.Children => items;

        public UIDock(T children)
        {
            items = children;
        }

        public void Parse(JObject container)
        {
            throw new NotImplementedException();
        }

        public JObject Serialize()
        {
            throw new NotImplementedException();
        }
    }
}
