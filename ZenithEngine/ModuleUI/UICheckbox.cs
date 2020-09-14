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
    public class UICheckbox : BaseItem<Checkbox, bool>
    {
        public UICheckbox(string name, object label, bool isChecked) : base(name, new Checkbox(), isChecked)
        {
            Label = label;
        }

        public object Label
        {
            get => Control.Content;
            set
            {
                Control.Content = value;
                if (value is DynamicResourceExtension)
                {
                    var l = (DynamicResourceExtension)value;
                    Control.SetResourceReference(ContentControl.ContentProperty, l.ResourceKey);
                }
            }
        }

        public override bool ValueInternal { get => Control.IsChecked ?? false; set => Control.IsChecked = value; }

        public override void Parse(string data)
        {
            try
            {
                Value = Convert.ToBoolean(data);
            }
            catch { }
        }

        public override string Serialize()
        {
            return Value.ToString();
        }
    }
}
