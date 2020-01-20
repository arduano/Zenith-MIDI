using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Reflection;

namespace ZenithEngine.UI
{
    public class InplaceConverter : IMultiValueConverter
    {
        Func<object[], object> func;
        Binding[] bindings;

        public InplaceConverter(Binding[] bindings, Func<object[], object> func)
        {
            this.bindings = bindings;
            this.func = func;
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return func(values);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public void Set(FrameworkElement o, DependencyProperty p)
        {
            var b = new MultiBinding();
            b.Converter = this;
            foreach (var _b in bindings) b.Bindings.Add(_b);
            o.SetBinding(p, b);
        }
    }
}
