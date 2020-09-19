using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ZenithEngine.ModuleUI
{
    public abstract class BaseItem<C> : BaseElement<C>, ISerializableItem
        where C : Control
    {
        public string ItemName { get; }

        public BaseItem(string name, C control) : base(control)
        {
            ItemName = name;
        }

        public HorizontalAlignment HorizontalContentAlignment { get => Control.HorizontalContentAlignment; set => Control.HorizontalContentAlignment = value; }
        public VerticalAlignment VerticalContentAlignment { get => Control.VerticalContentAlignment; set => Control.VerticalContentAlignment = value; }
        public Brush Background { get => Control.Background; set => Control.Background = value; }
        public Brush Foreground { get => Control.Foreground; set => Control.Foreground = value; }

        public bool IsEnabled
        {
            get
            {
                bool e = false;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    e = Control.IsEnabled;
                });
                return e;
            }
            set
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Control.IsEnabled = value;
                });
            }
        }

        public abstract void Parse(string data);

        public abstract string Serialize();
    }

    public abstract class BaseItem<C, T> : BaseItem<C>
        where C : Control
    {
        public static implicit operator T(BaseItem<C, T> item) => item.Value;

        public BaseItem(string name, C control, T value) : this(name, control)
        {
            Value = value;
            ValueChanged += v =>
            {
                if (!Value.Equals(v)) Value = v;
            };
        }

        public BaseItem(string name, C control) : base(name, control)
        {
            Margin = new Thickness(0, 0, 10, 10);
        }

        T cachedValue;
        public T Value
        {
            get => cachedValue;
            set => SaveValue(value);
        }

        void SaveValue(T value)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (!ValueInternal.Equals(value))
                {
                    ValueInternal = value;
                    ValueChanged?.Invoke(value);
                }
            });
            cachedValue = value;
        }

        protected void UpdateValue()
        {
            Value = ValueInternal;
        }

        public abstract T ValueInternal { get; set; }
        public event Action<T> ValueChanged;
    }
}
