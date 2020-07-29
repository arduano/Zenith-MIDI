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
    public class UITabGroup : TabControl, ISerializableContainer
    {
        UIContainerData childData;

        public UITabGroup()
        {
            childData = UITools.GetChildren(this);
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            foreach (var c in childData.Elements)
            {
                if (!(c is UITab)) throw new InvalidCastException("UITabGroup must only have UITab as children");
                Items.Add(c as UITab);
            }
        }

        public void Parse(JObject container)
        {
            UITools.ParseContainer(container, childData);
        }

        public JObject Serialize()
        {
            return UITools.SerializeContainer(childData);
        }
    }

    public class UITab : TabItem, ISerializableContainer
    {
        UIContainerData childData;

        public UITab(bool lastItemFill = false)
        {
            childData = UITools.GetChildren(this);
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;

            DockPanel dock = new DockPanel();
            Content = dock;
            dock.HorizontalAlignment = HorizontalAlignment.Stretch;
            dock.VerticalAlignment = VerticalAlignment.Stretch;
            dock.LastChildFill = lastItemFill;

            foreach (var c in childData.Elements)
            {
                dock.Children.Add(c);
                DockPanel.SetDock(c, Dock.Top);
            }
        }

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
