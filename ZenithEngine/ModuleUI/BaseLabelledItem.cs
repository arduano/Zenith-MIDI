using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ZenithEngine.UI;

namespace ZenithEngine.ModuleUI
{
    public abstract class BaseLabelledItem<C, T> : BaseItem<LabelledItem, T>
        where C : Control
    {
        public C InnerControl { get; }

        public BaseLabelledItem(string name, object label, C control, T value) : base(name, new LabelledItem())
        {
            InnerControl = control;
            Control.Content = InnerControl;
            Label = label;
            Value = value;
        }

        public BaseLabelledItem(string name, C control, T value) : this(name, null, control, value)
        { }

        public object Label
        {
            get
            {
                string l = "";
                Application.Current.Dispatcher.Invoke(() =>
                {
                    l = Control.Label;
                });
                return l;
            }
            set
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Control.Label = value as string;
                    if (value is DynamicResourceExtension)
                    {
                        var l = (DynamicResourceExtension)value;
                        Control.SetResourceReference(LabelledItem.LabelProperty, l.ResourceKey);
                    }
                });
            }
        }
    }
}
