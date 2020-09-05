using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ZenithEngine.UI
{
    public class Slider : Control
    {
        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(double), typeof(Slider), new PropertyMetadata(0.0));


        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(double), typeof(Slider), new PropertyMetadata(1.0));


        static void ValueChangedCallback(DependencyObject s, DependencyPropertyChangedEventArgs e)
        {
            //var slider = (Slider)s;
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
            DependencyProperty.Register("Value", typeof(double), typeof(Slider), new PropertyMetadata(0.0, ValueChangedCallback));


        double ScaledValue
        {
            get { return (double)GetValue(ScaledValueProperty); }
            set { SetValue(ScaledValueProperty, value); }
        }

        static readonly DependencyProperty ScaledValueProperty =
            DependencyProperty.Register("ScaledValue", typeof(double), typeof(Slider), new PropertyMetadata(0.0));


        public event EventHandler<double> UserValueChanged;

        FrameworkElement body;
        FrameworkElement bar;
        FrameworkElement leftBar;
        FrameworkElement head;
        RippleSource rippleSource;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (Template != null)
            {
                body = Template.FindName("PART_body", this) as FrameworkElement;
                bar = Template.FindName("PART_bar", this) as FrameworkElement;
                leftBar = Template.FindName("PART_leftBar", this) as FrameworkElement;
                head = Template.FindName("PART_head", this) as FrameworkElement;
                rippleSource = Template.FindName("PART_rippleSource", this) as RippleSource;

                if (bar != null)
                {
                    new InplaceConverter<double, double, double, double, double>(
                        new BBinding(ValueProperty, this),
                        new BBinding(MinimumProperty, this),
                        new BBinding(MaximumProperty, this),
                        new BBinding(ActualWidthProperty, bar),
                            (val, min, max, act) => (val - min) / (max - min) * act)
                        .Set(this, ScaledValueProperty);

                    if (head != null)
                    {
                        new InplaceConverter<double, Thickness>(new BBinding(ScaledValueProperty, this),
                        (val) => new Thickness(val, 0, 0, 0))
                            .Set(head, MarginProperty);
                    }
                    if(leftBar != null)
                    {
                        new InplaceConverter<double, double>(new BBinding(ScaledValueProperty, this),
                        (val) => val)
                            .Set(leftBar, WidthProperty);
                    }
                }

                if (body != null)
                {
                    body.MouseDown += Body_MouseDown;
                    body.MouseMove += Body_MouseMove;
                    body.MouseUp += Body_MouseUp;
                }
            }
            else
            {
                if (body != null)
                {
                    body.MouseDown -= Body_MouseDown;
                    body.MouseMove -= Body_MouseMove;
                    body.MouseUp -= Body_MouseUp;
                }

                body = null;
                bar = null;
                leftBar = null;
                head = null;
                rippleSource = null;
            }
        }

        private void Body_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            body.ReleaseMouseCapture();
        }

        private void Body_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (body.IsMouseCaptureWithin)
            {
                Value = e.GetPosition(bar).X / bar.ActualWidth * (Maximum - Minimum) + Minimum;
                UserValueChanged?.Invoke(this, Value);
            }
        }

        private void Body_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            body.CaptureMouse();
            Value = e.GetPosition(bar).X / bar.ActualWidth * (Maximum - Minimum) + Minimum;
            UserValueChanged?.Invoke(this, Value);
            rippleSource?.SendRipple();
            this.Focus();
        }
    }
}
