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
    public class UITabGroup : Grid, ISerializableContainer
    {
        UIContainerData childData;

        public UITabGroup()
        {
            childData = UITools.GetChildren(this);
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;

            var tabs = new TabControl();
            tabs.HorizontalAlignment = HorizontalAlignment.Stretch;
            tabs.VerticalAlignment = VerticalAlignment.Stretch;
            tabs.Margin = new Thickness(10);

            Children.Add(tabs);

            foreach (var c in childData.Elements)
            {
                if (!(c is UITab)) throw new InvalidCastException("UITabGroup must only have UITab as children");
                tabs.Items.Add((c as UITab).TabItem);
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

    public class UITab : Control, ISerializableContainer
    {
        UIContainerData childData;

        public TabItem TabItem { get; }

        public new Thickness Margin { get => dock.Margin; set => dock.Margin = value; }
        DockPanel dock = new DockPanel();

        public UITab(string name, bool lastItemFill = false)
        {
            Margin = new Thickness(10);

            childData = UITools.GetChildren(this);

            TabItem = new TabItem();
            TabItem.Header = name;

            TabItem.HorizontalAlignment = HorizontalAlignment.Stretch;
            TabItem.VerticalAlignment = VerticalAlignment.Stretch;

            TabItem.Content = dock;
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
