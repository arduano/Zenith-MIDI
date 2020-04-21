using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using ZenithEngine;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace NoteCountRender
{
    public class Render : IPluginRender
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

        public string Name => "Note Counter";

        public string Description => "Generate note counts and other midi statistics";

        public string LanguageDictName { get; } = "notecounter";

        public bool Initialized { get; set; } = false;

        public System.Windows.Media.ImageSource PreviewImage { get; set; }

        public bool ManualNoteDelete => true;

        public double NoteCollectorOffset => 0;

        public double Tempo { get; set; }

        public NoteColor[][] NoteColors { get; set; }

        public MidiInfo CurrentMidi { get; set; }

        public double NoteScreenTime => 0;

        public long LastNoteCount { get; set; } = 0;

        public System.Windows.Controls.Control SettingsControl { get; set; }

        RenderSettings renderSettings;
        Settings settings;

        GLTextEngine textEngine;
        public void Dispose()
        {
            textEngine.Dispose();
            Initialized = false;
            if (outputCsv != null) outputCsv.Close();
            Console.WriteLine("Disposed of NoteCountRender");
        }

        int fontSize = 40;
        string font = "Arial";
        public System.Drawing.FontStyle fontStyle = System.Drawing.FontStyle.Regular;

        StreamWriter outputCsv = null;

        public void Init()
        {
            textEngine = new GLTextEngine();
            if (settings.fontName != font || settings.fontSize != fontSize || settings.fontStyle != fontStyle)
            {
                font = settings.fontName;
                fontSize = settings.fontSize;
                fontStyle = settings.fontStyle;
            }
            textEngine.SetFont(font, fontStyle, fontSize);
            noteCount = 0;
            nps = 0;
            frames = 0;
            notesHit = new LinkedList<long>();
            Initialized = true;

            if (settings.saveCsv && settings.csvOutput != "")
            {
                outputCsv = new StreamWriter(settings.csvOutput);
            }

            Console.WriteLine("Initialised NoteCountRender");
        }

        public Render(RenderSettings settings)
        {
            this.renderSettings = settings;
            this.settings = new Settings();
            SettingsControl = new SettingsCtrl(this.settings);
            PreviewImage = BitmapToImageSource(Properties.Resources.preview);
        }

        long noteCount = 0;
        double nps = 0;
        int frames = 0;
        long currentNotes = 0;
        long polyphony = 0;

        LinkedList<long> notesHit = new LinkedList<long>();

        public void RenderFrame(FastList<Note> notes, double midiTime, int finalCompositeBuff)
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, finalCompositeBuff);

            GL.Viewport(0, 0, renderSettings.width, renderSettings.height);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Clear(ClearBufferMask.DepthBufferBit);

            GL.Enable(EnableCap.Blend);
            GL.EnableClientState(ArrayCap.VertexArray);
            GL.EnableClientState(ArrayCap.ColorArray);
            GL.EnableClientState(ArrayCap.TextureCoordArray);
            GL.Enable(EnableCap.Texture2D);

            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);

            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            if (settings.fontName != font || settings.fontSize != fontSize || settings.fontStyle != fontStyle)
            {
                font = settings.fontName;
                fontSize = settings.fontSize;
                fontStyle = settings.fontStyle;
                textEngine.SetFont(font, fontStyle, fontSize);
            }
            if (!renderSettings.Paused)
            {
                polyphony = 0;
                currentNotes = 0;
                long nc = 0;
                lock (notes)
                    foreach (Note n in notes)
                    {
                        nc++;
                        if (n.start < midiTime)
                        {
                            if (n.end > midiTime || !n.hasEnded)
                            {
                                polyphony++;
                            }
                            else if (n.meta != null)
                            {
                                n.delete = true;
                            }
                            if (n.meta == null)
                            {
                                currentNotes++;
                                noteCount++;
                                n.meta = true;
                            }
                        }
                        if (n.start > midiTime) break;
                    }
                LastNoteCount = nc;
                notesHit.AddLast(currentNotes);
                while (notesHit.Count > renderSettings.fps) notesHit.RemoveFirst();
                nps = notesHit.Sum();
            }

            double tempo = Tempo;

            int seconds = (int)Math.Floor((double)frames / renderSettings.fps);
            int milliseconds = (int)Math.Floor((double)frames * 1000 / renderSettings.fps);
            int totalsec = (int)Math.Floor(CurrentMidi.secondsLength / 1000);
            if (seconds > totalsec) seconds = totalsec;
            TimeSpan time = new TimeSpan(0, 0, seconds);
            TimeSpan miltime = new TimeSpan(0, 0, 0, 0, milliseconds);
            TimeSpan totaltime = new TimeSpan(0, 0, totalsec);
            TimeSpan totalmiltime = new TimeSpan(0, 0, 0, 0, (int)Math.Floor(CurrentMidi.secondsLength));
            if (time > totaltime) time = totaltime;
            if (!renderSettings.Paused) frames++;

            double barDivide = (double)CurrentMidi.division * CurrentMidi.timeSig.numerator / CurrentMidi.timeSig.denominator * 4;

            long limMidiTime = (long)midiTime;
            if (limMidiTime > CurrentMidi.tickLength) limMidiTime = CurrentMidi.tickLength;

            long bar = (long)Math.Floor(limMidiTime / barDivide) + 1;
            long maxbar = (long)Math.Floor(CurrentMidi.tickLength / barDivide);
            if (bar > maxbar) bar = maxbar;

            Func<string, Commas, string> replace = (text, separator) =>
            {
                string sep = "";
                if (separator == Commas.Comma) sep = "#,##";

                text = text.Replace("{bpm}", (Math.Round(tempo * 10) / 10).ToString());

                text = text.Replace("{nc}", noteCount.ToString(sep + "0"));
                text = text.Replace("{nr}", (CurrentMidi.noteCount - noteCount).ToString(sep + "0"));
                text = text.Replace("{tn}", CurrentMidi.noteCount.ToString(sep + "0"));

                text = text.Replace("{nps}", nps.ToString(sep + "0"));
                text = text.Replace("{plph}", polyphony.ToString(sep + "0"));

                text = text.Replace("{currsec}", seconds.ToString(sep + "0.0"));
                text = text.Replace("{currtime}", time.ToString("mm\\:ss"));
                text = text.Replace("{cmiltime}", miltime.ToString("mm\\:ss\\.fff"));
                text = text.Replace("{totalsec}", totalsec.ToString(sep + "0.0"));
                text = text.Replace("{totaltime}", totaltime.ToString("mm\\:ss"));
                text = text.Replace("{tmiltime}", totalmiltime.ToString("mm\\:ss\\.fff"));
                text = text.Replace("{remsec}", (totalsec - seconds).ToString(sep + "0.0"));
                text = text.Replace("{remtime}", (totaltime - time).ToString("mm\\:ss"));
                text = text.Replace("{rmiltime}", (totalmiltime - miltime).ToString("mm\\:ss\\.fff"));

                text = text.Replace("{currticks}", (limMidiTime).ToString(sep + "0"));
                text = text.Replace("{totalticks}", (CurrentMidi.tickLength).ToString(sep + "0"));
                text = text.Replace("{remticks}", (CurrentMidi.tickLength - limMidiTime).ToString(sep + "0"));

                text = text.Replace("{currbars}", bar.ToString(sep + "0"));
                text = text.Replace("{totalbars}", maxbar.ToString(sep + "0"));
                text = text.Replace("{rembars}", (maxbar - bar).ToString(sep + "0"));

                text = text.Replace("{ppq}", CurrentMidi.division.ToString());
                text = text.Replace("{tsn}", CurrentMidi.timeSig.numerator.ToString());
                text = text.Replace("{tsd}", CurrentMidi.timeSig.denominator.ToString());
                text = text.Replace("{avgnps}", ((double)CurrentMidi.noteCount / (double)totalsec).ToString(sep + "0"));
                return text;
            };


            string renderText = settings.text;
            renderText = replace(renderText, settings.thousandSeparator);

            if (settings.textAlignment == Alignments.TopLeft)
            {
                var size = textEngine.GetBoundBox(renderText);
                Matrix4 transform = Matrix4.Identity;
                transform = Matrix4.Mult(transform, Matrix4.CreateScale(1.0f / renderSettings.width, -1.0f / renderSettings.height, 1.0f));
                transform = Matrix4.Mult(transform, Matrix4.CreateTranslation(-1, 1, 0));
                transform = Matrix4.Mult(transform, Matrix4.CreateRotationZ(0));

                textEngine.Render(renderText, transform, Color4.White);
            }
            else if (settings.textAlignment == Alignments.TopRight)
            {
                float offset = 0;
                string[] lines = renderText.Split('\n');
                foreach (var line in lines)
                {
                    var size = textEngine.GetBoundBox(line);
                    Matrix4 transform = Matrix4.Identity;
                    transform = Matrix4.Mult(transform, Matrix4.CreateTranslation(-size.Width, offset, 0));
                    transform = Matrix4.Mult(transform, Matrix4.CreateScale(1.0f / renderSettings.width, -1.0f / renderSettings.height, 1.0f));
                    transform = Matrix4.Mult(transform, Matrix4.CreateTranslation(1, 1, 0));
                    transform = Matrix4.Mult(transform, Matrix4.CreateRotationZ(0));
                    offset += size.Height;
                    textEngine.Render(line, transform, Color4.White);
                }
            }
            else if (settings.textAlignment == Alignments.BottomLeft)
            {
                float offset = 0;
                string[] lines = renderText.Split('\n');
                foreach (var line in lines.Reverse())
                {
                    var size = textEngine.GetBoundBox(line);
                    Matrix4 transform = Matrix4.Identity;
                    transform = Matrix4.Mult(transform, Matrix4.CreateTranslation(0, offset - size.Height, 0));
                    transform = Matrix4.Mult(transform, Matrix4.CreateScale(1.0f / renderSettings.width, -1.0f / renderSettings.height, 1.0f));
                    transform = Matrix4.Mult(transform, Matrix4.CreateTranslation(-1, -1, 0));
                    transform = Matrix4.Mult(transform, Matrix4.CreateRotationZ(0));
                    offset -= size.Height;
                    textEngine.Render(line, transform, Color4.White);
                }
            }
            else if (settings.textAlignment == Alignments.BottomRight)
            {
                float offset = 0;
                string[] lines = renderText.Split('\n');
                foreach (var line in lines.Reverse())
                {
                    var size = textEngine.GetBoundBox(line);
                    Matrix4 transform = Matrix4.Identity;
                    transform = Matrix4.Mult(transform, Matrix4.CreateTranslation(-size.Width, offset - size.Height, 0));
                    transform = Matrix4.Mult(transform, Matrix4.CreateScale(1.0f / renderSettings.width, -1.0f / renderSettings.height, 1.0f));
                    transform = Matrix4.Mult(transform, Matrix4.CreateTranslation(1, -1, 0));
                    transform = Matrix4.Mult(transform, Matrix4.CreateRotationZ(0));
                    offset -= size.Height;
                    textEngine.Render(line, transform, Color4.White);
                }
            }
            else if (settings.textAlignment == Alignments.TopSpread)
            {
                float offset = 0;
                string[] lines = renderText.Split('\n');
                float dist = 1.0f / (lines.Length + 1);
                int p = 1;
                foreach (var line in lines.Reverse())
                {
                    var size = textEngine.GetBoundBox(line);
                    Matrix4 transform = Matrix4.Identity;
                    transform = Matrix4.Mult(transform, Matrix4.CreateTranslation(-size.Width / 2, 0, 0));
                    transform = Matrix4.Mult(transform, Matrix4.CreateScale(1.0f / renderSettings.width, -1.0f / renderSettings.height, 1.0f));
                    transform = Matrix4.Mult(transform, Matrix4.CreateTranslation((dist * p++) * 2 - 1, 1, 0));
                    transform = Matrix4.Mult(transform, Matrix4.CreateRotationZ(0));
                    offset -= size.Height;
                    textEngine.Render(line, transform, Color4.White);
                }
            }
            else if (settings.textAlignment == Alignments.BottomSpread)
            {
                float offset = 0;
                string[] lines = renderText.Split('\n');
                float dist = 1.0f / (lines.Length + 1);
                int p = 1;
                foreach (var line in lines.Reverse())
                {
                    var size = textEngine.GetBoundBox(line);
                    Matrix4 transform = Matrix4.Identity;
                    transform = Matrix4.Mult(transform, Matrix4.CreateTranslation(-size.Width / 2, -size.Height, 0));
                    transform = Matrix4.Mult(transform, Matrix4.CreateScale(1.0f / renderSettings.width, -1.0f / renderSettings.height, 1.0f));
                    transform = Matrix4.Mult(transform, Matrix4.CreateTranslation((dist * p++) * 2 - 1, -1, 0));
                    transform = Matrix4.Mult(transform, Matrix4.CreateRotationZ(0));
                    offset -= size.Height;
                    textEngine.Render(line, transform, Color4.White);
                }
            }

            if(outputCsv != null)
            {
                outputCsv.WriteLine(replace(settings.csvFormat, Commas.Nothing));
            }

            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.Texture2D);
            GL.DisableClientState(ArrayCap.VertexArray);
            GL.DisableClientState(ArrayCap.ColorArray);
            GL.DisableClientState(ArrayCap.TextureCoordArray);

            GL.DisableVertexAttribArray(0);
            GL.DisableVertexAttribArray(1);
        }

        public void ReloadTrackColors()
        {

        }
    }
}
