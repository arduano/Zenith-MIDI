using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ZenithEngine.ModuleUI
{
    public class UITextBox : BaseLabelledItem<TextBox, string>
    {
        public static implicit operator string(UITextBox val) => val.Value;

        public UITextBox(string name, object label, string value, int maxLength, int width) : base(name, label, new TextBox(), value)
        {
            Value = value;
            InnerControl.MaxLength = maxLength;
            InnerControl.TextChanged += (s, e) => UpdateValue();
            InnerControl.Width = width;
        }

        public override string ValueInternal { get => InnerControl.Text; set => InnerControl.Text = value; }

        public override void Parse(string data)
        {
            Value = data;
        }

        public override string Serialize()
        {
            return Value;
        }
    }
}
