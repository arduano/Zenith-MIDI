using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using ZenithEngine;
using ZenithEngine.DXHelper;
using ZenithEngine.MIDI;
using ZenithEngine.Modules;
using Direct2D1 = SharpDX.Direct2D1;
using DXGI = SharpDX.DXGI;
using DirectWrite = SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
using ZenithEngine.DXHelper.Presets;
using ZenithEngine.DXHelper.D2D;
using OpenTK.Graphics;
using System.Text.RegularExpressions;
using ZenithEngine.ModuleUI;

namespace NoteCountRender
{
    struct DecimalFormat
    {
        public DecimalFormat(int minDigits, int minDecimals, int round, int floor = 0, int ceiling = 0, string thousands = "", string point = ".")
        {
            MinDigits = minDigits;
            MinDecimals = minDecimals;
            Round = round;
            Floor = floor;
            Ceiling = ceiling;
            Thousands = thousands;
            Point = point;
        }

        public int MinDigits { get; }
        public int MinDecimals { get; }
        public int Round { get; }
        public int Floor { get; }
        public int Ceiling { get; }

        public string Thousands { get; }
        public string Point { get; }
    }

    struct NPSFrame
    {
        public NPSFrame(double time, long notes)
        {
            Time = time;
            Notes = notes;
        }

        public double Time { get; }
        public long Notes { get; }
    }

    public class Render : PureModule
    {
        #region Info
        public override string Name { get; } = "Note Count";
        public override string Description { get; } = "blah blah blah";
        public override ImageSource PreviewImage { get; } = LoadPreviewBitmap(Properties.Resources.preview);
        #endregion

        public override double StartOffset => 0;

        protected override NoteColorPalettePick PalettePicker => null;

        SettingsCtrl settingsView = LoadUI(() => new SettingsCtrl());
        BaseModel Data => settingsView.Data;
        public override ISerializableContainer SettingsControl => settingsView;

        public Render()
        {
            compositor = init.Add(new Compositor());
            plainShader = init.Add(Shaders.BasicTextured());
        }

        Dictionary<string, object> EndProgressRemaining(string name, object end, object progress, object remaining = null)
        {
            if (end is int)
            {
                progress = Math.Min(Convert.ToInt32(progress), Convert.ToInt32(end));
                remaining = Convert.ToInt32(end) - Convert.ToInt32(progress);
            }
            else if (end is long)
            {
                progress = Math.Min(Convert.ToInt64(progress), Convert.ToInt64(end));
                remaining = Convert.ToInt64(end) - Convert.ToInt64(progress);
            }
            else if (end is decimal)
            {
                progress = Math.Min(Convert.ToDecimal(progress), Convert.ToDecimal(end));
                remaining = Convert.ToDecimal(end) - Convert.ToDecimal(progress);
            }
            else if (end is float)
            {
                progress = Math.Min(Convert.ToSingle(progress), Convert.ToSingle(end));
                remaining = Convert.ToSingle(end) - Convert.ToSingle(progress);
            }
            else if (end is double)
            {
                progress = Math.Min(Convert.ToDouble(progress), Convert.ToDouble(end));
                remaining = Convert.ToDouble(end) - Convert.ToDouble(progress);
            }
            return new Dictionary<string, object>()
            {
                { $"{name}", progress },
                { $"{name}-curr", progress },
                { $"{name}-max", end },
                { $"{name}-rem", remaining },
            };
        }

        int GetValueForPart(IEnumerable<string> parts, string name, int fallback)
        {
            var n = name + ":";
            var converted = parts
                .Where(p => p.StartsWith(n))
                .Select(p => p.Substring(name.Length + 1))
                .Select(p => Convert.ToInt32(p));

            foreach (var p in converted) return p;
            return fallback;
        }

        string GetValueForPart(IEnumerable<string> parts, string name, string fallback)
        {
            var n = name + ":";
            var converted = parts
                .Where(p => p.StartsWith(n))
                .Select(p => p.Substring(name.Length));

            foreach (var p in converted) return p;
            return fallback;
        }


        string ProcessDecimal(decimal val, DecimalFormat format) =>
            ProcessDecimal(val, format.Round, format.Floor, format.Ceiling, format.MinDecimals, format.MinDecimals, format.Thousands, format.Point);
        string ProcessDecimal(decimal val, int round, int floor, int ceiling, int minDecimals, int minDigits, string thousands, string point)
        {
            string fixString(string s)
            {
                if (s.ToLower() == "space") return " ";
                if (s.ToLower() == "none") return "";
                return s;
            }

            string genZeros(int count)
            {
                return new string('0', count);
            }

            IEnumerable<string> splitThousands(string s)
            {
                while (s.Length > 0)
                {
                    int c = s.Length % 3;
                    if (c == 0) c = 3;
                    yield return s.Substring(0, c);
                    s = s.Substring(c);
                }
            }

            thousands = fixString(thousands);
            point = fixString(point);

            var txt = Decimal.Round(val, round).ToString();
            if (floor > 0) txt = (Decimal.Floor(val * Convert.ToDecimal(Math.Pow(10, floor))) / Convert.ToDecimal(Math.Pow(10, floor))).ToString();
            if (ceiling > 0) txt = (Decimal.Ceiling(val * Convert.ToDecimal(Math.Pow(10, ceiling))) / Convert.ToDecimal(Math.Pow(10, ceiling))).ToString();

            var split = txt.Split('.');
            string digits = split[0];
            string decimals = split.Length == 1 ? "" : split[1];

            if (minDecimals > decimals.Length)
            {
                decimals += genZeros(minDecimals - decimals.Length);
            }
            if (minDigits > digits.Length)
            {
                digits = genZeros(minDigits - digits.Length) + digits;
            }

            if (thousands != "") digits = string.Join(thousands, splitThousands(digits));

            if (decimals.Length == 0) return digits;
            return digits + point + decimals;
        }

        Dictionary<string, object> GetWithMaximum(string key, object curr)
        {
            if (!maximumVals.ContainsKey(key))
            {
                maximumVals.Add(key, curr);
            }
            else
            {
                var val = maximumVals[key];
                if (curr is int) maximumVals[key] = Math.Max((int)curr, (int)val);
                else if (curr is double) maximumVals[key] = Math.Max((double)curr, (double)val);
                else if (curr is decimal) maximumVals[key] = Math.Max((decimal)curr, (decimal)val);
                else if (curr is long) maximumVals[key] = Math.Max((long)curr, (long)val);
                else if (curr is float) maximumVals[key] = Math.Max((float)curr, (float)val);
            }

            return new Dictionary<string, object>() {
                { key, curr },
                { key + "-max", maximumVals[key] },
            };
        }

        CompositeRenderSurface composite;
        Compositor compositor;
        ShaderProgram plainShader;

        InterlopRenderTarget2D target2d;
        SolidColorBrushKeeper brush;
        TextFormatKeeper textFormat;

        public override void Init(DeviceGroup device, MidiPlayback midi, RenderStatus status)
        {
            var state = Data.State;

            init.Replace(ref composite, new CompositeRenderSurface(status.RenderWidth, status.RenderHeight));
            init.Replace(ref target2d, new InterlopRenderTarget2D(composite));
            init.Replace(ref brush, new SolidColorBrushKeeper(target2d, new Color4(255, 255, 255, 255)));
            init.Replace(ref textFormat, new TextFormatKeeper(state.FontName, state.FontSize * status.SSAA));

            countedNotes = 0;
            maximumVals = new Dictionary<string, object>();
            npsFrames.Clear();

            base.Init(device, midi, status);
        }

        Regex search = new Regex(@"{.+?}", RegexOptions.Singleline);

        long countedNotes = 0;
        Dictionary<string, object> maximumVals = new Dictionary<string, object>();
        List<NPSFrame> npsFrames = new List<NPSFrame>();

        public override void RenderFrame(DeviceContext context, IRenderSurface renderSurface)
        {
            var state = Data.State;
            if (
                textFormat.FontFamily != state.FontName ||
                textFormat.FontSize != state.FontSize * Status.SSAA
                )
            {
                init.Replace(ref textFormat, new TextFormatKeeper(state.FontName, state.FontSize * Status.SSAA));
            }

            var data = new List<Dictionary<string, object>>();

            long polyphony = 0;
            long notesHit = 0;

            Midi.CheckParseDistance(0);
            var time = Midi.PlayerPosition;
            foreach (var n in Midi.IterateNotesCustomDelete())
            {
                if (n.Start > time) continue;
                polyphony++;
                if (n.Meta == null)
                {
                    notesHit++;
                    n.Meta = true;
                }
                if (n.HasEnded && n.End < time) n.Delete = true;
            }
            countedNotes += notesHit;

            var stime = Midi.PlayerPositionSeconds;
            while (npsFrames.Count != 0 && npsFrames[0].Time < stime - 2)
                npsFrames.RemoveAt(0);
            npsFrames.Add(new NPSFrame(Midi.PlayerPositionSeconds, notesHit));

            long nps2 = npsFrames.Select(n => n.Notes).Sum() / 2;
            long nps1 = npsFrames.Where(n => n.Time > stime - 1).Select(n => n.Notes).Sum();
            long nps05 = npsFrames.Where(n => n.Time > stime - 0.5).Select(n => n.Notes).Sum() * 2;
            long nps025 = npsFrames.Where(n => n.Time > stime - 0.25).Select(n => n.Notes).Sum() * 4;

            string[] ScaleAbbTableMajor = { "Cb", "Gb", "Db", "Ab", "Eb", "Bb", "F", "C", "G", "D", "A", "E", "B", "F#", "C#" };
            string[] ScaleAbbTableMinor = { "Ab", "Eb", "B", "F", "C", "G", "D", "A", "E", "B", "F#", "C#", "G#", "D#", "A#"};
            string ScaleAbb = Midi.Scale.Ifminor == false ? ScaleAbbTableMajor[Midi.Scale.SFNum + 7] : ScaleAbbTableMinor[Midi.Scale.SFNum + 7] + "m";
            string ScaleName = Midi.Scale.Ifminor == false ? ScaleAbbTableMajor[Midi.Scale.SFNum + 7] + "Major" : ScaleAbbTableMinor[Midi.Scale.SFNum + 7] + " minor";

            data.Add(GetWithMaximum("plph", polyphony));
            data.Add(GetWithMaximum("nps", nps1));
            data.Add(GetWithMaximum("nps-1", nps1));
            data.Add(GetWithMaximum("nps-2", nps2));
            data.Add(GetWithMaximum("nps-05", nps05));
            data.Add(GetWithMaximum("nps-025", nps025));
            data.Add(EndProgressRemaining("nc", Midi.Midi.NoteCount, countedNotes));
            data.Add(EndProgressRemaining("sec", Midi.Midi.SecondsLength, Midi.PlayerPositionSeconds));
            data.Add(EndProgressRemaining("time",
                                          TimeSpan.FromSeconds(Midi.Midi.SecondsLength).ToString("mm\\:ss"),
                                          TimeSpan.FromSeconds(Math.Min(Midi.Midi.SecondsLength, Midi.PlayerPositionSeconds)).ToString("mm\\:ss"),
                                          TimeSpan.FromSeconds(Math.Max(0, Midi.Midi.SecondsLength - Midi.PlayerPositionSeconds)).ToString("mm\\:ss")));
            data.Add(EndProgressRemaining("time-milli",
                                          TimeSpan.FromSeconds(Midi.Midi.SecondsLength).ToString("mm\\:ss\\.fff"),
                                          TimeSpan.FromSeconds(Math.Min(Midi.Midi.SecondsLength, Midi.PlayerPositionSeconds)).ToString("mm\\:ss\\.fff"),
                                          TimeSpan.FromSeconds(Math.Max(0, Midi.Midi.SecondsLength - Midi.PlayerPositionSeconds)).ToString("mm\\:ss\\.fff")));
            data.Add(EndProgressRemaining("tick", Midi.Midi.TickLength, Midi.PlayerPosition));
            data.Add(new Dictionary<string, object>() { { "avgnps", Midi.Midi.NoteCount / Midi.Midi.SecondsLength },
                                                        { "avgnps-live", countedNotes / Math.Min(Midi.PlayerPositionSeconds, Midi.Midi.SecondsLength) },
                                                        { "bpm", Midi.Tempo.realTempo },
                                                        { "bpm-dev", Midi.Tempo.rawTempo },
                                                        { "ts", Midi.TimeSignature.Numerator.ToString() + "/" + Midi.TimeSignature.Denominator.ToString() },
                                                        { "ts-n", Midi.TimeSignature.Numerator },
                                                        { "ts-d", Midi.TimeSignature.Denominator },
                                                        { "scale-name", ScaleName }, 
                                                        { "scale-abb", ScaleAbb } });

            var rect = new RawRectangleF(0, 0, composite.Width, composite.Height);

            var dataMerged = data.SelectMany(d => d).ToDictionary(e => e.Key, e => e.Value);
            var builtText = ProcessText(dataMerged, state.TextTemplate);

            string text = builtText;
            context.ClearRenderTargetView(composite);
            using (target2d.BeginDraw())
            {
                textFormat.TextAlignment = DirectWrite.TextAlignment.Leading;
                textFormat.ParagraphAlignment = DirectWrite.ParagraphAlignment.Near;
                target2d.RenderTarget.DrawText(text, textFormat, rect, brush);
                //var layout = textFormat.GetLayout(text);
                //Console.WriteLine(layout.DetermineMinWidth());
            }

            compositor.Composite(context, composite, plainShader, renderSurface);
        }

        string ProcessText(Dictionary<string, object> data, string text)
        {
            var matches = search.Matches(text);

            var defaultFormat = new DecimalFormat(0, 0, 0, 0, 0, ",", ".");
            var formatDefaults = new Dictionary<string, DecimalFormat>()
            {
                { "sec", new DecimalFormat(0, 1, 0, 1) }
            };

            foreach (var _m in matches)
            {
                try
                {
                    var m = _m as Match;
                    if (m != null)
                    {
                        var tag = m.Value;
                        var parts = tag.TrimStart('{').TrimEnd('}').Split('/');
                        if (parts.Length == 0) continue;
                        var partName = parts[0].Trim();
                        if (!data.ContainsKey(partName)) continue;
                        var partNameBase = partName.Split('-').First();
                        var value = data[partName];
                        var metaParts = parts.Skip(1).Select(p => p.Replace(" ", "")).ToArray();
                        if (value is string)
                        {
                            text = text.Replace(tag, (string)value);
                            continue;
                        }
                        if (value is double || value is int || value is float || value is decimal || value is long)
                        {
                            decimal nv = Convert.ToDecimal(value);

                            var format = formatDefaults.ContainsKey(partNameBase) ? formatDefaults[partNameBase] : defaultFormat;
                            var round = GetValueForPart(metaParts, "rnd", format.Round);
                            var floor = GetValueForPart(metaParts, "flr", format.Floor);
                            var ceiling = GetValueForPart(metaParts, "cil", format.Ceiling);
                            var minDecimals = GetValueForPart(metaParts, "dec", format.MinDecimals);
                            var minDigits = GetValueForPart(metaParts, "dig", format.MinDigits);
                            var thousands = GetValueForPart(metaParts, "sep", format.Thousands);
                            var point = GetValueForPart(metaParts, "pnt", format.Point);
                            var processed = ProcessDecimal(nv, round, floor, ceiling, minDecimals, minDigits, thousands, point);
                            text = text.Replace(tag, processed);
                            continue;
                        }
                    }
                }
                catch { }
            }

            return text;
        }
    }
}
