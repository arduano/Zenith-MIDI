using Newtonsoft.Json.Linq;
using OpenTK.Input;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ZenithEngine.DXHelper;
using ZenithEngine.IO;

namespace TexturedRender
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    sealed class Required : Attribute
    {
        public Required()
        { }
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    sealed class RequiredWith : Attribute
    {
        public string[] Others { get; }
        public RequiredWith(params string[] others)
        {
            Others = others;
        }
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    sealed class WithLength : Attribute
    {
        public int Length { get; }
        public WithLength(int length)
        {
            Length = length;
        }
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    sealed class RequiredIf : Attribute
    {
        public RequiredIf(string field, object value)
        {
            Field = field;
            Value = value;
        }

        public string Field { get; }
        public object Value { get; }
    }

    public class SchemaLoadable : DeviceInitiable
    {
        public static string FormatError(Exception e)
        {
            if (e is OpenerException op)
                return op.Message;
            if (e is TargetInvocationException t)
                return FormatError(t.InnerException);
            else
                return e.ToString();
        }

        static BitmapImage BitmapToImageSource(Bitmap bitmap)
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
        public SchemaLoadable(string basePath, JObject obj, DirectoryFolder folder, string folderBasePath, Dictionary<string, SwitchItem> switches = null)
        {
            Dictionary<Type, Func<JToken, object>> valParsers = new Dictionary<Type, Func<JToken, object>>
            {
                { typeof(string), t => (string)(dynamic)t },
                { typeof(int), t => (int)(dynamic)t },
                { typeof(float), t => (float)(dynamic)t },
                { typeof(double), t => (double)(dynamic)t },
                { typeof(bool), t => (bool)(dynamic)t },
            };

            Bitmap openBitmap(string path)
            {
                try
                {
                    using (var s = folder.OpenStream(Path.Combine(folderBasePath, path)))
                        return new Bitmap(s);
                }
                catch (IOException e)
                {
                    throw new OpenerException($"Could not find file {path}");
                }
                catch
                {
                    throw new OpenerException($"Could not load file {path}");
                }
            }

            valParsers.Add(typeof(RenderTexture), t =>
            {
                var path = (string)valParsers[typeof(string)](t);
                using (var bmp = openBitmap(path))
                    return init.Add(RenderTexture.FromBitmap(bmp));
            });

            valParsers.Add(typeof(ImageSource), t =>
            {
                var path = (string)valParsers[typeof(string)](t);
                using (var bmp = openBitmap(path))
                    return BitmapToImageSource(bmp);
            });

            valParsers.Add(typeof(NoteShaderType), t =>
            {
                var text = (string)valParsers[typeof(string)](t);
                switch (text.ToLower())
                {
                    case "normal": return NoteShaderType.Normal;
                    case "inverse": return NoteShaderType.Inverted;
                    case "hybrid": return NoteShaderType.Hybrid;
                    default:
                        throw new OpenerException(
                   $"\"{text}\" is not a valid shader type. Valid shader types are \"normal\", \"inverse\" and \"hybrid\""
               );
                }
            });

            valParsers.Add(typeof(NoteType), t =>
            {
                var text = (string)valParsers[typeof(string)](t);
                switch (text.ToLower())
                {
                    case "both": return NoteType.Both;
                    case "black": return NoteType.Black;
                    case "white": return NoteType.White;
                    default:
                        throw new OpenerException(
                   $"\"{text}\" is not a valid shader type. Valid shader types are \"black\", \"white\" and \"both\""
               );
                }
            });

            var type = this.GetType();
            var fields = type.GetFields();
            foreach (var f in fields)
            {
                var path = basePath + f.Name;
                if (!obj.ContainsKey(f.Name))
                {
                    if (Attribute.IsDefined(f, typeof(Required)))
                    {
                        throw new OpenerException($"Field {path} is required");
                    }
                    if (Attribute.IsDefined(f, typeof(RequiredWith)))
                    {
                        var req = (RequiredWith)Attribute.GetCustomAttribute(f, typeof(RequiredWith));
                        foreach (var r in req.Others)
                        {
                            if (obj.ContainsKey(r))
                            {
                                throw new OpenerException($"Field {basePath}{r} is required if {path} is set");
                            }
                        }
                    }
                    continue;
                }

                object readField(JToken item, Type ftype, string fpath = null)
                {
                    if (fpath == null) fpath = path;
                    if (item.Type == JTokenType.Object)
                    {
                        var obj = (JObject)item;
                        if (obj.ContainsKey("_switch"))
                        {
                            if (switches == null)
                                throw new OpenerException($"Switches are not allowed on field {fpath}");
                            var sw = (string)readField(obj["_switch"], typeof(string), fpath + "_switch.");
                            if (!switches.ContainsKey(sw))
                                throw new OpenerException($"Switch with the name {sw} not found on field {fpath}");
                            var swv = switches[sw];
                            if (!obj.ContainsKey(swv.SelectedValue))
                            {
                                return null;
                                //if(!obj.ContainsKey(swv.Values[0]))
                                //    throw new OpenerException($"Value {swv.SelectedValue} and default value {swv.Values[0]} missing missing for switch {sw} on field {fpath}");
                                //return readField(obj[swv.Values[0]], ftype, fpath + swv.Values[0] + ".");
                            }
                            return readField(obj[swv.SelectedValue], ftype, fpath + swv.SelectedValue + ".");
                        }
                    }

                    if (typeof(SchemaLoadable).IsAssignableFrom(ftype))
                    {
                        if (item.Type != JTokenType.Object)
                            throw new OpenerException($"Field {fpath} must be an object, instead got {item.Type}");
                        var instance = (SchemaLoadable)Activator.CreateInstance(ftype, new object[] { fpath, (JObject)item, folder, folderBasePath, switches });
                        return init.Add(instance);
                    }

                    if (ftype == typeof(JObject))
                        if (item.Type != JTokenType.Array)
                            throw new OpenerException($"Field {fpath} must be an array, instead got {item.Type}");

                    try
                    {
                        if (valParsers.ContainsKey(ftype))
                        {
                            return valParsers[ftype](item);
                        }
                    }
                    catch (OpenerException e)
                    {
                        throw new OpenerException($"Failed to load {fpath} because:\n{e.Message}");
                    }
                    catch
                    {
                        throw new OpenerException($"Could not convert field {fpath} to type {ftype}");
                    }

                    return item;
                }

                var token = obj[f.Name];

                if (typeof(Array).IsAssignableFrom(f.FieldType))
                {
                    var arr = (JArray)readField(token, typeof(JArray));
                    if (arr == null) continue;

                    if (Attribute.IsDefined(f, typeof(WithLength)))
                    {
                        var len = (WithLength)Attribute.GetCustomAttribute(f, typeof(WithLength));
                        if (arr.Count != len.Length)
                            throw new OpenerException($"Array on field {path} must be {len.Length} items long");
                    }

                    var elType = f.FieldType.GetElementType();
                    var newArr = Activator.CreateInstance(f.FieldType, arr.Count);
                    var a = (Array)newArr;
                    for (int i = 0; i < arr.Count; i++)
                    {
                        var val = readField(arr[i], elType, path + $"[{i}].");
                        if (val == null)
                            throw new OpenerException($"Can't have null in array {path}[{i}]");
                        a.SetValue(val, i);
                    }
                    f.SetValue(this, newArr);
                }
                else if (typeof(SchemaLoadable).IsAssignableFrom(f.FieldType))
                {
                    var val = readField(token, f.FieldType);
                    if (val != null)
                        f.SetValue(this, val);
                }
                else
                {
                    var val = readField(token, f.FieldType);
                    if (val == null) continue;
                    try
                    {
                        f.SetValue(this, val);
                    }
                    catch
                    {
                        throw new OpenerException($"Could not set value of {path}");
                    }
                }
            }

            foreach (var f in fields.Where(f => Attribute.IsDefined(f, typeof(RequiredIf))))
            {
                var ri = (RequiredIf)Attribute.GetCustomAttribute(f, typeof(RequiredIf));
                var check = fields.Where(f => f.Name == ri.Field).First();
                var val = check.GetValue(this);
                if (val.Equals(ri.Value) && !obj.ContainsKey(f.Name))
                    throw new OpenerException($"{basePath}{f.Name} is required if {basePath}{ri.Field} is {ri.Value}");
            }
        }
    }
}
