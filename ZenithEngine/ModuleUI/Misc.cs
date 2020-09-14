using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
using System.Threading;
using System.Windows.Controls;

namespace ZenithEngine.ModuleUI
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class UIChild : Attribute
    {
        private readonly int order;
        private readonly string name;
        public UIChild([CallerMemberName] string name = null, [CallerLineNumber]int order = 0)
        {
            this.order = order;
            this.name = name;
        }

        public int Order => order;
        public string Name => name;
    }

    public interface ISerializableContainer
    {
        void Parse(JObject data);
        JObject Serialize();
    }

    public interface ISerializableItem
    {
        void Parse(string data);
        string Serialize();
        string ItemName { get; }
    }

    public interface IControl
    {
        UIElement Control { get; }
    }

    public interface IValueItem<T>
    {
        event EventHandler<T> ValueChanged;
        Dispatcher Dispatcher { get; }
        T Value { get; set; }
        T ValueInternal { get; set; }
    }

    public class UIContainerData
    {
        public UIContainerData(
            UIElement[] elements, 
            Dictionary<string, ISerializableItem> dataItems,
            ISerializableContainer[] containers)
        {
            Elements = elements;
            DataItems = dataItems;
            Containers = containers;
        }

        public UIElement[] Elements { get; }
        public Dictionary<string, ISerializableItem> DataItems { get; }
        public ISerializableContainer[] Containers { get; }
    }

    public static class UITools
    {
        public static UIContainerData GetChildren(object item)
        {
            var t = item.GetType();
            List<MemberInfo> members = new List<MemberInfo>();
            members.AddRange(t.GetFields());
            members.AddRange(t.GetProperties());
            var selected = from property in members
                           where Attribute.IsDefined(property, typeof(UIChild))
                           orderby ((UIChild)property
                                     .GetCustomAttributes(typeof(UIChild), false)
                                     .Single()).Order
                           select property;

            var elements = new List<UIElement>();
            var dataItems = new Dictionary<string, ISerializableItem>();
            var containers = new List<ISerializableContainer>();

            foreach(var m in selected)
            {
                object child = null;
                if (m is PropertyInfo)
                {
                    child = (m as PropertyInfo).GetValue(item);
                }
                else if (m is FieldInfo)
                {
                    child = (m as FieldInfo).GetValue(item);
                }
                else
                {
                    throw new Exception("shouldnt reach here");
                }

                if (child is UIElement)
                {
                    elements.Add((UIElement)child);
                }
                if(child is IControl)
                {
                    elements.Add(((IControl)child).Control);
                }
                if (child is BaseElement)
                {
                    if (child is ISerializableContainer)
                    {
                        var i = (ISerializableContainer)child;
                        containers.Add(i);
                    }
                    if (child is ISerializableItem)
                    {
                        var i = (ISerializableItem)child;
                        dataItems.Add(i.ItemName, i);
                    }
                }
            }

            return new UIContainerData(elements.ToArray(), dataItems, containers.ToArray());
        }
    }
}
