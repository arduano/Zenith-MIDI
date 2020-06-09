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

namespace ZenithEngine.ModuleUI
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class UIChild : Attribute
    {
        private readonly int order;
        public UIChild([CallerLineNumber]int order = 0)
        {
            this.order = order;
        }

        public int Order => order;
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
        public UIContainerData(UIElement[] elements, Dictionary<string, ISerializableItem> items, Dictionary<string, ISerializableContainer> containers)
        {
            Elements = elements;
            Items = items;
            Containers = containers;
        }

        public UIElement[] Elements { get; }
        public Dictionary<string, ISerializableItem> Items { get; }
        public Dictionary<string, ISerializableContainer> Containers { get; }
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

            var containers = new Dictionary<string, ISerializableContainer>();
            var items = new Dictionary<string, ISerializableItem>();

            var elements = selected.Select(m =>
            {
                UIElement element = null;
                if (m is PropertyInfo)
                {
                    element = (UIElement)(m as PropertyInfo).GetValue(item);
                }
                else if (m is FieldInfo)
                {
                    element = (UIElement)(m as FieldInfo).GetValue(item);
                }
                else
                {
                    throw new Exception("shouldnt reach here");
                }

                if (element is ISerializableContainer)
                {
                    containers.Add(m.Name, (ISerializableContainer)element);
                }
                else if (element is ISerializableItem)
                {
                    items.Add(m.Name, (ISerializableItem)element);
                }

                return element;
            }).ToArray();

            return new UIContainerData(elements, items, containers);
        }

        public static JObject SerializeContainer(UIContainerData container)
        {
            JToken test = "";

            var obj = new JObject();

            foreach (var item in container.Items)
            {
                obj.Add(item.Key, item.Value.Serialize());
            }

            foreach (var item in container.Containers)
            {
                obj.Add(item.Key, item.Value.Serialize());
            }

            return obj;
        }

        public static void ParseContainer(JObject data, UIContainerData container)
        {
            foreach (var item in data)
            {
                if (container.Containers.ContainsKey(item.Key))
                {
                    if (item.Value.Type != JTokenType.Object)
                    {
                        continue;
                    }
                    container.Containers[item.Key].Parse(item.Value as JObject);
                }
            }

            foreach (var item in data)
            {
                if (container.Containers.ContainsKey(item.Key))
                {
                    if (item.Value.Type != JTokenType.Object)
                    {
                        continue;
                    }
                    container.Containers[item.Key].Parse(item.Value as JObject);
                }
            }
        }

        public static void SyncValue<T>(IValueItem<T> item)
        {
            if (Thread.CurrentThread.ManagedThreadId != item.Dispatcher.Thread.ManagedThreadId)
            {
                item.Dispatcher.InvokeAsync(() => item.ValueInternal = item.Value).Wait();
            }
            else
            {
                item.ValueInternal = item.Value;
            }
        }

        public static void BindValue<T>(IValueItem<T> item)
        {
            item.ValueChanged += (s, e) =>
            {
                item.Value = item.ValueInternal;
            };
            item.Value = item.ValueInternal;
        }
    }
}
