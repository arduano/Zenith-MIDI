using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using ZenithEngine.DXHelper;
using ZenithEngine.IO;

namespace TexturedRender
{
    public enum SwitchItemType
    {
        Option,
        Text
    }

    public class SwitchItem : INotifyPropertyChanged
    {
        public SwitchItem() { }

        public SwitchItem(string name, string[] values)
        {
            Name = name;
            SelectedValue = values.First();
            Values = values;
            Type = SwitchItemType.Option;
        }

        public SwitchItem(string text)
        {
            Text = text;
            Type = SwitchItemType.Text;
        }

        public string Text { get; set; }
        public SwitchItemType? Type { get; set; }
        public string Name { get; set; }
        public string SelectedValue { get; set; }
        public string[] Values { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class LoadedPack : DeviceInitiable, IDisposable, INotifyPropertyChanged
    {
        public string Name { get; }
        public string Description => Pack?.description;
        public ImageSource Preview => Pack?.previewImage;
        public string Error { get; set; } = null;
        public bool HasError => Error != null;


        public SwitchItem[] Switches { get; set; } = new SwitchItem[0];

        DirectoryFolder folder;
        JObject jdata;
        string pathBase;

        public PackSchemaRoot Pack { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public LoadedPack(string name, DirectoryFolderLocation location)
        {
            Name = name;

            try
            {
                folder = DirectoryFolder.Open(location);

                var jsons = folder.FindByFilename("pack.json");
                if (jsons.Length == 0) throw new OpenerException("Could not find pack.json file");
                string json = folder.ReadAllText(jsons[0]);
                pathBase = Path.GetDirectoryName(jsons[0]);

                try
                {
                    jdata = (JObject)JsonConvert.DeserializeObject(json);
                }
                catch { throw new OpenerException("Corrupt json in pack.json"); }

                var switchData = new SwitchSchemaRoot(jdata, folder, pathBase);

                Switches = switchData.switches.Select(sw =>
                {
                    if (sw.IsSwitch)
                        return new SwitchItem(sw.name, sw.values);
                    return new SwitchItem(sw.text);
                }).ToArray();

                foreach (var s in Switches)
                {
                    s.PropertyChanged += switch_PropertyChanged;
                }

                Parse();
            }
            catch (Exception e)
            {
                Error = SchemaLoadable.FormatError(e);
            }
        }

        private void switch_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var s = sender as SwitchItem;
            if (e.PropertyName == nameof(s.SelectedValue))
            {
                Parse();
            }
        }

        public void Parse()
        {
            try
            {
                var switches = Switches
                    .Where(sw => sw.Type == SwitchItemType.Option)
                    .ToDictionary(sw => sw.Name, sw => sw);
                Pack = init.Replace(Pack, new PackSchemaRoot(jdata, folder, pathBase, switches));
            }
            catch (OpenerException e)
            {
                Error = e.Message;
            }
            catch (Exception e)
            {
                Error = e.ToString();
            }
        }

        protected override void InitInternal()
        {
            base.InitInternal();
        }

        public void Unload()
        {
            if (folder == null) return;
            folder?.Dispose();
            folder = null;

            foreach (var s in Switches)
            {
                s.PropertyChanged -= switch_PropertyChanged;
            }
        }
    }
}
