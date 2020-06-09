using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ZenithEngine.ModuleUI
{
    public class UIDock : DockPanel, ISerializableContainer
    {
        UIContainerData childData;

        public UIDock(Dock stack)
        {
            childData = UITools.GetChildren(this);
            HorizontalAlignment = HorizontalAlignment.Left;
            VerticalAlignment = VerticalAlignment.Top;
            foreach (var c in childData.Elements)
            {
                Children.Add(c);
                SetDock(c, stack);
            }
        }
        public UIDock() : this(Dock.Top) { }

        public void Parse(JObject container)
        {
            UITools.ParseContainer(container, childData);
        }

        public JObject Serialize()
        {
            return UITools.SerializeContainer(childData);
        }
    }
}
