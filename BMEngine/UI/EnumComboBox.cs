using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ZenithEngine.UI
{
    public class EnumComboBox : ComboBox
    {
        public Type Enum
        {
            get { return (Type)GetValue(EnumProperty); }
            set { SetValue(EnumProperty, value); }
        }

        public static readonly DependencyProperty EnumProperty =
            DependencyProperty.Register("Enum", typeof(Type), typeof(EnumComboBox), new PropertyMetadata(null));

        public object Selected
        {
            get { return (object)GetValue(SelectedProperty); }
            set { SetValue(SelectedProperty, value); }
        }

        public static readonly DependencyProperty SelectedProperty =
            DependencyProperty.Register("Selected", typeof(object), typeof(EnumComboBox), new PropertyMetadata(null, (s, e) =>
            {
                (s as EnumComboBox)?.SelectFromEnum();
            }));

        public EnumComboBox()
        {
            SelectionChanged += EnumComboBox_SelectionChanged;
        }

        void SelectFromEnum()
        {
            var item = Items.OfType<EnumComboBoxItem>().Where(i => i.EnumValue.Equals(Selected)).FirstOrDefault();
            SelectedItem = item;
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            SelectFromEnum();
        }

        private void EnumComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var val = SelectedItem as EnumComboBoxItem;
            if(val != null)
            {
                Selected = val.EnumValue;
            }
        }
    }

    public class EnumComboBoxItem : ComboBoxItem
    {
        public object EnumValue
        {
            get { return (object)GetValue(EnumValueProperty); }
            set { SetValue(EnumValueProperty, value); }
        }

        public static readonly DependencyProperty EnumValueProperty =
            DependencyProperty.Register("EnumValue", typeof(object), typeof(EnumComboBoxItem), new PropertyMetadata(null));
    }
}
