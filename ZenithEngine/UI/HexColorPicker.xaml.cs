using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ZenithEngine.UI
{
    /// <summary>
    /// Interaction logic for HexColorPicker.xaml
    /// </summary>
    public partial class HexColorPicker : UserControl
    {
        public Color Color
        {
            get { return (Color)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }

        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register("Color", typeof(Color), typeof(HexColorPicker), new PropertyMetadata(Color.FromArgb(255, 255, 255, 255), (s, e) => ((HexColorPicker)s).OnColorPropertyChanged(e)));


        public bool UseAlpha
        {
            get { return (bool)GetValue(UseAlphaProperty); }
            set { SetValue(UseAlphaProperty, value); }
        }

        public static readonly DependencyProperty UseAlphaProperty =
            DependencyProperty.Register("UseAlpha", typeof(bool), typeof(HexColorPicker), new PropertyMetadata(false));

        public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
            "ValueChanged", RoutingStrategy.Bubble,
            typeof(RoutedPropertyChangedEventHandler<Color>), typeof(HexColorPicker));

        public event RoutedPropertyChangedEventHandler<Color> ValueChanged
        {
            add { AddHandler(ValueChangedEvent, value); }
            remove { RemoveHandler(ValueChangedEvent, value); }
        }


        void OnColorPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            SaveString();
            RaiseEvent(new RoutedPropertyChangedEventArgs<Color>((Color)e.OldValue, (Color)e.NewValue, ValueChangedEvent));
        }

        string Hexify(byte val)
        {
            var s = val.ToString("X");
            if (s.Length == 1) return "0" + s;
            return s;
        }

        void SaveString()
        {
            string s = "";
            s += Hexify(Color.R);
            s += Hexify(Color.G);
            s += Hexify(Color.B);
            if (UseAlpha && (hexText.Text.Length != 6 || Color.A != 255))
                s += Hexify(Color.A);
            if (hexText.Text != s)
                hexText.Text = s;
        }


        public HexColorPicker()
        {
            InitializeComponent();

            new InplaceConverter(
                new[] { new BBinding(UseAlphaProperty, this) },
                (e) => (bool)e[0] ? 8 : 6
            ).Set(hexText, TextBox.MaxLengthProperty);
        }

        private void HexText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!((hexText.Text.Length == 6) || (UseAlpha && hexText.Text.Length == 8))) return;
            try
            {
                int col = int.Parse(hexText.Text, System.Globalization.NumberStyles.HexNumber);
                Color c;
                if (hexText.Text.Length == 8)
                    c = Color.FromArgb(
                        (byte)((col >> 0) & 0xFF),
                        (byte)((col >> 24) & 0xFF),
                        (byte)((col >> 16) & 0xFF),
                        (byte)((col >> 8) & 0xFF)
                    );
                else
                    c = Color.FromArgb(
                        255,
                        (byte)((col >> 16) & 0xFF),
                        (byte)((col >> 8) & 0xFF),
                        (byte)((col >> 0) & 0xFF)
                    );
                Color = c;
            }
            catch { }
        }

        private void HexText_LostFocus(object sender, RoutedEventArgs e)
        {
            SaveString();
        }

        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SaveString();
                e.Handled = true;
                Keyboard.ClearFocus();

                FrameworkElement parent = (FrameworkElement)hexText.Parent;
                while (parent != null && parent is IInputElement && !((IInputElement)parent).Focusable)
                {
                    parent = (FrameworkElement)parent.Parent;
                }

                DependencyObject scope = FocusManager.GetFocusScope(hexText);
                FocusManager.SetFocusedElement(scope, parent as IInputElement);
            }
        }
    }
}
