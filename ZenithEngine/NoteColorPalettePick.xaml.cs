using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
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
using System.Xml;
using ZenithEngine.ModuleUtil;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Drawing.Color;
using Path = System.IO.Path;

namespace ZenithEngine
{
    /// <summary>
    /// Interaction logic for NoteColorPalettePick.xaml
    /// </summary>
    public partial class NoteColorPalettePick : UserControl
    {
        string searchPath = "";
        public string SelectedImage { get; private set; } = "";
        bool randomise = true;
        int selectedIndex = -1;
        List<Bitmap> images = new List<Bitmap>();

        public event Action PaletteChanged;

        int seed = 0;

        float defS, defV;
        public NoteColorPalettePick()
        {
            InitializeComponent();
        }

        public void SetPath(string path, float defS = 1, float defV = 1)
        {
            this.defS = defS;
            this.defV = defV;
            searchPath = path;
            Reload();
        }

        public void Reload()
        {
            float mult = 0.12345f;
            if (!Directory.Exists(searchPath)) Directory.CreateDirectory(searchPath);
            using (Bitmap palette = new Bitmap(16, 8))
            {
                for (int i = 0; i < 16 * 8; i++)
                {
                    palette.SetPixel(i % 16, (i - i % 16) / 16, ColorUtil.FromHsv(i * mult % 1, defS, defV, 1).ToDrawing());
                }
                palette.Save(Path.Combine(searchPath, "Random.png"));
            }
            using (Bitmap palette = new Bitmap(32, 8))
            {
                for (int i = 0; i < 32 * 8; i++)
                {
                    palette.SetPixel(i % 32, (i - i % 32) / 32, ColorUtil.FromHsv(i * mult % 1, defS, defV, 1).ToDrawing());
                    i++;
                    palette.SetPixel(i % 32, (i - i % 32) / 32, ColorUtil.FromHsv(((i - 1) * mult + 0.166f) % 1, defS, defV, 1).ToDrawing());
                }
                palette.Save(Path.Combine(searchPath, "Random Gradients.png"));
            }
            using (Bitmap palette = new Bitmap(32, 8))
            {
                for (int i = 0; i < 32 * 8; i++)
                {
                    palette.SetPixel(i % 32, (i - i % 32) / 32, ColorUtil.FromHsv(i * mult % 1, defS, defV, 0.8f).ToDrawing());
                    i++;
                    palette.SetPixel(i % 32, (i - i % 32) / 32, ColorUtil.FromHsv(((i - 1) * mult + 0.166f) % 1, defS, defV, 0.8f).ToDrawing());
                }
                palette.Save(Path.Combine(searchPath, "Random Alpha Gradients.png"));
            }
            using (Bitmap palette = new Bitmap(16, 8))
            {
                for (int i = 0; i < 16 * 8; i++)
                {
                    palette.SetPixel(i % 16, (i - i % 16) / 16, ColorUtil.FromHsv(i * mult % 1, defS, defV, 0.8f).ToDrawing());
                }
                palette.Save(Path.Combine(searchPath, "Random with Alpha.png"));
            }
            var imagePaths = Directory.GetFiles(searchPath).Where(s => s.EndsWith(".png")).ToArray();

            paletteList.Items.Clear();
            foreach (var i in images) i.Dispose();
            images.Clear();

            Array.Sort(imagePaths, new Comparison<string>((s1, s2) =>
            {
                if (s1.Contains("Random.png")) return -1;
                if (s2.Contains("Random.png")) return 1;
                else return 0;
            }));

            foreach (var i in imagePaths)
            {
                try
                {
                    using (var fs = new System.IO.FileStream(i, System.IO.FileMode.Open))
                    {
                        Bitmap img = new Bitmap(fs);
                        if (!(img.Width == 16 || img.Width == 32) || img.Width < 1) continue;
                        images.Add(img);
                        var item = new ListBoxItem() { Content = Path.GetFileNameWithoutExtension(i) };
                        if (img.Width == 32) item.Foreground = Brushes.Blue;
                        paletteList.Items.Add(item);
                    }
                }
                catch
                {

                }
            }
            ReadPFAConfig();
            SelectImage(SelectedImage);
        }

        void ReadPFAConfig()
        {
            try
            {
                var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var configPath = Path.Combine(appdata, "Piano From Above/Config.xml");
                if (File.Exists(configPath))
                {
                    var data = File.ReadAllText(configPath);
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(data);
                    var colors = doc.GetElementsByTagName("Colors").Item(0);
                    Bitmap img = new Bitmap(16, 1);
                    for (int i = 0; i < 16; i++)
                    {
                        var c = colors.ChildNodes.Item(i);
                        int r = -1;
                        int g = -1;
                        int b = -1;
                        for (int j = 0; j < 3; j++)
                        {
                            var attrib = c.Attributes.Item(j);
                            if (attrib.Name == "R") r = Convert.ToInt32(attrib.InnerText);
                            if (attrib.Name == "G") g = Convert.ToInt32(attrib.InnerText);
                            if (attrib.Name == "B") b = Convert.ToInt32(attrib.InnerText);
                        }
                        img.SetPixel(i, 0, Color.FromArgb(r, g, b));
                    }
                    images.Add(img);
                    var item = new ListBoxItem() { Content = "PFA Config Colors" };
                    paletteList.Items.Add(item);

                }
            }
            catch { }
        }

        public void SelectImage(string img)
        {
            bool set = false;
            foreach (var i in paletteList.Items)
            {
                if ((string)((ListBoxItem)i).Content == img)
                {
                    paletteList.SelectedItem = i;
                    set = true;
                    break;
                }
            }
            if (!set)
            {
                paletteList.SelectedIndex = 0;
            }

            PaletteChanged?.Invoke();
        }

        public Color4[][] GetColors(int tracks)
        {
            Random r = new Random(seed);
            double[] order = new double[tracks * 16];
            int[] coords = new int[tracks * 16];
            for (int i = 0; i < order.Length; i++)
            {
                order[i] = r.NextDouble();
                coords[i] = i;
            }
            if (randomise)
            {
                Array.Sort(order, coords);
            }
            List<Color4[]> cols = new List<Color4[]>();
            var img = images[selectedIndex];
            for (int i = 0; i < tracks; i++)
            {
                Color4[] trackCols = new Color4[32];
                for (int j = 0; j < 16; j++)
                {
                    int y = coords[i * 16 + j];
                    int x = y % 16;
                    y = y - x;
                    y /= 16;
                    if (img.Width == 16)
                    {
                        trackCols[j * 2] = img.GetPixel(x, y % img.Height).ToDX();
                        trackCols[j * 2 + 1] = img.GetPixel(x, y % img.Height).ToDX();
                    }
                    else
                    {
                        trackCols[j * 2] = img.GetPixel(x * 2, y % img.Height).ToDX();
                        trackCols[j * 2 + 1] = img.GetPixel(x * 2 + 1, y % img.Height).ToDX();
                    }
                }
                cols.Add(trackCols);
            }
            return cols.ToArray();
        }

        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            Reload();
        }

        private void randomiseOrder_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            randomise = (bool)randomiseOrder.IsChecked;
            if (randomise) seed++;
            PaletteChanged?.Invoke();
        }

        private void openPaletteFolder_Click(object sender, RoutedEventArgs e)
        {
            if (!searchPath.Contains(":\\") && !searchPath.Contains(":/"))
                Process.Start("explorer.exe", System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), searchPath));
            else
                Process.Start("explorer.exe", searchPath);
        }

        private void PaletteList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (paletteList.SelectedItem == null) return;
            try
            {
                SelectedImage = (string)((ListBoxItem)paletteList.SelectedItem).Content;
                selectedIndex = paletteList.SelectedIndex;
                PaletteChanged?.Invoke();
            }
            catch
            { }
        }
    }
}
