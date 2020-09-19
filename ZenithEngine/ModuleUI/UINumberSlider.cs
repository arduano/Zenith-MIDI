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
    public class UINumberSlider : BaseLabelledItem<ValueSlider, double>
    {
        public UINumberSlider(string name, object label, double value, double minimum, double maximum, decimal trueMinimum, decimal trueMaximum, bool logarithmic = false)
            : base(name, label, new ValueSlider(), value)
        {
            Minimum = minimum;
            Maximum = maximum;
            TrueMin = trueMinimum;
            TrueMax = trueMaximum;
            Value = value;
            SliderWidth = 500;
            InnerControl.ValueChanged += (s, e) => UpdateValue();
        }

        public UINumberSlider(string name, object label, double value, double minimum, double maximum)
            : this(name, label, value, minimum, maximum, (decimal)minimum, (decimal)maximum, false)
        { }

        public double NumWidth { get => InnerControl.Width; set => InnerControl.Width = value; }
        public double NumHeight { get => InnerControl.Height; set => InnerControl.Height = value; }
        public double NumMinWidth { get => InnerControl.MinWidth; set => InnerControl.MinWidth = value; }
        public double NumMinHeight { get => InnerControl.MinHeight; set => InnerControl.MinHeight = value; }
        public double NumMaxWidth { get => InnerControl.MaxWidth; set => InnerControl.MaxWidth = value; }
        public double NumMaxHeight { get => InnerControl.MaxHeight; set => InnerControl.MaxHeight = value; }

        public double Minimum { get => InnerControl.Minimum; set => InnerControl.Minimum = value; }
        public double Maximum { get => InnerControl.Maximum; set => InnerControl.Maximum = value; }
        public decimal TrueMin { get => InnerControl.TrueMin; set => InnerControl.TrueMin = value; }
        public decimal TrueMax { get => InnerControl.TrueMax; set => InnerControl.TrueMax = value; }
        public int DecimalPoints { get => InnerControl.DecimalPoints; set => InnerControl.DecimalPoints = value; }
        public decimal Step { get => InnerControl.Step; set => InnerControl.Step = value; }

        public double SliderWidth { get => InnerControl.SliderWidth; set => InnerControl.SliderWidth = value; }
        public double MinNUDWidth { get => InnerControl.MinNUDWidth; set => InnerControl.MinNUDWidth = value; }

        bool logarithmic = false;
        public bool Logarithmic
        {
            get => logarithmic;
            set
            {
                if (logarithmic == value) return;
                logarithmic = value;
                if (logarithmic)
                {
                    InnerControl.NudToSlider = v => Math.Log(v, 2);
                    InnerControl.SliderToNud = v => Math.Pow(2, v);
                }
                else
                {
                    InnerControl.NudToSlider = v => v;
                    InnerControl.SliderToNud = v => v;
                }
            }
        }

        public override double ValueInternal { 
            get => InnerControl.Value; 
            set => InnerControl.Value = value;
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
