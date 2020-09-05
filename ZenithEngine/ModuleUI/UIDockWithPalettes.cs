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
    public class UIDockWithPalettes : Grid, ISerializableContainer
    {
        UIContainerData childData;

        public NoteColorPalettePick Palette { get; } = new NoteColorPalettePick();

        public UIDockWithPalettes(Dock stack)
        {
            Palette.SetPath("Plugins\\Assets\\Palettes");

            var child = new DockPanel();
            var items = new DockPanel();
            child.Margin = new Thickness(10);
            items.Margin = new Thickness(0, 0, 10, 0);

            Children.Add(child);
            child.Children.Add(items);

            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;

            items.Children.Add(Palette);
            Palette.HorizontalAlignment = HorizontalAlignment.Right;
            Palette.VerticalAlignment = VerticalAlignment.Stretch;

            DockPanel.SetDock(Palette, Dock.Right);
            child.LastChildFill = true;

            childData = UITools.GetChildren(this);
            foreach (var c in childData.Elements)
            {
                items.Children.Add(c);
                DockPanel.SetDock(c, stack);
            }
        }
        public UIDockWithPalettes() : this(Dock.Top) { }

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
