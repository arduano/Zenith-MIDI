using MIDITrailRender.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ZenithEngine.ModuleUI;

namespace MIDITrailRender.Views
{
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView : UserControl, ISerializableContainer
    {
        public BaseModel Data { get; private set; } = new BaseModel();

        public MainView()
        {
            DataContext = Data;

            InitializeComponent();
        }

        public void Parse(JObject data)
        {
            Dispatcher.Invoke(() =>
            {
                Data = data.ToObject<BaseModel>();
                DataContext = Data;
            });
        }

        public JObject Serialize()
        {
            return JObject.FromObject(Data);
        }
    }
}
