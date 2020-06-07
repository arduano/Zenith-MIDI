using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace ZenithEngine.ModuleUI
{
    public interface IUIItem
    {
    }

    public interface IFieldItem<T> : IUIItem
    {
        T Value { get; set; }
        event EventHandler<T> ValueChanged;
        string Serialize();
        void Parse(string value);
        object Label { get; set; }
    }

    public interface IContainerItem<T> : IUIItem
    {
        T Children { get; }
        T items { get; }
        JObject Serialize();
        void Parse(JObject container);
    }

    static class UIReflect
    {
        static Type icontainer = typeof(IContainerItem<object>);
        static Type ifield = typeof(IContainerItem<object>);

        static JObject FetchChildren<T>(IContainerItem<T> container)
        {
            JObject data = new JObject();

            var generic = typeof(IContainerItem<T>).GenericTypeArguments[0];
            foreach (MemberInfo f in generic.GetFields().Concat<MemberInfo>(generic.GetProperties()))
            {
                
            }

            return null;
        }
    }
}
