using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace ZenithEngine.UI
{
    public class RippleEffectDecorator : ContentControl
    {
        new public double Opacity
        {
            get { return (double)GetValue(OpacityProperty); }
            set { SetValue(OpacityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Opacity.  This enables animation, styling, binding, etc...
        new public static readonly DependencyProperty OpacityProperty =
            DependencyProperty.Register("Opacity", typeof(double), typeof(RippleEffectDecorator), new PropertyMetadata(0.4));

        public double ExpandTime
        {
            get { return (double)GetValue(ExpandTimeProperty); }
            set { SetValue(ExpandTimeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ExpandTime.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ExpandTimeProperty =
            DependencyProperty.Register("ExpandTime", typeof(double), typeof(RippleEffectDecorator), new PropertyMetadata(0.4));

        public double FadeTime
        {
            get { return (double)GetValue(FadeTimeProperty); }
            set { SetValue(FadeTimeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FadeTime.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FadeTimeProperty =
            DependencyProperty.Register("FadeTime", typeof(double), typeof(RippleEffectDecorator), new PropertyMetadata(0.3));

        public RippleEffectDecorator()
        {
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var parentGrid = new Grid();
            var grid = new Grid();
            var content = new ContentControl();
            grid.Background = Brushes.Transparent;
            grid.ClipToBounds = true;
            parentGrid.Children.Add(grid);
            parentGrid.Children.Add(content);

            var c = Content;
            this.Content = parentGrid;
            content.Content = c;

            grid.SetBinding(WidthProperty, new Binding("ActualWidth") { Source = parentGrid });
            grid.SetBinding(HeightProperty, new Binding("ActualHeight") { Source = parentGrid });

            parentGrid.PreviewMouseDown += (sender, e) =>
            {
                var targetWidth = (Math.Max(ActualWidth, ActualHeight) * 2) / ExpandTime * (ExpandTime + FadeTime);
                var mousePosition = (e as MouseButtonEventArgs).GetPosition(this);
                var startMargin = new Thickness(mousePosition.X, mousePosition.Y, 0, 0);
                var endMargin = new Thickness(mousePosition.X - targetWidth / 2, mousePosition.Y - targetWidth / 2, 0, 0);

                var ellipse = new Ellipse()
                {
                    Fill = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Opacity = Opacity
                };
                ellipse.Margin = startMargin;
                ellipse.SetBinding(HeightProperty, new Binding("Width") { Source = ellipse });

                Storyboard storyboard = new Storyboard();

                var expand = new DoubleAnimation(0, targetWidth, new Duration(TimeSpan.FromSeconds(ExpandTime + FadeTime)));
                storyboard.Children.Add(expand);
                Storyboard.SetTarget(expand, ellipse);
                Storyboard.SetTargetProperty(expand, new PropertyPath(WidthProperty));

                var marginShrink = new ThicknessAnimation(startMargin, endMargin, new Duration(TimeSpan.FromSeconds(ExpandTime + FadeTime)));
                storyboard.Children.Add(marginShrink);
                Storyboard.SetTarget(marginShrink, ellipse);
                Storyboard.SetTargetProperty(marginShrink, new PropertyPath(MarginProperty));

                var opacity = new DoubleAnimation(Opacity, 0, new Duration(TimeSpan.FromSeconds(FadeTime)));
                opacity.BeginTime = TimeSpan.FromSeconds(ExpandTime);
                storyboard.Children.Add(opacity);
                Storyboard.SetTarget(opacity, ellipse);
                Storyboard.SetTargetProperty(opacity, new PropertyPath(Ellipse.OpacityProperty));

                grid.Children.Add(ellipse);

                storyboard.Begin();

                var waitTime = ExpandTime + FadeTime;
                Task.Run(() =>
                {
                    Thread.Sleep(TimeSpan.FromSeconds(waitTime));
                    Dispatcher.Invoke(() =>
                    {
                        grid.Children.Remove(ellipse);
                    });
                });
                e.Handled = false;
            };
        }
    }
}
