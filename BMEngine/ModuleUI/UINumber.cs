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
    public class UINumber : Docked<decimal>
    {
        NumberSelect numberItem = new NumberSelect() { MinWidth = 80 };

        public override decimal Value
        {
            get => numberItem.Value;
            set { if (numberItem.Value != value) numberItem.Value = value; }
        }

        public override event EventHandler<decimal> ValueChanged;

        public double MinNumberWidth
        {
            get => numberItem.MinWidth;
            set => numberItem.MinWidth = value;
        }

        public decimal Max
        {
            get => numberItem.Maximum;
            set => numberItem.Maximum = value;
        }

        public decimal Min
        {
            get => numberItem.Minimum;
            set => numberItem.Minimum = value;
        }

        public decimal Step
        {
            get => numberItem.Step;
            set => numberItem.Step = value;
        }

        public int DecimalPoints
        {
            get => numberItem.DecimalPoints;
            set => numberItem.DecimalPoints = value;
        }

        public UINumber()
        {
            HorizontalAlignment = HorizontalAlignment.Left;
            Children.Add(labelItem);
            Children.Add(numberItem);
            SetDock(labelItem, Dock.Left);
            SetDock(numberItem, Dock.Left);

            numberItem.ValueChanged += (s, e) =>
            {
                ValueChanged?.Invoke(this, e.NewValue);
            };
        }

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
