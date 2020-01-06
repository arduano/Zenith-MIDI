using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Remoting;
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
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpCompress;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Archives.Tar;
using Brushes = System.Windows.Media.Brushes;
using Path = System.IO.Path;

namespace TexturedRender
{
    /// <summary>
    /// Interaction logic for SettingsCtrl.xaml
    /// </summary>

    class PackLocation
    {
        public string filename;
        public PackType type;
    }

    public partial class SettingsCtrl : UserControl
    {
        List<PackLocation> resourcePacks = new List<PackLocation>();
        Settings settings;

        public event Action PaletteChanged
        {
            add { paletteList.PaletteChanged += value; }
            remove { paletteList.PaletteChanged -= value; }
        }

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
            inited = true;
            paletteList.SetPath("Plugins\\Assets\\Palettes", 1f);
            ReloadPacks();
            SetValues();
        }

        void WriteDefaultPack()
        {
            string dir = "Plugins\\Assets\\Textured\\Resources\\Default";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            Properties.Resources.keyBlack.Save(dir + "\\keyBlack.png");
            Properties.Resources.keyBlackPressed.Save(dir + "\\keyBlackPressed.png");
            Properties.Resources.keyWhite.Save(dir + "\\keyWhite.png");
            Properties.Resources.keyWhitePressed.Save(dir + "\\keyWhitePressed.png");
            Properties.Resources.note.Save(dir + "\\note.png");
            Properties.Resources.bar.Save(dir + "\\bar.png");
            Properties.Resources.noteEdge.Save(dir + "\\noteEdge.png");
            Properties.Resources.preview.Save(dir + "\\preview.png");
            File.WriteAllBytes(dir + "\\pack.json", Properties.Resources.pack);
        }

        void ReloadPacks()
        {
            int lastSelected = pluginList.SelectedIndex;
            string lastSelectedName;
            if (lastSelected == -1)
            {
                lastSelectedName = "Default";
                lastSelected = 0;
            }
            else
            {
                lastSelectedName = (string)((ListBoxItem)pluginList.SelectedItem).Content;
            }

            string dir = "Plugins\\Assets\\Textured\\Resources";

            resourcePacks.Clear();
            WriteDefaultPack();
            pluginList.Items.Clear();
            foreach (var p in Directory.GetDirectories(dir))
            {
                resourcePacks.Add(new PackLocation() { filename = p, type = PackType.Folder });
            }
            foreach (var p in Directory.GetFiles(dir, "*.zip"))
            {
                resourcePacks.Add(new PackLocation() { filename = p, type = PackType.Zip });
            }
            foreach (var p in Directory.GetFiles(dir, "*.zrp"))
            {
                resourcePacks.Add(new PackLocation() { filename = p, type = PackType.Zrp });
            }
            foreach (var p in Directory.GetFiles(dir, "*.rar"))
            {
                resourcePacks.Add(new PackLocation() { filename = p, type = PackType.Rar });
            }
            foreach (var p in Directory.GetFiles(dir, "*.7z"))
            {
                resourcePacks.Add(new PackLocation() { filename = p, type = PackType.SevenZip });
            }
            foreach (var p in Directory.GetFiles(dir, "*.tar"))
            {
                resourcePacks.Add(new PackLocation() { filename = p, type = PackType.Tar });
            }
            foreach (var p in Directory.GetFiles(dir, "*.tar.bz"))
            {
                resourcePacks.Add(new PackLocation() { filename = p, type = PackType.Tar });
            }
            foreach (var p in Directory.GetFiles(dir, "*.tar.gz"))
            {
                resourcePacks.Add(new PackLocation() { filename = p, type = PackType.Tar });
            }
            foreach (var p in Directory.GetFiles(dir, "*.tar.xz"))
            {
                resourcePacks.Add(new PackLocation() { filename = p, type = PackType.Tar });
            }

            resourcePacks.Sort((a, b) =>
            {
                return a.filename.CompareTo(b.filename);
            });

            foreach (var p in resourcePacks)
            {
                if (p.type == PackType.Folder)
                    pluginList.Items.Add(new ListBoxItem()
                    {
                        Content = p.filename.Split('\\').Last(),
                        Foreground = Brushes.Black
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

        void UnloadPack(Pack r)
        {
            lock (r)
            {
                if (r.whiteKeyTex != null)
                    r.whiteKeyTex.Dispose();
                if (r.blackKeyTex != null)
                    r.blackKeyTex.Dispose();
                if (r.whiteKeyPressedTex != null)
                    r.whiteKeyPressedTex.Dispose();
                if (r.blackKeyPressedTex != null)
                    r.blackKeyPressedTex.Dispose();
                if (r.preview != null)
                    r.preview.Dispose();
                if (r.NoteTextures != null)
                    foreach (var n in r.NoteTextures)
                    {
                        if (n.noteMiddleTex != null)
                            n.noteMiddleTex.Dispose();
                        if (n.noteBottomTex != null)
                            n.noteBottomTex.Dispose();
                        if (n.noteTopTex != null)
                            n.noteTopTex.Dispose();
                    }
                if (r.OverlayTextures != null)
                    foreach (var o in r.OverlayTextures)
                    {
                        if (o.tex != null)
                            o.tex.Dispose();
                    }
                r.disposed = true;
            }
        }

        T parseType<T>(Pack pack, dynamic o)
        {
            if (o == null) throw new RuntimeBinderException();
            string switchName = null;
            try
            {
                switchName = (string)((JObject)o).GetValue("_switch");
                if (switchName == null) throw new RuntimeBinderException();
            }
            catch
            {
                try
                {
                    return (T)o;
                }
                catch
                {
                    throw new Exception("value " + o.ToString() + " can't be converted to type " + typeof(T).ToString());
                }
            }

            if (!pack.switchValues.ContainsKey(switchName))
            {
                throw new Exception("switch name not found: " + switchName);
            }

            dynamic _o;
            try
            {
                _o = ((JObject)o).GetValue(pack.switchValues[switchName]);
            }
            catch
            {
                throw new Exception("value " + pack.switchValues[switchName] + " not found on a switch");
            }

            try
            {
                return parseType<T>(pack, _o);
            }
            catch
            {
                throw new Exception("value " + _o.ToString() + " can't be converted to type " + typeof(T).ToString());
            }
        }

        Pack LoadPack(string p, PackType type, Dictionary<string, string> switches = null, Dictionary<string, string[]> assertSwitches = null)
        {
            var pack = new Pack() { name = p.Split('\\').Last() };
            string pbase = "";

            ZipArchive archive = null;
            IArchive compress = null;
            archive = null;
            MemoryStream zrpstream = null;

            TextureShaderType strToShader(string s)
            {
                if (s == "normal") return TextureShaderType.Normal;
                if (s == "inverse") return TextureShaderType.Inverted;
                if (s == "hybrid") return TextureShaderType.Hybrid;
                throw new Exception("Unknown shader type \"" + s + "\"");
            }

            KeyType strToKeyType(string s)
            {
                if (s == "black") return KeyType.Black;
                if (s == "white") return KeyType.White;
                if (s == "both") return KeyType.Both;
                throw new Exception("Unknown key type \"" + s + "\"");
            }

            Bitmap GetBitmap(string path)
            {
                if (type == PackType.Folder)
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
                else if (type == PackType.Zip || type == PackType.Zrp)
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
                string json = "";
                if (type == PackType.Folder)
                {
                    var files = Directory.GetFiles(p, "*pack.json", SearchOption.AllDirectories)
                        .Where(s => s.EndsWith("\\pack.json"))
                        .Select(s => s.Substring(p.Length + 1))
                        .ToArray();
                    Array.Sort(files.Select(s => s.Length).ToArray(), files);
                    var jsonpath = files[0];
                    pbase = jsonpath.Substring(0, jsonpath.Length - "pack.json".Length);
                    if (files.Length == 0) throw new Exception("Could not find pack.json file");
                    try
                    {
                        json = File.ReadAllText(Path.Combine(p, jsonpath));
                    }
                    catch { throw new Exception("Could not read pack.json file"); }
                }
                else if (type == PackType.Zip || type == PackType.Zrp)
                {
                    if (type == PackType.Zrp)
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
                    var files = archive.Entries.Where(e => e.Name == "pack.json").ToArray();
                    Array.Sort(files.Select(s => s.FullName.Length).ToArray(), files);
                    if (files.Length == 0) throw new Exception("Could not find pack.json file");
                    var jsonfile = files[0];
                    pbase = jsonfile.FullName.Substring(0, jsonfile.FullName.Length - "pack.json".Length);
                    using (var jfile = new StreamReader(jsonfile.Open()))
                    {
                        json = jfile.ReadToEnd();
                    }
                }
                else
                {
                    if (type == PackType.Rar)
                        compress = RarArchive.Open(p);
                    if (type == PackType.SevenZip)
                        compress = SevenZipArchive.Open(p);
                    if (type == PackType.Tar)
                        compress = TarArchive.Open(p);

                    var files = compress.Entries.Where(e => e.Key.EndsWith("\\pack.json") || e.Key == "pack.json").ToArray();
                    Array.Sort(files.Select(s => s.Key.Length).ToArray(), files);
                    if (files.Length == 0) throw new Exception("Could not find pack.json file");
                    var jsonfile = files[0];
                    pbase = jsonfile.Key.Substring(0, jsonfile.Key.Length - "pack.json".Length);
                    using (var jfile = new StreamReader(jsonfile.OpenEntryStream()))
                    {
                        json = jfile.ReadToEnd();
                    }
                }
                dynamic data;
                try
                {
                    data = (dynamic)JsonConvert.DeserializeObject(json);
                }
                catch { throw new Exception("Corrupt json in pack.json"); }
                try
                {
                    pack.description = data.description;
                }
                catch (RuntimeBinderException) { pack.description = "[no description]"; }


                #region Switches
                JArray sw = null;
                bool swIsArray = false;
                try
                {
                    var s = data.switches;
                    if (s.GetType() == typeof(JArray)) swIsArray = true;
                    sw = s;
                }
                catch (RuntimeBinderException) { }
                if (sw != null)
                {
                    if (!swIsArray) throw new Exception("switches must be an array");
                    foreach (dynamic s in sw)
                    {
                        string swName = null;
                        List<string> swVals = new List<string>();
                        dynamic swValArr;
                        swName = s.name;
                        if (swName == null)
                            throw new Exception("missing property 'name' on switch");
                        swValArr = s.values;
                        if (swValArr == null) throw new Exception("missing property 'values' on switch");
                        if (swValArr.GetType() != typeof(JArray)) throw new Exception("'values' must be a string array");
                        if (((JArray)swValArr).Count == 0) throw new Exception("'values' array must have at least 1 item");
                        foreach (dynamic v in (JArray)swValArr)
                        {
                            swVals.Add((string)v);
                        }
                        pack.switchChoices.Add(swName, swVals.ToArray());
                        if (assertSwitches != null)
                            if (!assertSwitches.ContainsKey(swName) || !swVals.SequenceEqual(assertSwitches[swName]))
                            {
                                throw new Exception("switches have been changed in pack.json, please reload pack");
                            }
                        pack.switchValues.Add(swName, swVals[0]);
                    }
                    if (assertSwitches != null)
                        if (pack.switchValues.Count != assertSwitches.Count)
                            throw new Exception("switches have been changed in pack.json, please reload pack");
                        else
                            pack.switchValues = switches;
                }
                #endregion


                string pname;
                try
                {
                    pname = parseType<string>(pack, data.previewImage);
                    if (pname != null)
                        pack.preview = GetBitmap(pname);
                }
                catch (RuntimeBinderException) { }

                #region Misc
                try
                {
                    pack.keyboardHeight = parseType<double>(pack, data.keyboardHeight) / 100;
                }
                catch (RuntimeBinderException) { }
                try
                {
                    pack.sameWidthNotes = parseType<bool>(pack, data.sameWidthNotes);
                }
                catch (RuntimeBinderException) { }
                try
                {
                    pack.blackKeysFullOctave = parseType<bool>(pack, data.blackKeysFullOctave);
                }
                catch (RuntimeBinderException) { }
                try
                {
                    pack.whiteKeysFullOctave = parseType<bool>(pack, data.whiteKeysFullOctave);
                }
                catch (RuntimeBinderException) { }
                try
                {
                    pack.blackKeyHeight = parseType<double>(pack, data.blackKeyHeight) / 100;
                }
                catch (RuntimeBinderException) { }
                try
                {
                    pack.blackKeyDefaultWhite = parseType<bool>(pack, data.blackKeysWhiteShade);
                }
                catch (RuntimeBinderException) { }
                try
                {
                    pack.interpolateUnendedNotes = parseType<float>(pack, data.interpolateUnendedNotes);
                }
                catch (RuntimeBinderException) { }
                #endregion

                #region Shaders
                string shader = null;
                try
                {
                    shader = parseType<string>(pack, data.noteShader);
                }
                catch (RuntimeBinderException) { }
                if (shader != null) pack.noteShader = strToShader(shader);
                shader = null;
                try
                {
                    shader = parseType<string>(pack, data.whiteKeyShader);
                }
                catch (RuntimeBinderException) { }
                if (shader != null) pack.whiteKeyShader = strToShader(shader);
                shader = null;
                try
                {
                    shader = parseType<string>(pack, data.blackKeyShader);
                }
                catch (RuntimeBinderException) { }
                if (shader != null) pack.blackKeyShader = strToShader(shader);
                #endregion

                #region Get Keys
                try
                {
                    pname = parseType<string>(pack, data.blackKey);
                }
                catch (RuntimeBinderException) { throw new Exception("Missing property \"blackKey\""); }
                pack.blackKeyTex = GetBitmap(pname);
                try
                {
                    pname = parseType<string>(pack, data.blackKeyPressed);
                }
                catch (RuntimeBinderException) { throw new Exception("Missing property \"blackKeyPressed\""); }
                pack.blackKeyPressedTex = GetBitmap(pname);
                try
                {
                    pname = parseType<string>(pack, data.whiteKey);
                }
                catch (RuntimeBinderException) { throw new Exception("Missing property \"whiteKey\""); }
                pack.whiteKeyTex = GetBitmap(pname);
                try
                {
                    pname = parseType<string>(pack, data.whiteKeyPressed);
                }
                catch (RuntimeBinderException) { throw new Exception("Missing property \"whiteKeyPressed\""); }
                pack.whiteKeyPressedTex = GetBitmap(pname);

                try
                {
                    pname = parseType<string>(pack, data.whiteKeyLeft);
                    if (pname != null)
                        pack.whiteKeyLeftTex = GetBitmap(pname);
                }
                catch (RuntimeBinderException) { pack.whiteKeyLeftTex = null; }
                try
                {
                    pname = parseType<string>(pack, data.whiteKeyLeftPressed);
                    if (pname != null)
                        pack.whiteKeyPressedLeftTex = GetBitmap(pname);
                }
                catch (RuntimeBinderException) { pack.whiteKeyPressedLeftTex = null; }

                try
                {
                    pname = parseType<string>(pack, data.whiteKeyRight);
                    if (pname != null)
                        pack.whiteKeyRightTex = GetBitmap(pname);
                }
                catch (RuntimeBinderException) { pack.whiteKeyRightTex = null; }
                try
                {
                    pname = parseType<string>(pack, data.whiteKeyRightPressed);
                    if (pname != null)
                        pack.whiteKeyPressedRightTex = GetBitmap(pname);
                }
                catch (RuntimeBinderException) { pack.whiteKeyPressedRightTex = null; }

                if ((pack.whiteKeyLeftTex == null) ^ (pack.whiteKeyPressedLeftTex == null))
                    if (pack.whiteKeyLeftTex == null)
                        throw new Exception("whiteKeyLeft is incliuded while whiteKeyLeftPressed is missing. Include or remove both.");
                    else
                        throw new Exception("whiteKeyLeftPressed is incliuded while whiteKeyLeft is missing. Include or remove both.");

                if ((pack.whiteKeyRightTex == null) ^ (pack.whiteKeyPressedRightTex == null))
                    if (pack.whiteKeyRightTex == null)
                        throw new Exception("whiteKeyRight is incliuded while whiteKeyRightPressed is missing. Include or remove both.");
                    else
                        throw new Exception("whiteKeyRightPressed is incliuded while whiteKeyRight is missing. Include or remove both.");

                #endregion

                #region Oversizes
                try
                {
                    pack.whiteKeyOversize = parseType<double>(pack, data.whiteKeyOversize) / 100;
                }
                catch (RuntimeBinderException) { }
                try
                {
                    pack.blackKeyOversize = parseType<double>(pack, data.blackKeyOversize) / 100;
                }
                catch (RuntimeBinderException) { }
                try
                {
                    pack.whiteKeyPressedOversize = parseType<double>(pack, data.whiteKeyPressedOversize) / 100;
                }
                catch (RuntimeBinderException) { }
                try
                {
                    pack.blackKeyPressedOversize = parseType<double>(pack, data.blackKeyPressedOversize) / 100;
                }
                catch (RuntimeBinderException) { }
                #endregion

                #region Black Key Sizes
                try
                {
                    pack.blackKey2setOffset = parseType<double>(pack, data.blackKey2setOffset);
                }
                catch (RuntimeBinderException) { }
                try
                {
                    pack.blackKey3setOffset = parseType<double>(pack, data.blackKey3setOffset);
                }
                catch (RuntimeBinderException) { }
                try
                {
                    pack.blackKeyScale = parseType<double>(pack, data.blackKeyScale);
                }
                catch (RuntimeBinderException) { }
                try
                {
                    pack.blackNote2setOffset = parseType<double>(pack, data.blackNote2setOffset);
                }
                catch (RuntimeBinderException) { }
                try
                {
                    pack.blackNote3setOffset = parseType<double>(pack, data.blackNote3setOffset);
                }
                catch (RuntimeBinderException) { }
                try
                {
                    pack.blackNoteScale = parseType<double>(pack, data.blackNoteScale);
                }
                catch (RuntimeBinderException) { }
                try
                {
                    JArray offsets = parseType<JArray>(pack, data.advancedBlackKeyOffsets);
                    if (offsets.Count != 5) throw new Exception("advancedBlackKeyOffsets must have 5 elements");
                    pack.advancedBlackKeyOffsets = offsets.Select(s => (double)s).ToArray();
                }
                catch (RuntimeBinderException) { }
                #endregion

                #region Bar
                try
                {
                    pname = parseType<string>(pack, data.bar);
                    pack.useBar = true;
                }
                catch (RuntimeBinderException) { }

                if (pack.useBar)
                {
                    pack.barTex = GetBitmap(pname);
                    try
                    {
                        pack.barHeight = parseType<double>(pack, data.barHeight) / 100;
                    }
                    catch (RuntimeBinderException) { }
                }
                #endregion

                #region Overlays
                JArray overlaysArray = null;
                bool notArray = false;
                try
                {
                    var _array = parseType<JArray>(pack, data.overlays);
                    if (_array != null)
                    {
                        notArray = _array.GetType() != typeof(JArray);
                        if (!notArray) overlaysArray = _array;
                    }
                }
                catch (RuntimeBinderException) { }
                if (overlaysArray != null)
                {
                    if (notArray) throw new Exception("overlays must be an array");
                    pack.OverlayTextures = overlaysArray.Select((dynamic o) =>
                    {
                        o = parseType<JObject>(pack, o);
                        var overlay = new KeyboardOverlay();
                        try
                        {
                            overlay.firstKey = parseType<int>(pack, o.firstKey);
                        }
                        catch (RuntimeBinderException) { throw new Exception("firstKey missing on one of the overlay textures"); }
                        try
                        {
                            overlay.lastKey = parseType<int>(pack, o.lastKey);
                        }
                        catch (RuntimeBinderException) { throw new Exception("lastKey missing on one of the overlay textures"); }

                        try
                        {
                            overlay.overlayBelow = parseType<bool>(pack, o.belowKeyboard);
                        }
                        catch (RuntimeBinderException) { }

                        try
                        {
                            pname = parseType<string>(pack, o.texture);
                        }
                        catch (RuntimeBinderException) { throw new Exception("\"texture\" missing on one of the overlay textures"); }

                        overlay.tex = GetBitmap(pname);

                        try
                        {
                            overlay.alpha = parseType<double>(pack, o.alpha);
                        }
                        catch (RuntimeBinderException) { }

                        overlay.texAspect = overlay.tex.Width / (double)overlay.tex.Height;

                        return overlay;
                    }).ToArray();
                }
                else
                {
                    pack.OverlayTextures = new KeyboardOverlay[0];
                }
                #endregion

                #region Notes
                JArray noteSizes;
                try
                {
                    noteSizes = parseType<JArray>(pack, data.notes);
                }
                catch (RuntimeBinderException) { throw new Exception("Missing Array Property \"notes\""); }
                if (noteSizes.Count == 0) throw new Exception("Note textures array can't be 0");
                if (noteSizes.Count > 4) throw new Exception("Only up to 4 note textures are supported");

                List<NoteTexture> noteTex = new List<NoteTexture>();
                bool hasBothKeyType = false;
                foreach (dynamic _s in noteSizes)
                {
                    dynamic s = parseType<JObject>(pack, _s);
                    NoteTexture tex = new NoteTexture();
                    try
                    {
                        tex.useCaps = parseType<bool>(pack, s.useEndCaps);
                    }
                    catch (RuntimeBinderException) { throw new Exception("Missing property \"useEndCaps\" in note size textures"); }
                    try
                    {
                        tex.stretch = parseType<bool>(pack, s.alwaysStretch);
                    }
                    catch (RuntimeBinderException) { throw new Exception("Missing property \"alwaysStretch\" in note size textures"); }
                    try
                    {
                        tex.maxSize = parseType<double>(pack, s.maxSize);
                    }
                    catch (RuntimeBinderException) { throw new Exception("Missing property \"maxSize\" in note size textures"); }

                    try
                    {
                        pname = parseType<string>(pack, s.middleTexture);
                    }
                    catch (RuntimeBinderException) { throw new Exception("Missing property \"middleTexture\""); }
                    tex.noteMiddleTex = GetBitmap(pname);
                    tex.noteMiddleAspect = (double)tex.noteMiddleTex.Height / tex.noteMiddleTex.Width;

                    try
                    {
                        tex.darkenBlackNotes = parseType<double>(pack, s.darkenBlackNotes);
                    }
                    catch (RuntimeBinderException) { }

                    try
                    {
                        tex.highlightHitNotes = parseType<double>(pack, s.highlightHitNotes);
                    }
                    catch (RuntimeBinderException) { }
                    if (tex.highlightHitNotes > 1 || tex.highlightHitNotes < 0) throw new Exception("highlightHitNotes must be between 0 and 1");

                    JArray array = null;
                    try
                    {
                        var _array = parseType<JArray>(pack, s.highlightHitNotesColor);
                        if (_array != null)
                        {
                            notArray = _array.GetType() != typeof(JArray);
                            if (!notArray) array = _array;
                        }
                    }
                    catch (RuntimeBinderException) { }
                    if (array != null)
                    {
                        if (notArray) throw new Exception("highlightHitNotes must be an array of 3 numbers (RGB or RGBA, e.g. [255, 255, 255])");
                        if (!(array.Count == 3)) throw new Exception("highlightHitNotes must be an array of 3 numbers (RGB or RGBA, e.g. [255, 255, 255])");
                        else
                        {
                            tex.highlightHitNotesColor = System.Drawing.Color.FromArgb(255, (int)array[0], (int)array[1], (int)array[2]);
                        }
                    }

                    try
                    {
                        tex.squeezeEndCaps = parseType<bool>(pack, s.squeezeEndCaps);
                    }
                    catch (RuntimeBinderException) { }

                    string keyType = null;
                    try
                    {
                        keyType = parseType<string>(pack, s.keyType);
                    }
                    catch (RuntimeBinderException) { }
                    if (keyType != null) tex.keyType = strToKeyType(keyType);
                    if (tex.keyType == KeyType.Both) hasBothKeyType = true;

                    if (tex.useCaps)
                    {
                        try
                        {
                            pname = parseType<string>(pack, s.topTexture);
                        }
                        catch (RuntimeBinderException) { throw new Exception("Missing property \"topTexture\""); }
                        tex.noteTopTex = GetBitmap(pname);
                        try
                        {
                            pname = parseType<string>(pack, s.bottomTexture);
                        }
                        catch (RuntimeBinderException) { throw new Exception("Missing property \"bottomTexture\""); }
                        tex.noteBottomTex = GetBitmap(pname);
                        tex.noteTopAspect = (double)tex.noteTopTex.Height / tex.noteTopTex.Width;
                        tex.noteBottomAspect = (double)tex.noteBottomTex.Height / tex.noteBottomTex.Width;

                        try
                        {
                            tex.noteTopOversize = parseType<double>(pack, s.topOversize);
                        }
                        catch (RuntimeBinderException) { throw new Exception("Missing property \"topOversize\" in note size textures"); }
                        try
                        {
                            tex.noteBottomOversize = parseType<double>(pack, s.bottomOversize);
                        }
                        catch (RuntimeBinderException) { throw new Exception("Missing property \"bottomOversize\" in note size textures"); }
                    }
                    noteTex.Add(tex);
                }

                if (!hasBothKeyType) throw new Exception("At least one note texture required with key type of \"both\"");

                noteTex.Sort((c1, c2) =>
                {
                    if (c1.maxSize < c2.maxSize) return -1;
                    if (c1.maxSize > c2.maxSize) return 1;
                    if (c2.keyType == KeyType.Both && c1.keyType != KeyType.Both) return -1;
                    if (c1.keyType == KeyType.Both && c2.keyType != KeyType.Both) return 1;
                    return 0;
                });
                bool firstBoth = false;
                for (int i = noteTex.Count - 1; i >= 0; i--)
                {
                    if (noteTex[i].keyType == KeyType.Both)
                    {
                        if (firstBoth) break;
                        else firstBoth = true;
                    }
                    noteTex[i].maxSize = double.PositiveInfinity;
                }
                pack.NoteTextures = noteTex.ToArray();
                #endregion
            }
            catch (Exception e)
            {
                pack.error = true;
                pack.description = e.Message;
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
            return pack;
        }

        private void PluginList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadSelectedPack();
        }

        private void ReloadPackButton_Click(object sender, RoutedEventArgs e)
        {
            LoadSelectedPack();
        }

        void LoadSelectedPack()
        {
            try
            {
                var p = resourcePacks[pluginList.SelectedIndex];
                if (settings.currPack != null) UnloadPack(settings.currPack);
                var pack = LoadPack(p.filename, p.type);
                if (!pack.error)
                {
                    pluginDesc.Foreground = Brushes.Black;
                    settings.currPack = pack;
                    settings.lastPackChangeTime = DateTime.Now.Ticks;
                }
                else
                {
                    pluginDesc.Foreground = Brushes.Red;
                    settings.currPack = null;
                    settings.lastPackChangeTime = DateTime.Now.Ticks;
                }
                if (pack.preview == null)
                    previewImg.Source = null;
                else
                    previewImg.Source = BitmapToImageSource(pack.preview);
                switchTab.Visibility = Visibility.Collapsed;
                switchPanel.Children.Clear();
                if (pack.switchChoices != null && pack.switchChoices.Count != 0)
                {
                    switchTab.Visibility = Visibility.Visible;
                    foreach (var s in pack.switchChoices.Keys)
                    {
                        var menu = new ComboBox();
                        menu.Tag = s;
                        foreach (var v in pack.switchChoices[s])
                        {
                            menu.Items.Add(new ComboBoxItem() { Content = v });
                        }
                        var dock = new DockPanel();
                        dock.HorizontalAlignment = HorizontalAlignment.Left;
                        dock.Children.Add(new Label() { Content = s });
                        dock.Children.Add(menu);
                        switchPanel.Children.Add(dock);
                        menu.SelectedIndex = 0;
                        pack.switchValues[s] = pack.switchChoices[s][0];
                        menu.SelectionChanged += Menu_SelectionChanged;
                    }
                }
                pluginDesc.Text = pack.description;
            }
            catch { }
        }

        private void Menu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (settings.currPack != null)
            {
                var p = resourcePacks[pluginList.SelectedIndex];
                var choices = settings.currPack.switchChoices;
                var vals = settings.currPack.switchValues;

                var box = (ComboBox)sender;
                var tag = (string)box.Tag;
                vals[tag] = choices[tag][box.SelectedIndex];

                UnloadPack(settings.currPack);
                var pack = LoadPack(p.filename, p.type, vals, choices);
                if (!pack.error)
                {
                    pluginDesc.Text = pack.description;
                    pluginDesc.Foreground = Brushes.Black;
                    settings.currPack = pack;
                    settings.lastPackChangeTime = DateTime.Now.Ticks;
                }
                else
                {
                    pluginDesc.Text = pack.description;
                    pluginDesc.Foreground = Brushes.Red;
                    settings.currPack = null;
                    settings.lastPackChangeTime = DateTime.Now.Ticks;
                }
                if (pack.preview == null)
                    previewImg.Source = null;
                else
                    previewImg.Source = BitmapToImageSource(pack.preview);
            }
        }

        public void SetValues()
        {
            firstNote.Value = settings.firstNote;
            lastNote.Value = settings.lastNote - 1;
            noteDeltaScreenTime.Value = Math.Log(settings.deltaTimeOnScreen, 2);
            blackNotesAbove.IsChecked = settings.blackNotesAbove;
            paletteList.SelectImage(settings.palette);
        }

        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            ReloadPacks();
        }

        bool screenTimeLock = false;
        private void ScreenTime_nud_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!inited) return;
            try
            {
                if (screenTimeLock) return;
                screenTimeLock = true;
                noteDeltaScreenTime.Value = Math.Log((double)screenTime_nud.Value, 2);
                settings.deltaTimeOnScreen = (double)screenTime_nud.Value;
                screenTimeLock = false;
            }
            catch
            {
                screenTimeLock = false;
            }
        }

        private void BlackNotesAbove_Checked(object sender, RoutedEventArgs e)
        {
            if (!inited) return;
            try
            {
                settings.blackNotesAbove = (bool)blackNotesAbove.IsChecked;
            }
            catch (NullReferenceException) { }
        }

        private void NoteDeltaScreenTime_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!inited) return;
            try
            {
                if (screenTimeLock) return;
                screenTimeLock = true;
                settings.deltaTimeOnScreen = Math.Pow(2, noteDeltaScreenTime.Value);
                screenTime_nud.Value = (decimal)settings.deltaTimeOnScreen;
                screenTimeLock = false;
            }
            catch (NullReferenceException)
            {
                screenTimeLock = false;
            }
        }

        private void Nud_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!inited) return;
            try
            {
                if (sender == firstNote) settings.firstNote = (int)firstNote.Value;
                if (sender == lastNote) settings.lastNote = (int)lastNote.Value + 1;
                if (sender == noteDeltaScreenTime) settings.deltaTimeOnScreen = (int)noteDeltaScreenTime.Value;
            }
            catch (NullReferenceException) { }
            catch (InvalidOperationException) { }
        }
    }
}
