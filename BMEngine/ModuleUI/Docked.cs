using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace ZenithEngine.ModuleUI
{
    public abstract class Docked<T> : DockPanel, IFieldItem<T>
    {
        protected UILabel labelItem = new UILabel() { Visibility = Visibility.Collapsed, Margin = new Thickness(0, 0, 5, 0) };

        object label = null;
        public object Label
        {
            get => label; set
            {
                label = value;
                if (label == null) labelItem.Visibility = Visibility.Collapsed;
                else labelItem.Visibility = Visibility.Visible;
                labelItem.Label = label;
            }
        }

        public abstract T Value { get; set; }
        public abstract event EventHandler<T> ValueChanged;

        public abstract void Parse(string value);
        public abstract string Serialize();

        public Docked()
        {
            Margin = new Thickness(0, 0, 0, 10);
        }
    }
}
