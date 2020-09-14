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
    public class UITabGroup : BaseContainerBasic<TabControl>
    {
        public UITabGroup() : base(new TabControl())
        {
            foreach (var c in ChildData.Elements)
            {
                if (!(c is TabItem)) throw new InvalidCastException("UITabGroup must only have UITab as children");
                Control.Items.Add(c as TabItem);
            }
        }
    }

    public class UITab : BaseContainerBasic<TabItem>
    {
        DockPanel panel = new DockPanel();

        public UITab(object name) : this(name, Dock.Top) { }
        public UITab(object name, Dock dock, bool lastChildFill = false) : base(new TabItem())
        {
            Control.Content = panel;
            foreach (var e in ChildData.Elements)
            {
                DockPanel.SetDock(e, dock);
                panel.Children.Add(e);
            }
            panel.LastChildFill = lastChildFill;
        }

        object label = null;
        public object Name
        {
            get => label;
            set
            {
                label = value;
                if (label is DynamicResourceExtension)
                {
                    var l = (DynamicResourceExtension)label;
                    Control.SetResourceReference(TabItem.HeaderProperty, l.ResourceKey);
                }
                else
                {
                    Control.Header = label;
                }
            }
        }
    }
}
