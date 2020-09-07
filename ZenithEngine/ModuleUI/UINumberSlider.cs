using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using ZenithEngine.UI;
using Slider = ZenithEngine.UI.Slider;

namespace ZenithEngine.ModuleUI
{
    public class UINumberSlider : Docked<double>, IValueItem<double>
    {
        public static implicit operator double(UINumberSlider val) => val.Value;
        public static implicit operator float(UINumberSlider val) => (float)val.Value;

        NumberBox numberItem = new NumberBox() { MinWidth = 80 };
        Slider sliderItem = new Slider();

        public double ValueInternal
        {
            get => (double)numberItem.Value;
            set { if (numberItem.Value != (decimal)value) numberItem.Value = (decimal)value; }
        }

        double cacheValue = 0;
        public override double Value
        {
            get => cacheValue;
            set
            {
                cacheValue = value;
                UITools.SyncValue(this);
            }
        }

        public override event EventHandler<double> ValueChanged;

        public double MinNumberWidth
        {
            get => numberItem.MinWidth;
            set => numberItem.MinWidth = value;
        }

        public double SliderWidth
        {
            get => sliderItem.Width;
            set => sliderItem.Width = value;
        }

        public double SliderMax
        {
            get => unlog(sliderItem.Maximum);
            set => sliderItem.Maximum = log(value);
        }

        public double SliderMin
        {
            get => unlog(sliderItem.Minimum);
            set => sliderItem.Minimum = log(value);
        }

        public double Max
        {
            get => (double)numberItem.Maximum;
            set => numberItem.Maximum = (decimal)value;
        }

        public double Min
        {
            get => (double)numberItem.Minimum;
            set => numberItem.Minimum = (decimal)value;
        }

        public double Step
        {
            get => (double)numberItem.Step;
            set => numberItem.Step = (decimal)value;
        }

        public int DecimalPoints
        {
            get => numberItem.DecimalPoints;
            set => numberItem.DecimalPoints = value;
        }

        bool logarithmic = false;
        public bool Logarithmic
        {
            get => logarithmic;
            set
            {
                logarithmic = value;
                if (log(SliderMax) != sliderItem.Maximum) sliderItem.Maximum = log(SliderMax);
                if (log(SliderMin) != sliderItem.Minimum) sliderItem.Minimum = log(SliderMin);
            }
        }

        double log(double v) => logarithmic ? Math.Log(v, 2) : v;
        double unlog(double v) => logarithmic ? Math.Pow(2, v) : v;

        public UINumberSlider()
        {
            HorizontalAlignment = HorizontalAlignment.Left;
            Children.Add(labelItem);
            Children.Add(sliderItem);
            Children.Add(numberItem);
            SetDock(labelItem, Dock.Left);
            SetDock(sliderItem, Dock.Left);
            SetDock(numberItem, Dock.Left);

            Margin = new Thickness(0, 0, 10, 10);
            sliderItem.Margin = new Thickness(0, 4, 0, 0);

            numberItem.ValueChanged += (s, e) =>
            {
                sliderItem.Value = log((double)e.NewValue);
                ValueChanged?.Invoke(this, (double)e.NewValue);
            };

            sliderItem.UserValueChanged += (s, e) =>
            {
                numberItem.Value = (decimal)unlog(sliderItem.Value);
            };

            DecimalPoints = 2;
            Step = 1;
            Max = 10000;
            SliderMax = 1000;
            Min = 0;
            SliderMin = 0;
            Logarithmic = false;

            MinNumberWidth = 80;
            SliderWidth = 400;

            UITools.BindValue(this);
        }

        public override void Parse(string value)
        {
            try
            {
                Value = Convert.ToDouble(value);
            }
            catch { }
        }

        public override string Serialize()
        {
            return Value.ToString();
        }
    }
}
