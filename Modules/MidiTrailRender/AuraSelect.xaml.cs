using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Drawing;
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
using Image = System.Drawing.Image;
using Path = System.IO.Path;
using Color = System.Drawing.Color;
using System.Diagnostics;
using System.Reflection;

namespace MIDITrailRender
{
    /// <summary>
    /// Interaction logic for AuraSelect.xaml
    /// </summary>
    public partial class AuraSelect : UserControl
    {
        #region PreviewConvert
        BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }
        #endregion

        Settings settings;
        List<Bitmap> images = new List<Bitmap>();
        int selectedIndex = 0;
        public long lastSetTime = 0;

        string aurasFolder = "Plugins\\Assets\\MIDITrail\\Aura";

        public string SelectedImageName => (string)((ListBoxItem)imagesList.SelectedItem).Content;
        public Bitmap SelectedImage
        {
            get
            {
                return images[selectedIndex];
            }
        }

        public void LoadSettings()
        {
            auraStrength.Value = (decimal)settings.auraStrength;
            auraEnabled.IsChecked = settings.auraEnabled;
            bool set = false;
            foreach (var i in imagesList.Items)
            {
                if ((string)((ListBoxItem)i).Content == settings.selectedAuraImage)
                {
                    imagesList.SelectedItem = i;
                    set = true;
                    break;
                }
            }
            if (!set)
            {
                imagesList.SelectedIndex = 0;
            }
        }

        void ReloadImages()
        {
            foreach (var i in images) i.Dispose();
            images.Clear();
            imagesList.Items.Clear();
            if (!Directory.Exists(aurasFolder)) Directory.CreateDirectory(aurasFolder);
            try
            {
                Properties.Resources.aura_ring.Save(Path.Combine(aurasFolder, "ring.png"));
            }
            catch { }
            var imagePaths = Directory.GetFiles(aurasFolder).Where((p) => p.EndsWith(".png"));
            foreach (var i in imagePaths)
            {
                try
                {
                    using (var fs = new System.IO.FileStream(i, System.IO.FileMode.Open))
                    {
                        Bitmap img = new Bitmap(fs);
                        if (img.Width != img.Height) continue;
                        if (((int)img.PixelFormat & (int)System.Drawing.Imaging.PixelFormat.Alpha) > 0)
                        {
                            images.Add(img);
                            var item = new ListBoxItem() { Content = Path.GetFileNameWithoutExtension(i) };
                            imagesList.Items.Add(item);
                        }
                    }
                }
                catch
                {

                }
            }
            bool set = false;
            foreach (var i in imagesList.Items)
            {
                if ((string)((ListBoxItem)i).Content == settings.selectedAuraImage)
                {
                    imagesList.SelectedItem = i;
                    set = true;
                    break;
                }
            }
            if (!set)
            {
                imagesList.SelectedIndex = 0;
            }
        }

        public AuraSelect(Settings settings) : base()
        {
            this.settings = settings;
            InitializeComponent();
            Resources.MergedDictionaries.Clear();
            ReloadImages();
        }

        private void Reload_Click(object sender, RoutedEventArgs e)
        {
            ReloadImages();
        }

        private void ImagesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (imagesList.SelectedIndex != -1)
            {
                selectedIndex = imagesList.SelectedIndex;
                settings.selectedAuraImage = SelectedImageName;
                lastSetTime = DateTime.Now.Ticks;
                var img = new Bitmap(SelectedImage);
                for (int i = 0; i < img.Width; i++)
                for(int j = 0; j < img.Height; j++)
                    {
                        var col = img.GetPixel(i, j);
                        img.SetPixel(i, j, Color.FromArgb(1, col.A, col.A, col.A));
                    }
                        imagePreview.Source = BitmapToImageSource(img);
            }
        }

        private void AuraStrength_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            try
            {
                settings.auraStrength = (double)auraStrength.Value;
            }
            catch { }
        }

        private void AuraEnabled_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                settings.auraEnabled = (bool)auraEnabled.IsChecked;
            }
            catch { }
        }

        private void openFolder_Click(object sender, RoutedEventArgs e)
        {
            if (!aurasFolder.Contains(":\\") && !aurasFolder.Contains(":/"))
                Process.Start("explorer.exe", System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), aurasFolder));
            else
                Process.Start("explorer.exe", aurasFolder);
        }
    }
}
