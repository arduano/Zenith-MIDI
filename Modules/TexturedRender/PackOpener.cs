using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZenithEngine.IO;
using ZenithEngine.DXHelper;
using System.Drawing;
using Newtonsoft.Json.Linq;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;

namespace TexturedRender
{
    class OpenerException : Exception
    {
        public OpenerException(string message) : base(message) { }
    };

    static class PackOpener
    {
        public static Pack Load(string name, DirectoryFolder folder, Dictionary<string, string> switches = null, Dictionary<string, string[]> assertSwitches = null)
        {
            var pack = new Pack() { Name = name };
            try
            {
                var jsons = folder.FindByFilename("pack.json");
                if (jsons.Length == 0) throw new OpenerException("Could not find pack.json file");
                string json = folder.ReadAllText(jsons[0]);
                string pbase = Path.GetDirectoryName(jsons[0]);

                dynamic data;
                JObject jdata;
                try
                {
                    jdata = (JObject)JsonConvert.DeserializeObject(json);
                    data = (dynamic)jdata;
                }
                catch { throw new OpenerException("Corrupt json in pack.json"); }

                TextureShaderType strToShader(string s)
                {
                    if (s == "normal") return TextureShaderType.Normal;
                    if (s == "inverse") return TextureShaderType.Inverted;
                    if (s == "hybrid") return TextureShaderType.Hybrid;
                    throw new OpenerException("Unknown shader type \"" + s + "\"");
                }

                KeyType strToKeyType(string s)
                {
                    if (s == "black") return KeyType.Black;
                    if (s == "white") return KeyType.White;
                    if (s == "both") return KeyType.Both;
                    throw new OpenerException("Unknown key type \"" + s + "\"");
                }

                RenderTexture getImage(string path)
                {
                    path = Path.Combine(pbase, path);
                    Stream s;
                    try
                    {
                        s = folder.OpenStream(path);
                    }
                    catch { throw new OpenerException("Could not open " + path); }
                    RenderTexture b;
                    try
                    {
                        var bitmap = new Bitmap(s);
                        b = RenderTexture.FromBitmap(bitmap);
                        bitmap.Dispose();
                    }
                    catch { throw new OpenerException("Corrupt image: " + path); }
                    s.Close();
                    return b;
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
                            throw new OpenerException("value " + o.ToString() + " can't be converted to type " + typeof(T).ToString());
                        }
                    }

                    if (!pack.SwitchValues.ContainsKey(switchName))
                    {
                        throw new OpenerException("switch name not found: " + switchName);
                    }

                    dynamic _o;
                    try
                    {
                        _o = ((JObject)o).GetValue(pack.SwitchValues[switchName]);
                    }
                    catch
                    {
                        throw new OpenerException("value " + pack.SwitchValues[switchName] + " not found on a switch");
                    }

                    try
                    {
                        return parseType<T>(pack, _o);
                    }
                    catch
                    {
                        throw new OpenerException("value " + _o.ToString() + " can't be converted to type " + typeof(T).ToString());
                    }
                }

                RenderTexture fetchImage(JObject obj, string path)
                {
                    try
                    {
                        return getImage(parseType<string>(pack, obj.GetValue(path)));
                    }
                    catch (RuntimeBinderException)
                    {
                        throw new OpenerException($"Missing property \"{path}\"");
                    }
                }

                RenderTexture tryFetchImage(JObject obj, string path)
                {
                    try
                    {
                        return getImage(parseType<string>(pack, obj.GetValue(path)));
                    }
                    catch (RuntimeBinderException)
                    {
                        return null;
                    }
                }

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
                    if (!swIsArray) throw new OpenerException("switches must be an array");
                    foreach (dynamic s in sw)
                    {
                        string swName = null;
                        List<string> swVals = new List<string>();
                        dynamic swValArr;
                        swName = s.name;
                        if (swName == null)
                        {
                            string titleText = s.text;
                            if (titleText == null)
                                throw new OpenerException("missing property 'name' or 'text' on switch");
                            else
                            {
                                pack.SwitchOrder.Add(titleText);
                                continue;
                            }
                        }
                        pack.SwitchOrder.Add(swName);
                        swValArr = s.values;
                        if (swValArr == null) throw new OpenerException("missing property 'values' on switch");
                        if (swValArr.GetType() != typeof(JArray)) throw new OpenerException("'values' must be a string array");
                        if (((JArray)swValArr).Count == 0) throw new OpenerException("'values' array must have at least 1 item");
                        foreach (dynamic v in (JArray)swValArr)
                        {
                            swVals.Add((string)v);
                        }
                        pack.SwitchChoices.Add(swName, swVals.ToArray());
                        if (assertSwitches != null)
                            if (!assertSwitches.ContainsKey(swName) || !swVals.SequenceEqual(assertSwitches[swName]))
                            {
                                throw new OpenerException("switches have been changed in pack.json, please reload pack");
                            }
                        pack.SwitchValues.Add(swName, swVals[0]);
                    }
                    if (assertSwitches != null)
                        if (pack.SwitchValues.Count != assertSwitches.Count)
                            throw new OpenerException("switches have been changed in pack.json, please reload pack");
                        else
                            pack.SwitchValues = switches;
                }
                #endregion

                #region Misc
                try
                {
                    pack.KeyboardHeight = parseType<double>(pack, data.keyboardHeight) / 100;
                }
                catch (RuntimeBinderException) { }
                try
                {
                    pack.SameWidthNotes = parseType<bool>(pack, data.sameWidthNotes);
                }
                catch (RuntimeBinderException) { }
                try
                {
                    pack.BlackKeysFullOctave = parseType<bool>(pack, data.blackKeysFullOctave);
                }
                catch (RuntimeBinderException) { }
                try
                {
                    pack.WhiteKeysFullOctave = parseType<bool>(pack, data.whiteKeysFullOctave);
                }
                catch (RuntimeBinderException) { }
                try
                {
                    pack.BlackKeyHeight = parseType<double>(pack, data.blackKeyHeight) / 100;
                }
                catch (RuntimeBinderException) { }
                try
                {
                    pack.BlackKeyDefaultWhite = parseType<bool>(pack, data.blackKeysWhiteShade);
                }
                catch (RuntimeBinderException) { }
                try
                {
                    pack.LinearScaling = parseType<bool>(pack, data.linearTextureScaling);
                }
                catch (RuntimeBinderException) { }
                #endregion

                #region Get Keys
                pack.BlackKeyTex = fetchImage(jdata, "blackKey");
                pack.BlackKeyPressedTex = fetchImage(jdata, "blackKeyPressed");
                pack.WhiteKeyTex = fetchImage(jdata, "whiteKey");
                pack.WhiteKeyPressedTex = fetchImage(jdata, "whiteKeyPressed");

                pack.WhiteKeyLeftTex = tryFetchImage(jdata, "whiteKeyLeft");
                pack.WhiteKeyPressedLeftTex = tryFetchImage(jdata, "whiteKeyLeftPressed");
                pack.WhiteKeyRightTex = tryFetchImage(jdata, "whiteKeyRight");
                pack.WhiteKeyPressedRightTex = tryFetchImage(jdata, "whiteKeyRightPressed");

                if ((pack.WhiteKeyLeftTex == null) ^ (pack.WhiteKeyPressedLeftTex == null))
                    if (pack.WhiteKeyLeftTex == null)
                        throw new OpenerException("whiteKeyLeft is incliuded while whiteKeyLeftPressed is missing. Include or remove both.");
                    else
                        throw new OpenerException("whiteKeyLeftPressed is incliuded while whiteKeyLeft is missing. Include or remove both.");

                if ((pack.WhiteKeyRightTex == null) ^ (pack.WhiteKeyPressedRightTex == null))
                    if (pack.WhiteKeyRightTex == null)
                        throw new OpenerException("whiteKeyRight is incliuded while whiteKeyRightPressed is missing. Include or remove both.");
                    else
                        throw new OpenerException("whiteKeyRightPressed is incliuded while whiteKeyRight is missing. Include or remove both.");
                #endregion

                #region Oversizes
                try
                {
                    pack.WhiteKeyOversize = parseType<double>(pack, data.whiteKeyOversize) / 100;
                }
                catch (RuntimeBinderException) { }
                try
                {
                    pack.BlackKeyOversize = parseType<double>(pack, data.blackKeyOversize) / 100;
                }
                catch (RuntimeBinderException) { }
                try
                {
                    pack.WhiteKeyPressedOversize = parseType<double>(pack, data.whiteKeyPressedOversize) / 100;
                }
                catch (RuntimeBinderException) { }
                try
                {
                    pack.BlackKeyPressedOversize = parseType<double>(pack, data.blackKeyPressedOversize) / 100;
                }
                catch (RuntimeBinderException) { }
                #endregion

                #region Black Key Sizes
                try
                {
                    pack.BlackKey2setOffset = parseType<double>(pack, data.blackKey2setOffset);
                }
                catch (RuntimeBinderException) { }
                try
                {
                    pack.BlackKey3setOffset = parseType<double>(pack, data.blackKey3setOffset);
                }
                catch (RuntimeBinderException) { }
                try
                {
                    pack.BlackKeyScale = parseType<double>(pack, data.blackKeyScale);
                }
                catch (RuntimeBinderException) { }
                try
                {
                    pack.BlackNote2setOffset = parseType<double>(pack, data.blackNote2setOffset);
                }
                catch (RuntimeBinderException) { }
                try
                {
                    pack.BlackNote3setOffset = parseType<double>(pack, data.blackNote3setOffset);
                }
                catch (RuntimeBinderException) { }
                try
                {
                    pack.BlackNoteScale = parseType<double>(pack, data.blackNoteScale);
                }
                catch (RuntimeBinderException) { }
                try
                {
                    JArray offsets = parseType<JArray>(pack, data.advancedBlackKeyOffsets);
                    if (offsets.Count != 5) throw new OpenerException("advancedBlackKeyOffsets must have 5 elements");
                    pack.AdvancedBlackKeyOffsets = offsets.Select(s => (double)s).ToArray();
                }
                catch (RuntimeBinderException) { }
                try
                {
                    JArray offsets = parseType<JArray>(pack, data.advancedBlackKeySizes);
                    if (offsets.Count != 5) throw new OpenerException("advancedBlackKeySizes must have 5 elements");
                    pack.AdvancedBlackKeySizes = offsets.Select(s => (double)s).ToArray();
                }
                catch (RuntimeBinderException) { }
                #endregion

                #region Bar
                pack.BarTex = tryFetchImage(jdata, "bar");
                pack.UseBar = pack.BarTex != null;
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
                    if (notArray) throw new OpenerException("overlays must be an array");
                    pack.OverlayTextures = overlaysArray.Select((JToken token) =>
                    {
                        JObject jo;
                        try
                        {
                            jo = (JObject)token;
                        }
                        catch { throw new OpenerException("Overlays array must only contain objects"); }
                        dynamic o = jo;
                        o = parseType<JObject>(pack, o);
                        var overlay = new KeyboardOverlay();
                        try
                        {
                            overlay.FirstKey = parseType<int>(pack, o.firstKey);
                        }
                        catch (RuntimeBinderException) { throw new OpenerException("firstKey missing on one of the overlay textures"); }
                        try
                        {
                            overlay.LastKey = parseType<int>(pack, o.lastKey);
                        }
                        catch (RuntimeBinderException) { throw new OpenerException("lastKey missing on one of the overlay textures"); }

                        try
                        {
                            overlay.OverlayBelow = parseType<bool>(pack, o.belowKeyboard);
                        }
                        catch (RuntimeBinderException) { }

                        overlay.Texture = tryFetchImage(jo, "texture");

                        try
                        {
                            overlay.Alpha = parseType<double>(pack, o.alpha);
                        }
                        catch (RuntimeBinderException) { }

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
                catch (RuntimeBinderException) { throw new OpenerException("Missing Array Property \"notes\""); }
                if (noteSizes.Count == 0) throw new OpenerException("Note textures array can't be 0");
                if (noteSizes.Count > 4) throw new OpenerException("Only up to 4 note textures are supported");

                List<NoteTexture> noteTex = new List<NoteTexture>();
                bool hasBothKeyType = false;
                foreach (dynamic _s in noteSizes)
                {
                    JObject js = parseType<JObject>(pack, _s);
                    dynamic s = js;
                    NoteTexture tex = new NoteTexture();
                    try
                    {
                        tex.UseCaps = parseType<bool>(pack, s.useEndCaps);
                    }
                    catch (RuntimeBinderException) { throw new OpenerException("Missing property \"useEndCaps\" in note size textures"); }
                    try
                    {
                        tex.Stretch = parseType<bool>(pack, s.alwaysStretch);
                    }
                    catch (RuntimeBinderException) { throw new OpenerException("Missing property \"alwaysStretch\" in note size textures"); }
                    try
                    {
                        tex.MaxSize = parseType<double>(pack, s.maxSize);
                    }
                    catch (RuntimeBinderException) { throw new OpenerException("Missing property \"maxSize\" in note size textures"); }

                    tex.NoteMiddleTex = fetchImage(js, "middleTexture");

                    try
                    {
                        tex.DarkenBlackNotes = parseType<double>(pack, s.darkenBlackNotes);
                    }
                    catch (RuntimeBinderException) { }

                    try
                    {
                        tex.HighlightHitNotes = parseType<double>(pack, s.highlightHitNotes);
                    }
                    catch (RuntimeBinderException) { }
                    if (tex.HighlightHitNotes > 1 || tex.HighlightHitNotes < 0) throw new OpenerException("highlightHitNotes must be between 0 and 1");

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
                        if (notArray) throw new OpenerException("highlightHitNotes must be an array of 3 numbers (RGB or RGBA, e.g. [255, 255, 255])");
                        if (!(array.Count == 3)) throw new OpenerException("highlightHitNotes must be an array of 3 numbers (RGB or RGBA, e.g. [255, 255, 255])");
                        else
                        {
                            tex.HighlightHitNotesColor = System.Drawing.Color.FromArgb(255, (int)array[0], (int)array[1], (int)array[2]);
                        }
                    }

                    try
                    {
                        tex.SqueezeEndCaps = parseType<bool>(pack, s.squeezeEndCaps);
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

                    if (tex.UseCaps)
                    {
                        tex.NoteTopTex = fetchImage(js, "topTexture");
                        tex.NoteBottomTex = fetchImage(js, "bottomTexture");

                        try
                        {
                            tex.NoteTopOversize = parseType<double>(pack, s.topOversize);
                        }
                        catch (RuntimeBinderException) { throw new OpenerException("Missing property \"topOversize\" in note size textures"); }
                        try
                        {
                            tex.NoteBottomOversize = parseType<double>(pack, s.bottomOversize);
                        }
                        catch (RuntimeBinderException) { throw new OpenerException("Missing property \"bottomOversize\" in note size textures"); }
                    }
                    noteTex.Add(tex);
                }

                if (!hasBothKeyType) throw new OpenerException("At least one note texture required with key type of \"both\"");

                noteTex.Sort((c1, c2) =>
                {
                    if (c1.MaxSize < c2.MaxSize) return -1;
                    if (c1.MaxSize > c2.MaxSize) return 1;
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
                    noteTex[i].MaxSize = double.PositiveInfinity;
                }
                pack.NoteTextures = noteTex.ToArray();
                #endregion
            }
            catch (OpenerException e)
            {
                pack.Error = true;
                pack.Description = e.Message;
            }
            catch (Exception e)
            {
                pack.Error = true;
                pack.Description = "An error occured:\n" + e.Message;
            }
            finally
            {
                folder.Dispose();
            }

            return pack;
        }
    }
}
