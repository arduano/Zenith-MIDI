using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using ZenithEngine.UI;
using System.Windows;

namespace ZenithEngine.ModuleUI
{
    public class UINumber : BaseLabelledItem<NumberBox, decimal>
    {
        public static implicit operator int(UINumber val) => (int)val.Value;
        public static implicit operator double(UINumber val) => (double)val.Value;

        public UINumber(string name, object label, decimal value, decimal minimum, decimal maximum, int decimalsPoints = 0) : base(name, label, new NumberBox(), value)
        {
            Minimum = minimum;
            Maximum = maximum;
            DecimalPoints = decimalsPoints;
            Value = value;
            MinNUDWidth = 80;
            InnerControl.ValueChanged += (s, e) => UpdateValue();
        }

        public UINumber(string name, decimal value, decimal minimum, decimal maximum, int decimalsPoints = 0)
            : this(name, null, value, minimum, maximum, decimalsPoints)
        { }

        public double NumWidth { get => InnerControl.Width; set => InnerControl.Width = value; }
        public double NumHeight { get => InnerControl.Height; set => InnerControl.Height = value; }
        public double NumMinWidth { get => InnerControl.MinWidth; set => InnerControl.MinWidth = value; }
        public double NumMinHeight { get => InnerControl.MinHeight; set => InnerControl.MinHeight = value; }
        public double NumMaxWidth { get => InnerControl.MaxWidth; set => InnerControl.MaxWidth = value; }
        public double NumMaxHeight { get => InnerControl.MaxHeight; set => InnerControl.MaxHeight = value; }

        public decimal Minimum { get => InnerControl.Minimum; set => InnerControl.Minimum = value; }
        public decimal Maximum { get => InnerControl.Maximum; set => InnerControl.Maximum = value; }
        public int DecimalPoints { get => InnerControl.DecimalPoints; set => InnerControl.DecimalPoints = value; }
        public decimal? Step { get => InnerControl.Step; set => InnerControl.Step = value; }
        public double MinNUDWidth { get => InnerControl.MinWidth; set => InnerControl.MinWidth = value; }

        public override decimal ValueInternal { get => InnerControl.Value; set => InnerControl.Value = value; }

        public override void Parse(string value)
        {
            try
            {
                Value = Convert.ToDecimal(value);
            }
            catch { }
        }

        public override string Serialize()
        {
            return Value.ToString();
        }
    }
}
