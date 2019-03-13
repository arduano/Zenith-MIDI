using OpenTK.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
using Brushes = System.Windows.Media.Brushes;
using Color = System.Drawing.Color;
using Path = System.IO.Path;

namespace BMEngine
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
            pathLabel.Text = "Path: " + path;
            Reload();
        }

        public void Reload()
        {
            float mult = 0.12345f;
            if (!Directory.Exists(searchPath)) Directory.CreateDirectory(searchPath);
            using (Bitmap palette = new Bitmap(16, 64))
            {
                for (int i = 0; i < 16 * 64; i++)
                {
                    palette.SetPixel(i % 16, (i - i % 16) / 16, (Color)Color4.FromHsv(new OpenTK.Vector4(i * mult % 1, defS, defV, 1)));
                }
                palette.Save(Path.Combine(searchPath, "Random.png"));
            }
            using (Bitmap palette = new Bitmap(32, 64))
            {
                for (int i = 0; i < 32 * 64; i++)
                {
                    palette.SetPixel(i % 32, (i - i % 32) / 32, (Color)Color4.FromHsv(new OpenTK.Vector4(i * mult % 1, defS, defV, 1)));
                    i++;
                    palette.SetPixel(i % 32, (i - i % 32) / 32, (Color)Color4.FromHsv(new OpenTK.Vector4(((i - 1) * mult + 0.166f) % 1, defS, defV, 1)));
                }
                palette.Save(Path.Combine(searchPath, "Random Gradients.png"));
            }
            using (Bitmap palette = new Bitmap(16, 64))
            {
                for (int i = 0; i < 16 * 64; i++)
                {
                    palette.SetPixel(i % 16, (i - i % 16) / 16, (Color)Color4.FromHsv(new OpenTK.Vector4(i * mult % 1, defS, defV, 0.8f)));
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
            SelectImage(SelectedImage);
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
        }

        public Color4[] GetColors(int tracks)
        {
            Random r = new Random(0);
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
            List<Color4> cols = new List<Color4>();
            var img = images[selectedIndex];
            for (int i = 0; i < tracks; i++)
            {
                for (int j = 0; j < 16; j++)
                {
                    int y = coords[i * 16 + j];
                    int x = y % 16;
                    y = y - x;
                    y /= 16;
                    if (img.Width == 16)
                    {
                        cols.Add(img.GetPixel(x, y % img.Height));
                        cols.Add(img.GetPixel(x, y % img.Height));
                    }
                    else
                    {
                        cols.Add(img.GetPixel(x * 2, y % img.Height));
                        cols.Add(img.GetPixel(x * 2 + 1, y % img.Height));
                    }
                }
            }
            return cols.ToArray();
        }

        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            Reload();
        }

        private void RandomiseOrder_Checked(object sender, RoutedEventArgs e)
        {
            randomise = (bool)randomiseOrder.IsChecked;
        }

        private void PaletteList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (paletteList.SelectedItem == null) return;
            try
            {
                SelectedImage = (string)((ListBoxItem)paletteList.SelectedItem).Content;
                selectedIndex = paletteList.SelectedIndex;

            }
            catch
            { }
        }
    }
}
