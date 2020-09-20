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
    /// Interaction logic for ValueSlider.xaml
    /// </summary>
    public partial class ValueSlider : UserControl
    {
        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(double), typeof(ValueSlider), new PropertyMetadata(0.0, (s, e) => (s as ValueSlider).OnSliderMetaChange(e)));


        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(double), typeof(ValueSlider), new PropertyMetadata(1.0, (s, e) => (s as ValueSlider).OnSliderMetaChange(e)));


        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(double), typeof(ValueSlider), new PropertyMetadata(0.0, (s, e) => (s as ValueSlider).OnValueChange(e)));


        public int DecimalPoints
        {
            get { return (int)GetValue(DecimalPointsProperty); }
            set { SetValue(DecimalPointsProperty, value); }
        }

        public static readonly DependencyProperty DecimalPointsProperty =
            DependencyProperty.Register("DecimalPoints", typeof(int), typeof(ValueSlider), new PropertyMetadata(2));


        public decimal TrueMin
        {
            get { return (decimal)GetValue(TrueMinProperty); }
            set { SetValue(TrueMinProperty, value); }
        }

        public static readonly DependencyProperty TrueMinProperty =
            DependencyProperty.Register("TrueMin", typeof(decimal), typeof(ValueSlider), new PropertyMetadata((decimal)0.0));


        public decimal TrueMax
        {
            get { return (decimal)GetValue(TrueMaxProperty); }
            set { SetValue(TrueMaxProperty, value); }
        }

        public static readonly DependencyProperty TrueMaxProperty =
            DependencyProperty.Register("TrueMax", typeof(decimal), typeof(ValueSlider), new PropertyMetadata((decimal)1.0d));

        public decimal? Step
        { get => (decimal?)GetValue(StepProperty); set => SetValue(StepProperty, value); }
        public static readonly DependencyProperty StepProperty = DependencyProperty.Register("Step", typeof(decimal?), typeof(ValueSlider), new PropertyMetadata(null));


        public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
                "ValueChanged", RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<double>), typeof(ValueSlider));


        public double SliderWidth
        {
            get { return (double)GetValue(SliderWidthProperty); }
            set { SetValue(SliderWidthProperty, value); }
        }

        public static readonly DependencyProperty SliderWidthProperty =
            DependencyProperty.Register("SliderWidth", typeof(double), typeof(ValueSlider), new PropertyMetadata(double.NaN));


        public double MinNUDWidth
        {
            get { return (double)GetValue(MinNUDWidthProperty); }
            set { SetValue(MinNUDWidthProperty, value); }
        }

        public static readonly DependencyProperty MinNUDWidthProperty =
            DependencyProperty.Register("MinNUDWidth", typeof(double), typeof(ValueSlider), new PropertyMetadata(80.0));


        public Func<double, double> SliderToNud
        {
            get { return (Func<double, double>)GetValue(SliderToNudProperty); }
            set { SetValue(SliderToNudProperty, value); }
        }

        public static readonly DependencyProperty SliderToNudProperty =
            DependencyProperty.Register("SliderToNud", typeof(Func<double, double>), typeof(ValueSlider), new PropertyMetadata((Func<double, double>)(a => a), (s, e) => (s as ValueSlider).OnSliderMetaChange(e)));


        public Func<double, double> NudToSlider
        {
            get { return (Func<double, double>)GetValue(NudToSliderProperty); }
            set { SetValue(NudToSliderProperty, value); }
        }   

        public static readonly DependencyProperty NudToSliderProperty =
            DependencyProperty.Register("NudToSlider", typeof(Func<double, double>), typeof(ValueSlider), new PropertyMetadata((Func<double, double>)(a => a), (s, e) => (s as ValueSlider).OnSliderMetaChange(e)));


        public event RoutedPropertyChangedEventHandler<double> ValueChanged
        {
            add { AddHandler(ValueChangedEvent, value); }
            remove { RemoveHandler(ValueChangedEvent, value); }
        }

        void OnValueChange(DependencyPropertyChangedEventArgs e)
        {
            if (!ignoreslider) slider.Value = NudToSlider(Value);
            if (!ignorevalue) updown.Value = (decimal)Value;
            ignoreslider = false;
            ignorevalue = false;
            RaiseEvent(new RoutedPropertyChangedEventArgs<double>((double)e.OldValue, (double)e.NewValue, ValueChangedEvent));
        }

        void OnSliderMetaChange(DependencyPropertyChangedEventArgs e)
        {
            slider.Minimum = NudToSlider(Minimum);
            slider.Maximum = NudToSlider(Maximum);
            slider.Value = NudToSlider(Value);
        }

        public ValueSlider()
        {
            InitializeComponent();

            FocusVisualStyle = null;

            slider.SetBinding(Slider.WidthProperty, new Binding("SliderWidth") { Source = this });
            slider.SetBinding(Slider.MaximumProperty, new Binding("Maximum") { Source = this });
            slider.SetBinding(Slider.MinimumProperty, new Binding("Minimum") { Source = this });
            updown.SetBinding(NumberBox.MinimumProperty, new Binding("TrueMin") { Source = this });
            updown.SetBinding(NumberBox.MaximumProperty, new Binding("TrueMax") { Source = this });
            updown.SetBinding(NumberBox.DecimalPointsProperty, new Binding("DecimalPoints") { Source = this });
            updown.SetBinding(NumberBox.StepProperty, new Binding("Step") { Source = this });
            updown.SetBinding(NumberBox.MinWidthProperty, new Binding("MinNUDWidth") { Source = this });
        }

        bool ignoreslider = false;
        bool ignorevalue = false;

        private void Slider_ValueChanged(object sender, double e)
        {
            if (IsInitialized)
            {
                ignoreslider = true;
                Value = SliderToNud(slider.Value);
            }
        }

        private void Updown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (IsInitialized)
            {
                if (!ignorevalue)
                {
                    ignorevalue = true;
                    Value = (double)updown.Value;
                }
                ignorevalue = false;
            }
        }
    }
}
