using Microsoft.CSharp;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json.Linq;
using SharpCompress;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Archives.Tar;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Dynamic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
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
using ScriptedEngine;
using Brushes = System.Windows.Media.Brushes;
using Path = System.IO.Path;
using ZenithEngine.UI;
using System.Threading;
using Newtonsoft.Json;

namespace ScriptedRender
{
    /// <summary>
    /// Interaction logic for SettingsCtrl.xaml
    /// </summary>

    class ScriptLocation
    {
        public string filename;
        public ScriptType type;
    }

    public partial class SettingsCtrl : UserControl
    {
        List<ScriptLocation> resourcePacks = new List<ScriptLocation>();
        Settings settings;

        public event Action PaletteChanged
        {
            add { paletteList.PaletteChanged += value; }
            remove { paletteList.PaletteChanged -= value; }
        }

        JObject packProfiles = null;
        string profilesDir;
        List<Control> settingsControls = new List<Control>();
        List<UISetting> settingsMeta = new List<UISetting>();
        List<object[]> profiles = new List<object[]>();

        string packPath = "Plugins\\Assets\\Scripted\\Resources";

        CSharpCodeProvider provider = new CSharpCodeProvider();

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

        bool inited = false;
        public SettingsCtrl(Settings settings)
        {
            this.settings = settings;
            InitializeComponent();
            noteDeltaScreenTime.nudToSlider = v => Math.Log(v, 2);
            noteDeltaScreenTime.sliderToNud = v => Math.Pow(2, v);
            inited = true;
            paletteList.SetPath("Plugins\\Assets\\Palettes", 1f);
            ReloadPacks();
            SetValues();
        }

        void WriteDefaultPack()
        {
            var stream = new MemoryStream(Properties.Resources.Examples);
            var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            foreach (var e in archive.Entries)
            {
                var path = Path.Combine("Plugins/Assets/Scripted/Resources", e.FullName);
                if (!Directory.Exists(Path.GetDirectoryName(path))) Directory.CreateDirectory(Path.GetDirectoryName(path));
                if (e.FullName.EndsWith("/")) continue;
                if (File.Exists(path)) continue;
                var f = File.OpenWrite(path);
                var es = e.Open();
                es.CopyTo(f);
                f.Dispose();
                es.Dispose();
            }
            archive.Dispose();
            stream.Dispose();
        }

        void ReloadPacks()
        {
            int lastSelected = pluginList.SelectedIndex;
            string lastSelectedName;
            if (lastSelected == -1)
            {
                lastSelectedName = "Example Flat";
                lastSelected = 0;
            }
            else
            {
                lastSelectedName = (string)((ListBoxItem)pluginList.SelectedItem).Content;
            }

            string dir = packPath;

            resourcePacks.Clear();
            WriteDefaultPack();
            pluginList.Items.Clear();
            foreach (var p in Directory.GetDirectories(dir))
            {
                resourcePacks.Add(new ScriptLocation() { filename = p, type = ScriptType.Folder });
            }
            foreach (var p in Directory.GetFiles(dir, "*.zip"))
            {
                resourcePacks.Add(new ScriptLocation() { filename = p, type = ScriptType.Zip });
            }
            foreach (var p in Directory.GetFiles(dir, "*.zrp"))
            {
                resourcePacks.Add(new ScriptLocation() { filename = p, type = ScriptType.Zrp });
            }
            foreach (var p in Directory.GetFiles(dir, "*.rar"))
            {
                resourcePacks.Add(new ScriptLocation() { filename = p, type = ScriptType.Rar });
            }
            foreach (var p in Directory.GetFiles(dir, "*.7z"))
            {
                resourcePacks.Add(new ScriptLocation() { filename = p, type = ScriptType.SevenZip });
            }
            foreach (var p in Directory.GetFiles(dir, "*.tar"))
            {
                resourcePacks.Add(new ScriptLocation() { filename = p, type = ScriptType.Tar });
            }
            foreach (var p in Directory.GetFiles(dir, "*.tar.bz"))
            {
                resourcePacks.Add(new ScriptLocation() { filename = p, type = ScriptType.Tar });
            }
            foreach (var p in Directory.GetFiles(dir, "*.tar.gz"))
            {
                resourcePacks.Add(new ScriptLocation() { filename = p, type = ScriptType.Tar });
            }
            foreach (var p in Directory.GetFiles(dir, "*.tar.xz"))
            {
                resourcePacks.Add(new ScriptLocation() { filename = p, type = ScriptType.Tar });
            }

            resourcePacks.Sort((a, b) =>
            {
                return a.filename.CompareTo(b.filename);
            });

            foreach (var p in resourcePacks)
            {
                if (p.type == ScriptType.Folder)
                    pluginList.Items.Add(new ListBoxItem()
                    {
                        Content = p.filename.Split('\\').Last(),
                        Foreground = Brushes.White
                    });
                else
                    pluginList.Items.Add(new ListBoxItem()
                    {
                        Content = p.filename.Split('\\').Last(),
                        Foreground = Brushes.Green
                    });
            }

            if ((string)((ListBoxItem)pluginList.Items[lastSelected]).Content == lastSelectedName)
            {
                pluginList.SelectedIndex = lastSelected;
            }
            else
            {
                foreach (ListBoxItem p in pluginList.Items)
                {
                    if ((string)p.Content == lastSelectedName)
                    {
                        pluginList.SelectedItem = p;
                        break;
                    }
                }
            }
        }

        void UnloadScript(Script r)
        {
            foreach (var lt in r.textures) lt.bitmap.Dispose();
        }

        Script LoadScript(string p, ScriptType type, Dictionary<string, string> switches = null, Dictionary<string, string[]> assertSwitches = null)
        {
            var script = new Script() { name = p.Split('\\').Last() };
            string pbase = "";

            ZipArchive archive = null;
            IArchive compress = null;
            archive = null;
            MemoryStream zrpstream = null;

            Bitmap GetBitmap(string path)
            {
                if (type == ScriptType.Folder)
                {
                    if (path == null)
                    { }
                    path = Path.Combine(p, pbase, path);
                    FileStream s;
                    try
                    {
                        s = File.OpenRead(path);
                    }
                    catch { throw new Exception("Could not open " + path); }
                    Bitmap b;
                    try
                    {
                        b = new Bitmap(s);
                    }
                    catch { throw new Exception("Corrupt image: " + path); }
                    s.Close();
                    return b;
                }
                else if (type == ScriptType.Zip || type == ScriptType.Zrp)
                {
                    path = Path.Combine(pbase, path);
                    Stream s;
                    try
                    {
                        s = archive.GetEntry(path).Open();
                    }
                    catch { throw new Exception("Could not open " + path); }
                    Bitmap b;
                    try
                    {
                        b = new Bitmap(s);
                    }
                    catch { throw new Exception("Corrupt image: " + path); }
                    s.Close();
                    return b;
                }
                else
                {
                    path = Path.Combine(pbase, path).Replace("/", "\\");
                    Stream s;
                    var e = compress.Entries.Where(a => a.Key == path).ToArray();
                    if (e.Length == 0) throw new Exception("Could not open " + path);
                    s = e[0].OpenEntryStream();
                    Bitmap b;
                    try
                    {
                        b = new Bitmap(s);
                    }
                    catch { throw new Exception("Corrupt image: " + path); }
                    s.Close();
                    return b;
                }
            }
            try
            {
                string code = "";
                if (type == ScriptType.Folder)
                {
                    var files = Directory.GetFiles(p, "*script.cs", SearchOption.AllDirectories)
                        .Where(s => s.EndsWith("\\script.cs"))
                        .Select(s => s.Substring(p.Length + 1))
                        .ToArray();
                    Array.Sort(files.Select(s => s.Length).ToArray(), files);
                    if (files.Length == 0) throw new Exception("Could not find script.cs file");
                    var jsonpath = files[0];
                    pbase = jsonpath.Substring(0, jsonpath.Length - "script.cs".Length);
                    if (files.Length == 0) throw new Exception("Could not find script.cs file");
                    try
                    {
                        code = File.ReadAllText(Path.Combine(p, jsonpath));
                    }
                    catch { throw new Exception("Could not read script.cs file"); }
                }
                else if (type == ScriptType.Zip || type == ScriptType.Zrp)
                {
                    if (type == ScriptType.Zrp)
                    {
                        var encoded = File.OpenRead(p);
                        var key = new byte[16];
                        var iv = new byte[16];
                        encoded.Read(key, 0, 16);
                        encoded.Read(iv, 0, 16);

                        zrpstream = new MemoryStream();

                        using (AesManaged aes = new AesManaged())
                        {
                            ICryptoTransform decryptor = aes.CreateDecryptor(key, iv);
                            using (CryptoStream cs = new CryptoStream(encoded, decryptor, CryptoStreamMode.Read))
                            {
                                cs.CopyTo(zrpstream);
                            }
                            zrpstream.Position = 0;
                            archive = new ZipArchive(zrpstream);
                        }
                    }
                    else
                    {
                        archive = ZipFile.OpenRead(p);
                    }
                    var files = archive.Entries.Where(e => e.Name == "script.cs").ToArray();
                    Array.Sort(files.Select(s => s.FullName.Length).ToArray(), files);
                    if (files.Length == 0) throw new Exception("Could not find script.cs file");
                    var jsonfile = files[0];
                    pbase = jsonfile.FullName.Substring(0, jsonfile.FullName.Length - "script.cs".Length);
                    using (var jfile = new StreamReader(jsonfile.Open()))
                    {
                        code = jfile.ReadToEnd();
                    }
                }
                else
                {
                    if (type == ScriptType.Rar)
                        compress = RarArchive.Open(p);
                    if (type == ScriptType.SevenZip)
                        compress = SevenZipArchive.Open(p);
                    if (type == ScriptType.Tar)
                        compress = TarArchive.Open(p);

                    var files = compress.Entries.Where(e => e.Key.EndsWith("\\script.cs") || e.Key == "script.cs").ToArray();
                    Array.Sort(files.Select(s => s.Key.Length).ToArray(), files);
                    if (files.Length == 0) throw new Exception("Could not find script.cs file");
                    var jsonfile = files[0];
                    pbase = jsonfile.Key.Substring(0, jsonfile.Key.Length - "script.cs".Length);
                    using (var jfile = new StreamReader(jsonfile.OpenEntryStream()))
                    {
                        code = jfile.ReadToEnd();
                    }
                }

                var fc = code.Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "");

                Dictionary<string, string> notallowed = new Dictionary<string, string>()
                {
                    {"System.IO", null},
                    {"System.Reflection", null},
                    {"Microsoft.CSharp", null},
                    {"System.Net", null},
                    {"Microsoft.VisualBasic", null},
                    {"System.Drawing", null},
                    {"System.AttributeUsage", null},
                    {"System.EnterpriseServices", null},
                    {"System.Media", null},
                    {"System.Messaging", null},
                    {"System.Printing", null},
                    {"System.Security", null},
                    {"System.ServiceModel", null},
                    {"System.ServiceProcess", null},
                    {"System.Speech", null},
                    {"System.Web", null},
                    {"System.Windows", null},
                    {"System.Xml", null},
                    {"Microsoft.Windows", null},
                    {"Microsoft.Win32", null},
                    {"Microsoft.SqlServer", null},
                    {"Microsoft.JScript", null},
                    {"Microsoft.Build", null},
                    {"Accessibility", null},
                    {"Microsoft.Activities", null},
                    {"System.Diagnostics", "random user"},
                    {"System.Runtime", "random user"},
                    {"System.Management", "random user"}
                };

                foreach (var n in notallowed)
                {
                    if (fc.Contains(n.Key))
                    {
                        if (n.Value == null)
                            throw new Exception(n.Key + " is not allowed");
                        else
                            if (fc.Contains(n.Key)) throw new Exception(n.Key + " is not allowed (found by " + n.Value + ")");
                    }
                }

                CompilerParameters compiler_parameters = new CompilerParameters();

                compiler_parameters.GenerateInMemory = true;

                compiler_parameters.GenerateExecutable = false;

                compiler_parameters.ReferencedAssemblies.Add(typeof(object).Assembly.Location);
                compiler_parameters.ReferencedAssemblies.Add(typeof(OpenTK.Vector2).Assembly.Location);
                compiler_parameters.ReferencedAssemblies.Add(typeof(IEnumerable<object>).Assembly.Location);
                compiler_parameters.ReferencedAssemblies.Add(typeof(LinkedList<object>).Assembly.Location);
                compiler_parameters.ReferencedAssemblies.Add(typeof(System.Drawing.Color).Assembly.Location);
                compiler_parameters.ReferencedAssemblies.Add(typeof(IO).Assembly.Location);
                compiler_parameters.ReferencedAssemblies.Add(typeof(System.Linq.Enumerable).Assembly.Location);

                compiler_parameters.CompilerOptions = "/optimize /unsafe /nostdlib";

                CompilerResults results = provider.CompileAssemblyFromSource(compiler_parameters, code);

                if (results.Errors.HasErrors)
                {
                    StringBuilder builder = new StringBuilder();
                    foreach (CompilerError error in results.Errors)
                    {
                        builder.AppendLine(String.Format("Error ({0}): {1}", error.ErrorNumber, error.ErrorText));
                    }
                    throw new Exception(String.Format("Error on line {0}:\n{1}", results.Errors[0].Line, results.Errors[0].ErrorText) + "\n" + builder.ToString());
                }

                IO.loadTexture = (path, loop, linear) =>
                {
                    var bmp = GetBitmap(path);
                    var loaded = new Texture()
                    {
                        bitmap = bmp,
                        path = path,
                        width = bmp.Width,
                        height = bmp.Height,
                        aspectRatio = bmp.Width / (double)bmp.Height,
                        looped = loop,
                        linear = linear
                    };
                    script.textures.Add(loaded);
                    return loaded;
                };

                Assembly assembly = results.CompiledAssembly;
                var renderType = assembly.GetType("Script");
                var instance = (dynamic)Activator.CreateInstance(renderType);
                script.instance = instance;
                script.renderType = renderType;

                IO.callLoadFunction(script.instance);

                if (renderType.GetField("Description") != null) script.description = instance.Description;
                if (renderType.GetField("Preview") != null) script.preview = GetBitmap(instance.Preview);

                if (renderType.GetMethod("Load") == null) throw new Exception("Load method required");
                if (renderType.GetMethod("Render") == null) throw new Exception("Render method required");

                if (renderType.GetMethod("RenderInit") != null) script.hasPreRender = true;
                if (renderType.GetMethod("RenderDispose") != null) script.hasPostRender = true;

                bool hasVar(string name, Type ftype)
                {
                    if (renderType.GetField(name) != null)
                        if (ftype.IsAssignableFrom(renderType.GetField(name).FieldType))
                            return true;
                    if (renderType.GetProperty(name) != null)
                        if (ftype.IsAssignableFrom(renderType.GetProperty(name).PropertyType))
                            return true;
                    return false;
                }

                if (hasVar("ManualNoteDelete", typeof(bool))) script.hasManualNoteDelete = true;
                if (hasVar("NoteCollectorOffset", typeof(double))) script.hasCollectorOffset = true;
                if (hasVar("NoteScreenTime", typeof(double))) script.hasNoteScreenTime = true;
                if (hasVar("LastNoteCount", typeof(long))) script.hasNoteCount = true;
                if (hasVar("UseProfiles", typeof(bool))) script.hasProfiles = script.instance.UseProfiles;

                if (hasVar("SettingsUI", typeof(IEnumerable<UISetting>)))
                {
                    script.uiSettings = instance.SettingsUI;
                }
            }
            catch (Exception e)
            {
                if (e is TargetInvocationException) e = e.InnerException;
                script.error = true;
                script.description = e.Message;
            }
            finally
            {
                if (archive != null)
                    archive.Dispose();
                if (zrpstream != null)
                    zrpstream.Dispose();
                if (compress != null)
                    compress.Dispose();
            }
            return script;
        }

        private void PluginList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadSelectedPack();
        }

        private void ReloadPackButton_Click(object sender, RoutedEventArgs e)
        {
            LoadSelectedPack();
        }

        void PopulateSettingsDock(IEnumerable<UISetting> settings, DockPanel dock)
        {
            void Dispatch(Action action)
            {
                if (Thread.CurrentThread.ManagedThreadId == Dispatcher.Thread.ManagedThreadId)
                {
                    action();
                }
                else
                {
                    Dispatcher.InvokeAsync(action).Task.GetAwaiter().GetResult();
                }
            }

            dock.LastChildFill = false;
            foreach (var sett in settings)
            {
                if (sett is UILabel)
                {
                    var s = sett as UILabel;
                    var label = new Label() { Content = s.Text, FontSize = s.FontSize, Margin = new Thickness(0, 0, 0, s.Padding) };
                    DockPanel.SetDock(label, Dock.Top);

                    dock.Children.Add(label);
                }
                if (sett is UINumber)
                {
                    var s = sett as UINumber;
                    var number = new NumberSelect() { Minimum = (decimal)s.Minimum, Maximum = (decimal)s.Maximum, DecimalPoints = s.DecialPoints, Step = (decimal)s.Step, Value = (decimal)s.Value };
                    number.MinWidth = 100;
                    var label = new Label() { Content = s.Text, FontSize = 16 };
                    var d = new DockPanel() { HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, s.Padding) };
                    if (!(s.Text == null || s.Text == ""))
                    {
                        number.Margin = new Thickness(5, 0, 0, 1);
                        d.Children.Add(label);
                    }
                    d.Children.Add(number);
                    DockPanel.SetDock(d, Dock.Top);
                    number.ValueChanged += (_, e) => { s.Value = (double)e.NewValue; };
                    dock.Children.Add(d);

                    s.EnableToggled += (enable) =>
                    {
                        Dispatch(() => number.IsEnabled = enable);
                    };

                    s.ValueChanged += (v) =>
                    {
                        if (number.Value != (decimal)v)
                            number.Value = (decimal)v;
                    };

                    settingsControls.Add(number);
                    settingsMeta.Add(s);
                }
                if (sett is UINumberSlider)
                {
                    var s = sett as UINumberSlider;
                    double min = s.Minimum;
                    double max = s.Maximum;
                    if (s.Logarithmic)
                    {
                        min = Math.Log(min, 2);
                        max = Math.Log(max, 2);
                    }
                    var number = new ValueSlider() { Minimum = min, Maximum = max, TrueMin = (decimal)s.TrueMinimum, TrueMax = (decimal)s.TrueMaximum, DecimalPoints = s.DecialPoints };
                    if (s.Logarithmic)
                    {
                        number.nudToSlider = v => Math.Log(v, 2);
                        number.sliderToNud = v => Math.Pow(2, v);
                    }
                    number.Value = s.Value;
                    number.MinWidth = 400;
                    var label = new Label() { Content = s.Text, FontSize = 16 };
                    var d = new DockPanel() { HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, s.Padding) };
                    if (!(s.Text == null || s.Text == ""))
                    {
                        number.Margin = new Thickness(5, 0, 0, 1);
                        d.Children.Add(label);
                    }
                    d.Children.Add(number);
                    DockPanel.SetDock(d, Dock.Top);
                    number.ValueChanged += (_, e) => { s.Value = e.NewValue; };
                    dock.Children.Add(d);

                    s.EnableToggled += (enable) =>
                    {
                        Dispatch(() => number.IsEnabled = enable);
                    };

                    s.ValueChanged += (v) =>
                    {
                        v = Math.Round(v, s.DecialPoints);
                        if (number.Value != v)
                            number.Value = v;
                    };

                    settingsControls.Add(number);
                    settingsMeta.Add(s);
                }
                if (sett is UIDropdown)
                {
                    var s = sett as UIDropdown;
                    var drop = new ComboBox() { FontSize = 16 };
                    var label = new Label() { Content = s.Text, FontSize = 16 };
                    foreach (var o in s.Options) drop.Items.Add(new ComboBoxItem() { Content = o, FontSize = 16 });
                    var d = new DockPanel() { HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, s.Padding) };
                    if (!(s.Text == null || s.Text == ""))
                    {
                        drop.Margin = new Thickness(5, 0, 0, 0);
                        d.Children.Add(label);
                    }
                    d.Children.Add(drop);
                    DockPanel.SetDock(d, Dock.Top);
                    drop.SelectionChanged += (_, e) =>
                    {
                        var combo = (ComboBox)e.Source;
                        s.Index = combo.SelectedIndex;
                        s.Value = s.Options[combo.SelectedIndex];
                    };
                    drop.SelectedIndex = s.Index;
                    dock.Children.Add(d);

                    s.EnableToggled += (enable) =>
                    {
                        Dispatch(() => drop.IsEnabled = enable);
                    };

                    s.IndexChanged += (i) =>
                    {
                        if (drop.SelectedIndex != i)
                            drop.SelectedIndex = i;
                    };

                    settingsControls.Add(drop);
                    settingsMeta.Add(s);
                }
                if (sett is UICheckbox)
                {
                    var s = sett as UICheckbox;
                    var check = new BetterCheckbox() { FontSize = 16, Text = s.Text };
                    DockPanel.SetDock(check, Dock.Top);
                    check.CheckToggled += (_, e) =>
                    {
                        s.Checked = e.NewValue;
                    };
                    check.Margin = new Thickness(0, 0, 0, s.Padding);
                    check.IsChecked = s.Checked;
                    dock.Children.Add(check);

                    s.EnableToggled += (enable) =>
                    {
                        Dispatch(() => check.IsEnabled = enable);
                    };

                    s.ValueChanged += (c) =>
                    {
                        if (check.IsChecked != c)
                            check.IsChecked = c;
                    };

                    settingsControls.Add(check);
                    settingsMeta.Add(s);
                }
                if (sett is UITabs)
                {
                    var s = sett as UITabs;
                    var tabs = new TabControl();
                    foreach (var k in s.Tabs.Keys)
                    {
                        var item = new TabItem() { Header = k };
                        var d = new DockPanel() { Margin = new Thickness(10) };
                        d.LastChildFill = false;
                        var c = new Grid();
                        c.Children.Add(d);
                        item.Content = c;
                        PopulateSettingsDock(s.Tabs[k], d);
                        tabs.Items.Add(item);
                    }
                    dock.Children.Add(tabs);
                    dock.LastChildFill = true;
                    break;
                }
            }
        }

        void LoadSelectedPack()
        {
            if (pluginList.SelectedIndex == -1) return;
            try
            {
                settingsControls.Clear();
                settingsMeta.Clear();
                profiles.Clear();
                profileSelect.Items.Clear();
                var p = resourcePacks[pluginList.SelectedIndex];
                if (settings.currScript != null) UnloadScript(settings.currScript);
                var script = LoadScript(p.filename, p.type);

                if (!script.error)
                {
                    pluginDesc.Foreground = Brushes.White;
                    settings.currScript = script;
                    settings.lastScriptChangeTime = DateTime.Now.Ticks;
                }
                else
                {
                    pluginDesc.Foreground = Brushes.Red;
                    settings.currScript = null;
                    settings.lastScriptChangeTime = DateTime.Now.Ticks;
                }
                if (script.preview == null)
                    previewImg.Source = null;
                else
                    previewImg.Source = BitmapToImageSource(script.preview);
                switchTab.Visibility = Visibility.Collapsed;
                settingsPanel.Children.Clear();

                if (script.uiSettings != null)
                {
                    switchTab.Visibility = Visibility.Visible;
                    PopulateSettingsDock(script.uiSettings, settingsPanel);
                }

                if (script.hasProfiles)
                {
                    profilesDir = p.filename + ".profiles.json";
                    if (File.Exists(profilesDir))
                    {
                        try
                        {
                            var text = File.ReadAllText(profilesDir);
                            packProfiles = (JObject)JsonConvert.DeserializeObject(text);
                        }
                        catch
                        {
                            packProfiles = new JObject();
                        }
                    }
                    else
                    {
                        packProfiles = new JObject();
                    }
                    profileDock.Visibility = Visibility.Visible;
                    foreach (var v in packProfiles)
                    {
                        if (!(v.Value is JArray)) continue;
                        var data = (v.Value as JArray).Select<JToken, object>(val => {
                            if (val.Type == JTokenType.Boolean) return (bool)val;
                            if (val.Type == JTokenType.Integer) return (int)val;
                            if (val.Type == JTokenType.Float) return (double)val;
                            return null;
                        }).ToArray();
                        if(!CheckProfileValidity(data)) return;
                        profiles.Add(data);
                        profileSelect.Items.Add(new ComboBoxItem()
                        {
                            Content = v.Key,
                            Tag = v.Key
                        });
                    }
                }
                else
                {
                    profilesDir = null;
                    packProfiles = null;
                    profileDock.Visibility = Visibility.Collapsed;
                }

                pluginDesc.Text = script.description;
            }
            catch { }
        }

        public void SetValues()
        {
            firstNote.Value = settings.firstNote;
            lastNote.Value = settings.lastNote - 1;
            noteDeltaScreenTime.Value = settings.deltaTimeOnScreen;
            paletteList.SelectImage(settings.palette);
        }

        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            ReloadPacks();
        }

        bool screenTimeLock = false;

        private void NoteDeltaScreenTime_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!inited) return;
            try
            {
                if (screenTimeLock) return;
                screenTimeLock = true;
                settings.deltaTimeOnScreen = noteDeltaScreenTime.Value;
                screenTimeLock = false;
            }
            catch (NullReferenceException)
            {
                screenTimeLock = false;
            }
        }

        private void Nud_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (!inited) return;
            try
            {
                if (sender == firstNote) settings.firstNote = (int)firstNote.Value;
                if (sender == lastNote) settings.lastNote = (int)lastNote.Value + 1;
            }
            catch (NullReferenceException) { }
            catch (InvalidOperationException) { }
        }

        private void openFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (!packPath.Contains(":\\") && !packPath.Contains(":/"))
                Process.Start("explorer.exe", System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), packPath));
            else
                Process.Start("explorer.exe", packPath);
        }

        void SaveProfilesFile()
        {
            File.WriteAllText(profilesDir, JsonConvert.SerializeObject(packProfiles));
        }

        private void saveProfile_Click(object sender, RoutedEventArgs e)
        {
            var name = profileName.Text.Trim();
            if (name == "") MessageBox.Show("Please write a name for the profile", "Invalid name");
            else
            {
                if (packProfiles.ContainsKey(name))
                {
                    if (MessageBox.Show("Are you sure you want to override profile " + name + "?", "Override", MessageBoxButton.YesNo) == MessageBoxResult.No) return;
                    packProfiles.Remove(name);
                    ComboBoxItem item = null;
                    foreach (var i in profileSelect.Items)
                    {
                        if ((string)((ComboBoxItem)i).Tag == name)
                        {
                            item = (ComboBoxItem)i;
                            break;
                        }
                    }
                    profiles.RemoveAt(profileSelect.Items.IndexOf(item));
                    profileSelect.Items.Remove(item);
                }
                var values = settingsMeta.Select(s =>
                {
                    if (s is UINumber)
                        return (object)(s as UINumber).Value;
                    if (s is UINumberSlider)
                        return (object)(s as UINumberSlider).Value;
                    if (s is UICheckbox)
                        return (object)(s as UICheckbox).Checked;
                    if (s is UIDropdown)
                        return (object)(s as UIDropdown).Index;
                    return (object)null;
                }).ToArray();
                packProfiles.Add(name, new JArray(values));
                SaveProfilesFile();
                profiles.Add(values);
                profileSelect.Items.Add(new ComboBoxItem()
                {
                    Content = name,
                    Tag = name
                });
                profileSelect.SelectedIndex = profileSelect.Items.Count - 1;
            }
        }

        private bool CheckProfileValidity(IEnumerable<object> data)
        {
            var i = 0;
            foreach (var d in data)
            {
                if (settingsMeta[i] is UINumber)
                    if (!typeof(double).IsAssignableFrom(d.GetType()) && !typeof(int).IsAssignableFrom(d.GetType())) 
                        return false;
                if (settingsMeta[i] is UINumberSlider)
                    if (!typeof(double).IsAssignableFrom(d.GetType()) && !typeof(int).IsAssignableFrom(d.GetType())) 
                        return false;
                if (settingsMeta[i] is UICheckbox)
                    if (!typeof(bool).IsAssignableFrom(d.GetType())) 
                        return false;
                if (settingsMeta[i] is UIDropdown)
                {
                    if (!typeof(int).IsAssignableFrom(d.GetType())) 
                        return false;
                    var index = (int)d;
                    if (index < 0 || index >= ((UIDropdown)settingsMeta[i]).Options.Length) 
                        return false;
                }
                i++;
            }
            return true;
        }

        private void deleteProfile_Click(object sender, RoutedEventArgs e)
        {
            if (profileSelect.SelectedIndex == -1) return;
            var i = profileSelect.SelectedIndex;
            profiles.RemoveAt(i);
            var name = (string)((ComboBoxItem)profileSelect.SelectedItem).Tag;
            var selected = profileSelect.SelectedItem;
            profileSelect.SelectedIndex = -1;
            profileSelect.Items.Remove(selected);
            packProfiles.Remove(name);
            SaveProfilesFile();
        }

        private void profileDefaults_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < settingsMeta.Count; i++)
            {
                var m = settingsMeta[i];
                if (m is UINumber)
                {
                    var me = m as UINumber;
                    me.Value = me.Default;
                }
                if (m is UINumberSlider)
                {
                    var me = m as UINumberSlider;
                    me.Value = me.Default;
                }
                if (m is UICheckbox)
                {
                    var me = m as UICheckbox;
                    me.Checked = me.Default;
                }
                if (m is UIDropdown)
                {
                    var me = m as UIDropdown;
                    me.Index = me.Default;
                }
            }
        }

        private void profileSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (profileSelect.SelectedIndex == -1) return;
            var profile = profiles[profileSelect.SelectedIndex];

            for (int i = 0; i < profile.Length; i++)
            {
                var d = profile[i]
;                if (settingsMeta[i] is UINumber)
                {
                    if (!typeof(double).IsAssignableFrom(d.GetType()) && !typeof(int).IsAssignableFrom(d.GetType())) return;
                    (settingsMeta[i] as UINumber).Value = (double)d;
                }
                if (settingsMeta[i] is UINumberSlider)
                {
                    if (!typeof(double).IsAssignableFrom(d.GetType()) && !typeof(int).IsAssignableFrom(d.GetType())) return;
                    (settingsMeta[i] as UINumberSlider).Value = (double)d;
                }
                if (settingsMeta[i] is UICheckbox)
                {
                    if (!typeof(bool).IsAssignableFrom(d.GetType())) return;
                    (settingsMeta[i] as UICheckbox).Checked = (bool)d;
                }
                if (settingsMeta[i] is UIDropdown)
                {
                    if (!typeof(int).IsAssignableFrom(d.GetType())) return;
                    var index = (int)d;
                    if (index < 0 || index >= ((UIDropdown)settingsMeta[i]).Options.Length) return;
                    (settingsMeta[i] as UIDropdown).Index = (int)d;
                }
            }
        }

        private void profileSelect_DropDownOpened(object sender, EventArgs e)
        {
            profileSelect.SelectedIndex = -1;
        }
    }
}
