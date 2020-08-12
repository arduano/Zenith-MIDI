using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ZenithEngine.UI
{
    public class NumberBox : Control
    {
        public decimal Value
        { get => (decimal)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(decimal), typeof(NumberBox), new PropertyMetadata((decimal)0, new PropertyChangedCallback(OnPropertyChange)));
        public int DecimalPoints
        { get => (int)GetValue(DecimalPointsProperty); set => SetValue(DecimalPointsProperty, value); }
        public static readonly DependencyProperty DecimalPointsProperty = DependencyProperty.Register("DecimalPoints", typeof(int), typeof(NumberBox), new PropertyMetadata((int)0, new PropertyChangedCallback(OnPropertyChange)));
        public decimal Minimum
        { get => (decimal)GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register("Minimum", typeof(decimal), typeof(NumberBox), new PropertyMetadata((decimal)0, new PropertyChangedCallback(OnPropertyChange)));
        public decimal Maximum
        { get => (decimal)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register("Maximum", typeof(decimal), typeof(NumberBox), new PropertyMetadata((decimal)1000, new PropertyChangedCallback(OnPropertyChange)));
        public decimal Step
        { get => (decimal)GetValue(StepProperty); set => SetValue(StepProperty, value); }
        public static readonly DependencyProperty StepProperty = DependencyProperty.Register("Step", typeof(decimal), typeof(NumberBox), new PropertyMetadata((decimal)1));

        public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
            "ValueChanged", RoutingStrategy.Bubble,
            typeof(RoutedPropertyChangedEventHandler<decimal>), typeof(NumberBox));

        public event RoutedPropertyChangedEventHandler<decimal> ValueChanged
        {
            add { AddHandler(ValueChangedEvent, value); }
            remove { RemoveHandler(ValueChangedEvent, value); }
        }

        private static void OnPropertyChange(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((NumberBox)sender).UpdateValue();
        }

        TextBox textBox;
        Button upArrow;
        Button downArrow;

        public bool TextFocused => textBox?.IsFocused ?? false;

        string prevText = "";

        public NumberBox() : base()
        {
            KeyDown += UserControl_KeyDown;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (Template != null)
            {
                textBox = Template.FindName("PART_textbox", this) as TextBox;
                upArrow = Template.FindName("PART_upArrow", this) as Button;
                downArrow = Template.FindName("PART_downArrow", this) as Button;

                upArrow.SetBinding(IsEnabledProperty, new BBinding(IsEnabledProperty, this));
                downArrow.SetBinding(IsEnabledProperty, new BBinding(IsEnabledProperty, this));
                textBox.SetBinding(IsEnabledProperty, new BBinding(IsEnabledProperty, this));

                textBox.TextChanged += TextBox_TextChanged;
                upArrow.Click += UpButton_Click;
                downArrow.Click += DownButton_Click;

                prevText = Value.ToString();
                textBox.Text = prevText;

                UpdateValue();
            }
            else
            {
                if (textBox != null) textBox.TextChanged -= TextBox_TextChanged;
                if (upArrow != null) upArrow.Click -= UpButton_Click;
                if (downArrow != null) downArrow.Click -= DownButton_Click;

                textBox = null;
                upArrow = null;
                downArrow = null;
            }
        }

        bool ignoreChange = false;
        void UpdateValue()
        {
            if (!ignoreChange)
            {
                try
                {
                    decimal old = Value;
                    decimal d = Decimal.Round(old, DecimalPoints);
                    if (d < Minimum) d = Minimum;
                    if (d > Maximum) d = Maximum;
                    if (d != old)
                    {
                        Value = d;
                    }
                    try
                    {
                        RaiseEvent(new RoutedPropertyChangedEventArgs<decimal>(old, d, ValueChangedEvent));
                    }
                    catch { }
                }
                catch { }
                if (textBox != null)
                {
                    textBox.Text = Value.ToString();
                }
            }
            ignoreChange = false;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                decimal _d = Convert.ToDecimal(textBox.Text);
                decimal d = Decimal.Round(_d, DecimalPoints);
                if (d < Minimum) d = Minimum;
                if (d > Maximum) d = Maximum;
                else
                {
                    var old = Value;
                    if (d != old)
                    {
                        ignoreChange = true;
                        Value = d;
                        try
                        {
                            RaiseEvent(new RoutedPropertyChangedEventArgs<decimal>(old, d, ValueChangedEvent));
                        }
                        catch { }
                    }
                }
            }
            catch
            {
                if (textBox.Text != "")
                    textBox.Text = prevText;
            }
            prevText = textBox.Text;
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            CheckAndSave();
        }

        void CheckAndSave()
        {
            try
            {
                decimal _d = Convert.ToDecimal(textBox.Text);
                decimal d = Decimal.Round(_d, DecimalPoints);
                if (d < Minimum) d = Minimum;
                if (d > Maximum) d = Maximum;
                var old = Value;
                if (d != old)
                {
                    Value = d;
                    try
                    {
                        RaiseEvent(new RoutedPropertyChangedEventArgs<decimal>(old, d, ValueChangedEvent));
                    }
                    catch { }
                }
            }
            catch { }
            textBox.Text = Value.ToString();
        }

        private void TextBox_TextInput(object sender, TextCompositionEventArgs e)
        {

        }

        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            var d = Value + Step;
            if (d < Minimum) d = Minimum;
            if (d > Maximum) d = Maximum;
            var old = Value;
            Value = d;
            textBox.Text = Value.ToString();
            if (old != d)
                RaiseEvent(new RoutedPropertyChangedEventArgs<decimal>(old, d, ValueChangedEvent));
        }

        private void DownButton_Click(object sender, RoutedEventArgs e)
        {
            var d = Value - Step;
            if (d < Minimum) d = Minimum;
            if (d > Maximum) d = Maximum;
            var old = Value;
            Value = d;
            textBox.Text = Value.ToString();
            if (old != d)
                RaiseEvent(new RoutedPropertyChangedEventArgs<decimal>(old, d, ValueChangedEvent));
        }

        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CheckAndSave();
                e.Handled = true;
                Keyboard.ClearFocus();

                FrameworkElement parent = (FrameworkElement)textBox.Parent;
                while (parent != null && parent is IInputElement && !((IInputElement)parent).Focusable)
                {
                    parent = (FrameworkElement)parent.Parent;
                }

                DependencyObject scope = FocusManager.GetFocusScope(textBox);
                FocusManager.SetFocusedElement(scope, parent as IInputElement);
            }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
        }
    }
}
