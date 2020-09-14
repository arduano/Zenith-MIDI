using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace ZenithEngine.ModuleUI
{
    public class UIDockWithPalettes : BaseContainer<DockPanel>
    {
        DockPanel innerPanel = new DockPanel();

        public NoteColorPalettePick Palette { get; } = new NoteColorPalettePick();

        public UIDockWithPalettes() : this(Dock.Top) { }
        public UIDockWithPalettes(Dock dock, bool lastChildFill = false) : base(new DockPanel())
        {
            Control.LastChildFill = true;
            Palette.SetPath("Plugins\\Assets\\Palettes");
            DockPanel.SetDock(Palette, Dock.Right);

            Control.Children.Add(Palette);
            Control.Children.Add(innerPanel);

            foreach (var e in ChildData.Elements)
            {
                DockPanel.SetDock(e, dock);
                innerPanel.Children.Add(e);
            }
            innerPanel.LastChildFill = lastChildFill;
        }
    }
}
