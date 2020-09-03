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

    public class InplaceConverter<T1, R1> : InplaceConverter
    {
        public InplaceConverter(Binding binding1, Func<T1, R1> func)
            : base(new[] { binding1 }, (d) => func((T1)d[0]))
        { }
    }

    public class InplaceConverter<T1, T2, R1> : InplaceConverter
    {
        public InplaceConverter(Binding binding1, Binding binding2, Func<T1, T2, R1> func)
            : base(new[] { binding1, binding2 }, (d) => func((T1)d[0], (T2)d[1]))
        { }
    }

    public class InplaceConverter<T1, T2, T3, R1> : InplaceConverter
    {
        public InplaceConverter(Binding binding1, Binding binding2, Binding binding3, Func<T1, T2, T3, R1> func)
            : base(new[] { binding1, binding2, binding3 }, (d) => func((T1)d[0], (T2)d[1], (T3)d[2]))
        { }
    }

    public class InplaceConverter<T1, T2, T3, T4, R1> : InplaceConverter
    {
        public InplaceConverter(Binding binding1, Binding binding2, Binding binding3, Binding binding4, Func<T1, T2, T3, T4, R1> func)
            : base(new[] { binding1, binding2, binding3, binding4 }, (d) => func((T1)d[0], (T2)d[1], (T3)d[2], (T4)d[3]))
        { }
    }
}
