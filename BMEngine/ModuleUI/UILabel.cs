using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace ZenithEngine.ModuleUI
{
    public class UILabel : Label
    {
        object label = null;
        public object Label
        {
            get => label;
            set
            {
                label = value;
                if (label is DynamicResourceExtension)
                {
                    var l = (DynamicResourceExtension)label;
                    SetResourceReference(ContentProperty, l.ResourceKey);
                }
                else
                {
                    Content = label;
                }
            }
        }

        public UILabel()
        {
            Margin = new Thickness(0, 0, 5, 5);
            HorizontalAlignment = HorizontalAlignment.Left;
            VerticalAlignment = VerticalAlignment.Top;
        }
    }
}
