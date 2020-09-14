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
    public class UIDock : BaseContainer<DockPanel>
    {
        public UIDock() : this(Dock.Top) { }
        public UIDock(Dock dock, bool lastChildFill = false) : base(new DockPanel())
        {
            foreach(var e in ChildData.Elements)
            {
                DockPanel.SetDock(e, dock);
                Control.Children.Add(e);
            }
            Control.LastChildFill = lastChildFill;
        }
    }
}
