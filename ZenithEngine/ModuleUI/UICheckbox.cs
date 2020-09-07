using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ZenithEngine.UI;

namespace ZenithEngine.ModuleUI
{
    public class UICheckbox : Checkbox, ISerializableItem, IValueItem<bool>
    {
        public static implicit operator bool(UICheckbox val) => val.Value;

        public bool ValueInternal
        {
            get => (bool)IsChecked;
            set { if (IsChecked != value) IsChecked = value; }
        }

        bool cacheValue = false;
        public bool Value
        {
            get => cacheValue;
            set
            {
                cacheValue = value;
                UITools.SyncValue(this);
            }
        }

        object label = null;

        public event EventHandler<bool> ValueChanged;

        public object Label
        {
            get => label; set
            {
                label = value;
                if (label is DynamicResourceExtension)
                {
                    var l = (DynamicResourceExtension)label;
                    SetResourceReference(ContentProperty, l.ResourceKey);
                }
                else
                {
                    if (label is string) Content = (string)label;
                    else Content = label.ToString();
                }
            }
        }

        public UICheckbox()
        {
            Margin = new Thickness(0, 5, 10, 10);
            HorizontalAlignment = HorizontalAlignment.Left;
            VerticalAlignment = VerticalAlignment.Top;

            CheckToggled += (s, e) =>
            {
                ValueChanged?.Invoke(this, (bool)IsChecked);
            };

            UITools.BindValue(this);
        }

        public void Parse(string data)
        {
            IsChecked = Convert.ToBoolean(data);
        }

        public string Serialize()
        {
            return IsChecked.ToString();
        }
    }
}
