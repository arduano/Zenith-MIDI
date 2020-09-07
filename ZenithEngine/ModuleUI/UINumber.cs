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
    public class UINumber : Docked<decimal>, IValueItem<decimal>
    {
        public static implicit operator decimal(UINumber val) => val.Value;
        public static implicit operator int(UINumber val) => (int)val.Value;
        public static implicit operator double(UINumber val) => (double)val.Value;
        public static implicit operator float(UINumber val) => (float)val.Value;

        NumberBox numberItem = new NumberBox() { MinWidth = 80 };

        public decimal ValueInternal
        {
            get => numberItem.Value;
            set { if (numberItem.Value != value) numberItem.Value = value; }
        }

        decimal cacheValue = 0;
        public override decimal Value
        {
            get => cacheValue;
            set
            {
                cacheValue = value;
                UITools.SyncValue(this);
            }
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
            Children.Add(labelItem);
            Children.Add(numberItem);
            SetDock(labelItem, Dock.Left);
            SetDock(numberItem, Dock.Left);

            Margin = new Thickness(0, 0, 10, 10);

            numberItem.ValueChanged += (s, e) =>
            {
                ValueChanged?.Invoke(this, e.NewValue);
            };

            UITools.BindValue(this);
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
