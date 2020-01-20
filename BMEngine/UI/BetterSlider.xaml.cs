using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ZenithEngine.UI
{
    /// <summary>
    /// Interaction logic for BetterSlider.xaml
    /// </summary>
    public partial class BetterSlider : UserControl
    {
        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(double), typeof(BetterSlider), new PropertyMetadata(0.0));


        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(double), typeof(BetterSlider), new PropertyMetadata(0.0));


        static void ValueChangedCallback(DependencyObject s, DependencyPropertyChangedEventArgs e)
        {
            var slider = (BetterSlider)s;
            //slider.ScaledValue = (slider.Value - slider.Minimum) / slider.Maximum * slider.barGrid.ActualWidth;
        }

        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set
            {
                var v = value;
                if (v > Maximum) v = Maximum;
                if (v < Minimum) v = Minimum;
                SetValue(ValueProperty, v);
            }
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(double), typeof(BetterSlider), new PropertyMetadata(0.0, ValueChangedCallback));


        double ScaledValue
        {
            get { return (double)GetValue(ScaledValueProperty); }
            set { SetValue(ScaledValueProperty, value); }
        }

        static readonly DependencyProperty ScaledValueProperty =
            DependencyProperty.Register("ScaledValue", typeof(double), typeof(BetterSlider), new PropertyMetadata(0.0));


        public event EventHandler<double> UserValueChanged;


        public BetterSlider()
        {
            InitializeComponent();

            new InplaceConverter(new[]
            {
                new BBinding(ValueProperty, this),
                new BBinding(MinimumProperty, this),
                new BBinding(MaximumProperty, this),
                new BBinding(ActualWidthProperty, barGrid),
            },
            (values) =>
            {
                try
                {
                    return ((double)values[0] - (double)values[1]) / ((double)values[2] - (double)values[1]) * (double)values[3];
                }
                catch { return 0; }
            })
                .Set(this, ScaledValueProperty);

            new InplaceConverter(new[] { new BBinding(ScaledValueProperty, this) },
            (values) => new Thickness((double)values[0], 0, 0, 0))
                .Set(headGrid, MarginProperty);
        }

        private void ClickerGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            //hoverEllipse.Visibility = Visibility.Visible;
        }

        private void ClickerGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            //hoverEllipse.Visibility = Visibility.Hidden;
        }

        private void ClickerGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            clickerGrid.CaptureMouse();
            Value = e.GetPosition(barGrid).X / barGrid.ActualWidth * (Maximum - Minimum) + Minimum;
            UserValueChanged?.Invoke(this, Value);
            AddRipple();
            this.Focus();
        }

        private void ClickerGrid_MouseMove(object sender, MouseEventArgs e)
        {
            if (clickerGrid.IsMouseCaptureWithin)
            {
                Value = e.GetPosition(barGrid).X / barGrid.ActualWidth * (Maximum - Minimum) + Minimum;
                UserValueChanged?.Invoke(this, Value);
            }
        }

        private void ClickerGrid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            clickerGrid.ReleaseMouseCapture();
        }

        void AddRipple()
        {
            double ExpandTime = 0.1;
            double FadeTime = 0.1;

            double o = 0.7;

            var targetWidth = auraGrid.ActualWidth;

            var ellipse = new Ellipse()
            {
                Fill = (Brush)Resources["PrimaryBrush"],
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Opacity = o
            };
            ellipse.SetBinding(HeightProperty, new Binding("Width") { Source = ellipse });

            Storyboard storyboard = new Storyboard();

            var expand = new DoubleAnimation(0, targetWidth, new Duration(TimeSpan.FromSeconds(ExpandTime + FadeTime)));
            storyboard.Children.Add(expand);
            Storyboard.SetTarget(expand, ellipse);
            Storyboard.SetTargetProperty(expand, new PropertyPath(WidthProperty));

            var opacity = new DoubleAnimation(o, 0, new Duration(TimeSpan.FromSeconds(FadeTime)));
            opacity.BeginTime = TimeSpan.FromSeconds(ExpandTime);
            storyboard.Children.Add(opacity);
            Storyboard.SetTarget(opacity, ellipse);
            Storyboard.SetTargetProperty(opacity, new PropertyPath(Ellipse.OpacityProperty));

            auraGrid.Children.Add(ellipse);

            storyboard.Begin();

            var waitTime = ExpandTime + FadeTime;
            Task.Run(() =>
            {
                Thread.Sleep(TimeSpan.FromSeconds(waitTime));
                Dispatcher.Invoke(() =>
                {
                    headGrid.Children.Remove(ellipse);
                });
            });
        }
    }

    public class DoubleMultiplyConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double r = 1;
            foreach (var v in values) r *= (double)v;
            return r;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ScaledValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return ((double)values[0] - (double)values[1]) / ((double)values[2] - (double)values[1]) * (double)values[3];
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ThicknessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new Thickness(
                    (double)value,
                    0,
                    0,
                    0
                );
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
