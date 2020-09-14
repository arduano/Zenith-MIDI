using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ZenithEngine.ModuleUI
{
    public class BaseElement
    { }

    public class BaseElement<C> : BaseElement, IControl
        where C : FrameworkElement
    {
        public C Control { get; }

        public BaseElement(C control)
        {
            Control = control;
        }

        public double Width { get => Control.Width; set => Control.Width = value; }
        public double Height { get => Control.Height; set => Control.Height = value; }
        public double MinWidth { get => Control.MinWidth; set => Control.MinWidth = value; }
        public double MinHeight { get => Control.MinHeight; set => Control.MinHeight = value; }
        public double MaxWidth { get => Control.MaxWidth; set => Control.MaxWidth = value; }
        public double MaxHeight { get => Control.MaxHeight; set => Control.MaxHeight = value; }
        public HorizontalAlignment HorizontalAlignment { get => Control.HorizontalAlignment; set => Control.HorizontalAlignment = value; }
        public VerticalAlignment VerticalAlignment { get => Control.VerticalAlignment; set => Control.VerticalAlignment = value; }
        public Thickness Margin { get => Control.Margin; set => Control.Margin = value; }

        UIElement IControl.Control => Control;
    }
}
