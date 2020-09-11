using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ZenithEngine.UI
{
    public class Overlay : ContentControl
    {
        public bool Showing
        {
            get { return (bool)GetValue(ShowingProperty); }
            set { SetValue(ShowingProperty, value); }
        }

        public static readonly DependencyProperty ShowingProperty =
            DependencyProperty.Register("Showing", typeof(bool), typeof(Overlay), new PropertyMetadata(false));

        public Overlay()
        {
            new InplaceConverter<bool, Visibility>(
                new BBinding(ShowingProperty, this),
                showing => showing ? Visibility.Visible : Visibility.Hidden)
                .Set(this, VisibilityProperty);
        }
    }
}
