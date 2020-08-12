using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace ZenithEngine.UI
{
    public class RippleSource : Grid
    {
        public Brush Color
        {
            get { return (Brush)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }

        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register("Color", typeof(Brush), typeof(RippleSource), new PropertyMetadata(Brushes.White));

        public void SendRipple()
        {
            var rippleBox = this;

            double ExpandTime = 0.1;
            double FadeTime = 0.1;

            double o = 0.7;

            var targetWidth = rippleBox.ActualWidth;

            var ellipse = new Ellipse()
            {
                Fill = Color,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Opacity = o
            };
            ellipse.SetBinding(HeightProperty, new Binding("Width") { Source = ellipse });

            Storyboard storyboard = new Storyboard();

            var expand = new DoubleAnimation(10, targetWidth, new Duration(TimeSpan.FromSeconds(ExpandTime + FadeTime)));
            storyboard.Children.Add(expand);
            Storyboard.SetTarget(expand, ellipse);
            Storyboard.SetTargetProperty(expand, new PropertyPath(WidthProperty));

            var opacity = new DoubleAnimation(o, 0, new Duration(TimeSpan.FromSeconds(FadeTime)));
            opacity.BeginTime = TimeSpan.FromSeconds(ExpandTime);
            storyboard.Children.Add(opacity);
            Storyboard.SetTarget(opacity, ellipse);
            Storyboard.SetTargetProperty(opacity, new PropertyPath(Ellipse.OpacityProperty));

            rippleBox.Children.Add(ellipse);

            storyboard.Begin();

            var waitTime = ExpandTime + FadeTime;
            Task.Run(() =>
            {
                Thread.Sleep(TimeSpan.FromSeconds(waitTime));
                Dispatcher.Invoke(() =>
                {
                    //rippleBox.Children.Remove(ellipse);
                });
            });
        }
    }
}
