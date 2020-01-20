using System;
using System.Collections.Generic;
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
    /// Interaction logic for BetterCheckbox.xaml
    /// </summary>
    public partial class BetterCheckbox : UserControl
    {
        public bool IsChecked
        {
            get { return (bool)GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, value); }
        }

        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register("IsChecked", typeof(bool), typeof(BetterCheckbox), new PropertyMetadata(false, (s, e) => ((BetterCheckbox)s).OnCheckChanged()));


        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(BetterCheckbox), new PropertyMetadata(""));


        public static readonly RoutedEvent CheckToggledEvent = EventManager.RegisterRoutedEvent(
            "RadioChecked", RoutingStrategy.Bubble,
            typeof(RoutedPropertyChangedEventHandler<bool>), typeof(BetterCheckbox));

        public event RoutedPropertyChangedEventHandler<bool> CheckToggled
        {
            add { AddHandler(CheckToggledEvent, value); }
            remove { RemoveHandler(CheckToggledEvent, value); }
        }


        void OnCheckChanged()
        {
            RaiseEvent(new RoutedPropertyChangedEventArgs<bool>(!IsChecked, IsChecked, CheckToggledEvent));
        }


        public BetterCheckbox()
        {
            InitializeComponent();
            new InplaceConverter(new[] {
                new BBinding(IsCheckedProperty, this)
            }, v => (bool)v[0] ? Visibility.Visible : Visibility.Hidden)
                .Set(checkedBox, VisibilityProperty);
        }

        private void DockPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            IsChecked = !IsChecked;


            double ExpandTime = 0.1;
            double FadeTime = 0.1;

            double o = 0.7;

            var targetWidth = rippleBox.ActualWidth;

            var ellipse = new Ellipse()
            {
                Fill = (Brush)Resources["PrimaryBrush"],
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
                    rippleBox.Children.Remove(ellipse);
                });
            });
        }
    }
}
