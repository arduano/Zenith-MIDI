using Newtonsoft.Json.Linq;
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
    public class BaseContainerBasic<C> : BaseElement<C>, ISerializableContainer
        where C : FrameworkElement
    {
        protected UIContainerData ChildData { get; }

        public BaseContainerBasic(C control) : base(control)
        {
            ChildData = UITools.GetChildren(this);
            Margin = new Thickness(10);
        }

        public void Parse(JObject data)
        {
            foreach (var d in data)
            {
                if (d.Value.Type != JTokenType.String) continue;
                if (ChildData.DataItems.ContainsKey(d.Key))
                {
                    var item = ChildData.DataItems[d.Key] as ISerializableItem;
                    if (item != null) item.Parse((string)d.Value);
                }
            }
        }

        public JObject Serialize()
        {
            var obj = new JObject();
            foreach (var c in ChildData.DataItems.Values)
            {
                obj.Add(c.Serialize());
            }

            foreach (var c in ChildData.Containers)
            {
                var s = c.Serialize();
                foreach (var vk in s)
                {
                    if (!obj.ContainsKey(vk.Key))
                        obj.Add(vk.Key, vk.Value);
                }
            }

            return obj;
        }
    }

    public class BaseContainer<C> : BaseContainerBasic<C>
        where C : Panel
    {
        public BaseContainer(C control) : base(control)
        { }

        public Brush Background { get => Control.Background; set => Control.Background = value; }
    }
}
