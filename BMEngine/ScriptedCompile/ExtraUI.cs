using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptedEngine
{
    public abstract class UISetting { }

    public class UITabs : UISetting
    {
        public Dictionary<string, IEnumerable<UISetting>> Tabs { get; set; } = new Dictionary<string, IEnumerable<UISetting>>();

        public UITabs() { }
        public UITabs(Dictionary<string, IEnumerable<UISetting>> tabs) { Tabs = tabs; }
    }

    public class UILabel : UISetting
    {
        public double FontSize { get; } = 16;
        public string Text { get; } = "";

        public UILabel(string text) { Text = text; }
        public UILabel(string text, double fontSize) { Text = text; FontSize = fontSize; }
    }

    public class UINumber : UISetting
    {
        public UINumber(string text, double value, double minimum, double maximum, int decialPoints)
        {
            Text = text;
            Value = value;
            Minimum = minimum;
            Maximum = maximum;
            DecialPoints = decialPoints;
        }

        public UINumber(string text, double value, double minimum, double maximum, int decialPoints, double step) : this(text, value, minimum, maximum, decialPoints)
        {
            Step = step;
        }

        public string Text { get; }
        public double Value { get; set; }
        public double Minimum { get; }
        public double Maximum { get; }
        public int DecialPoints { get; }
        public double Step { get; } = 1;
    }

    public class UINumberSlider : UISetting
    {
        public UINumberSlider(string text, double value, double minimum, double maximum, double trueMinimum, double trueMaximum, int decialPoints)
        {
            Text = text;
            Value = value;
            Minimum = minimum;
            Maximum = maximum;
            TrueMinimum = trueMinimum;
            TrueMaximum = trueMaximum;
            DecialPoints = decialPoints;
        }

        public UINumberSlider(string text, double value, double minimum, double maximum, double trueMinimum, double trueMaximum, int decialPoints, bool logarithmic) : this(text, value, minimum, maximum, trueMinimum, trueMaximum, decialPoints)
        {
            Logarithmic = logarithmic;
        }

        public string Text { get; }
        public double Value { get; set; }
        public double Minimum { get; }
        public double Maximum { get; }
        public double TrueMinimum { get; }
        public double TrueMaximum { get; }
        public int DecialPoints { get; }
        public bool Logarithmic { get; } = false;
    }

    public class UIDropdown : UISetting
    {
        public UIDropdown(string text, string[] options)
        {
            Text = text;
            Options = options;
            Index = 0;
            Value = options[0];
        }

        public UIDropdown(string text, int index, string[] options)
        {
            Text = text;
            Index = index;
            Value = options[index];
            Options = options;
        }

        public string Text { get; }
        public string Value { get; set; }
        public int Index { get; set; }
        public string[] Options { get; }
    }
}
