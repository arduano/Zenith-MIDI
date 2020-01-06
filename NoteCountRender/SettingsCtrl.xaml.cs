using System;
using System.Collections.Generic;
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
using Path = System.IO.Path;
using FontStyle = System.Drawing.FontStyle;
using Microsoft.Win32;

namespace NoteCountRender
{
    /// <summary>
    /// Interaction logic for SettingsCtrl.xaml
    /// </summary>
    public partial class SettingsCtrl : UserControl
    {
        Settings settings;

        string defText = @"Notes: {nc} / {tn}
BPM: {bpm}
NPS: {nps}
PPQ: {ppq}
Polyphony: {plph}
Time: {currtime}";
        string fullText = @"Notes: {nc} / {tn} / {nr}
BPM: {bpm}
NPS: {nps}
Polyphony: {plph}
Seconds: {currsec} / {totalsec} / {remsec}
Time: {currtime} / {totaltime} / {remtime}
Ticks: {currticks} / {totalticks} / {remticks}
Bars: {currbars} / {totalbars} / {rembars}
PPQ: {ppq}
Time Signature: {tsn}/{tsd}
Average NPS: {avgnps}";

        bool initialised = false;

        Dictionary<FontStyle, string> fontStyles = new Dictionary<FontStyle, string>() {
            { System.Drawing.FontStyle.Regular, "Regular" },
            { System.Drawing.FontStyle.Bold, "Bold" },
            { System.Drawing.FontStyle.Italic, "Italic" },
            { System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic, "Bold Italic" },
        };

        public SettingsCtrl(Settings settings) : base()
        {
            this.settings = settings;
            InitializeComponent();
            foreach (var font in System.Drawing.FontFamily.Families)
            {
                try
                {
                    using (var f = new System.Drawing.Font(font.Name, 12)) { }
                }
                catch { continue; }
                var dock = new DockPanel();
                dock.Children.Add(new Label()
                {
                    Content = font.Name,
                    Padding = new Thickness(2)
                });
                dock.Children.Add(new Label()
                {
                    Content = "AaBbCc123",
                    FontFamily = new FontFamily(font.Name),
                    Padding = new Thickness(2),
                    VerticalContentAlignment = VerticalAlignment.Center
                });
                var item = new ComboBoxItem()
                {
                    Content = dock
                };
                item.Tag = font.Name;
                fontSelect.Items.Add(item);
            }
            foreach (var i in fontSelect.Items)
            {
                if ((string)((ComboBoxItem)i).Tag == settings.fontName)
                {
                    fontSelect.SelectedItem = i;
                    break;
                }
            }
            fontSize.Value = settings.fontSize;
            textTemplate.Text = settings.text;
            saveCsv.IsChecked = settings.saveCsv;
            csvFormat.Text = settings.csvFormat;
            initialised = true;
            UpdateFontStyles();
            Reload();
        }

        private void AlignSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!initialised) return;
            settings.textAlignment = (Alignments)alignSelect.SelectedIndex;
        }

        private void FontSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!initialised) return;
            settings.fontSize = (int)fontSize.Value;
        }

        private void FontSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!initialised) return;
            settings.fontName = (string)((ComboBoxItem)fontSelect.SelectedItem).Tag;
            UpdateFontStyles();
        }

        void UpdateFontStyles()
        {
            fontStyle.Items.Clear();
            using (var font = new System.Drawing.FontFamily(settings.fontName))
            {
                foreach (var k in fontStyles.Keys)
                {
                    if (font.IsStyleAvailable(k))
                    {
                        fontStyle.Items.Add(new ComboBoxItem() { Content = fontStyles[k] });
                    }
                }
            }
            fontStyle.SelectedIndex = 0;
        }

        private void FontStyle_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!initialised) return;
            try
            {
                settings.fontStyle = fontStyles.ToDictionary(kp => kp.Value, kp => kp.Key)[(string)((ComboBoxItem)fontStyle.SelectedItem).Content];
            }
            catch { }
        }

        private void TextTemplate_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!initialised) return;
            settings.text = textTemplate.Text;
        }

        List<string> templateStrings = new List<string>();
        void Reload()
        {
            if (!Directory.Exists("Plugins/Assets/NoteCounter/Templates"))
            {
                Directory.CreateDirectory("Plugins/Assets/NoteCounter/Templates");
            }
            try
            {
                File.WriteAllText("Plugins/Assets/NoteCounter/Templates/default.txt", defText);
            }
            catch { }
            try
            {
                File.WriteAllText("Plugins/Assets/NoteCounter/Templates/full.txt", fullText);
            }
            catch { }
            var files = Directory.GetFiles("Plugins/Assets/NoteCounter/Templates").Where(f => f.EndsWith(".txt"));
            templateStrings.Clear();
            templates.Items.Clear();
            foreach (var f in files)
            {
                string text = File.ReadAllText(f);
                templateStrings.Add(text);
                templates.Items.Add(new ComboBoxItem() { Content = Path.GetFileNameWithoutExtension(f) });
            }
            foreach (var i in templates.Items)
            {
                if ((string)((ComboBoxItem)i).Content == "default")
                {
                    templates.SelectedItem = i;
                    break;
                }
            }
        }

        private void Templates_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                textTemplate.Text = templateStrings[templates.SelectedIndex];
            }
            catch { }
        }

        private void Reload_Click(object sender, RoutedEventArgs e)
        {
            Reload();
        }

        private void saveCsv_Checked(object sender, RoutedEventArgs e)
        {
            if (!initialised) return;
            settings.saveCsv = (bool)saveCsv.IsChecked;
         }

        private void browseOutputSaveButton_Click(object sender, RoutedEventArgs e)
        {
            var save = new SaveFileDialog();
            save.OverwritePrompt = true;
            save.Filter = "CSV date file (*.csv)|*.csv";
            if ((bool)save.ShowDialog())
            {
                csvPath.Text = save.FileName;
                settings.csvOutput = save.FileName;
            }
        }

        private void csvFormat_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!initialised) return; 
            settings.csvFormat = csvFormat.Text;
        }
    }
}
