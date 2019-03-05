using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MidiUtils;

namespace Colour_Event_Inject
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new ColourForm());

            var midiout = new StreamWriter(@"E:\test.mid");
            var midiin = new StreamReader(@"E:\Midi\Pi.mid");
            MidiWriter writer = new MidiWriter(midiout.BaseStream);
            writer.Init();

            var filter = new TrackFilter();


            MidiFileInfo info = MidiFileInfo.Parse(midiin.BaseStream);
            writer.WriteDivision(info.Division);
            writer.WriteNtrks((ushort)Math.Min(info.TrackCount, 65535));
            writer.WriteFormat(info.Format);

            for (int i = 0; i < info.TrackCount; i++)
            {
                Console.WriteLine("Priocessing track: " + i);
                byte[] trackbytes = new byte[info.Tracks[i].Length];
                midiin.BaseStream.Position = info.Tracks[i].Start;
                midiin.BaseStream.Read(trackbytes, 0, (int)info.Tracks[i].Length);
                writer.InitTrack();
                long prevtime = 0;
                double hue = i * 60;
                int d = 3;
                filter.MidiEventFilter = (byte[] dtimeb, int dtime, byte[] data, long time) =>
                {
                    byte[] e = new byte[] { 0xFF, 0x0A, 0x08, 0x00, 0x0F, 0x7F, 0x00, 0x00, 0x00, 0x00, 0xFF };
                    //byte[] e = new byte[] { 0xFF, 0x0A, 0x0C, 0x00, 0x0F, 0x7F, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x00, 0x00, 0x00, 0xFF };
                    if (time - prevtime > d)
                    {
                        List<byte> o = new List<byte>();
                        long start = time - dtime;
                        while(prevtime + d < time)
                        {
                            prevtime += d;
                            int delta = (int)(prevtime - start);
                            int r, g, b;
                            HsvToRgb(hue, 1, 1, out r, out g, out b);
                            hue += 1;
                            hue = hue % 360;
                            e[7] = (byte)r;
                            e[8] = (byte)g;
                            e[9] = (byte)b;
                            //HsvToRgb(hue + 60, 1, 1, out r, out g, out b);
                            //hue += 1;
                            //hue = hue % 360;
                            //e[11] = (byte)r;
                            //e[12] = (byte)g;
                            //e[13] = (byte)b;
                            o.AddRange(filter.MakeVariableLen(delta));
                            o.AddRange(e);
                            start += delta;
                        }
                        o.AddRange(filter.MakeVariableLen((int)(time - start)));
                        o.AddRange(data);
                        return o.ToArray();
                    }
                    return dtimeb.Concat(data).ToArray();
                };
                var newtrackbytes = filter.FilterTrack(new MemoryByteReader(trackbytes));
                writer.Write(newtrackbytes);
                writer.EndTrack();
            }

            writer.Close();
        }

        static void HsvToRgb(double h, double S, double V, out int r, out int g, out int b)
        {
            double H = h;
            while (H < 0) { H += 360; };
            while (H >= 360) { H -= 360; };
            double R, G, B;
            if (V <= 0)
            { R = G = B = 0; }
            else if (S <= 0)
            {
                R = G = B = V;
            }
            else
            {
                double hf = H / 60.0;
                int i = (int)Math.Floor(hf);
                double f = hf - i;
                double pv = V * (1 - S);
                double qv = V * (1 - S * f);
                double tv = V * (1 - S * (1 - f));
                switch (i)
                {

                    // Red is the dominant color

                    case 0:
                        R = V;
                        G = tv;
                        B = pv;
                        break;

                    // Green is the dominant color

                    case 1:
                        R = qv;
                        G = V;
                        B = pv;
                        break;
                    case 2:
                        R = pv;
                        G = V;
                        B = tv;
                        break;

                    // Blue is the dominant color

                    case 3:
                        R = pv;
                        G = qv;
                        B = V;
                        break;
                    case 4:
                        R = tv;
                        G = pv;
                        B = V;
                        break;

                    // Red is the dominant color

                    case 5:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // Just in case we overshoot on our math by a little, we put these here. Since its a switch it won't slow us down at all to put these here.

                    case 6:
                        R = V;
                        G = tv;
                        B = pv;
                        break;
                    case -1:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // The color is not defined, we should throw an error.

                    default:
                        //LFATAL("i Value error in Pixel conversion, Value is %d", i);
                        R = G = B = V; // Just pretend its black/white
                        break;
                }
            }
            r = Clamp((int)(R * 255.0));
            g = Clamp((int)(G * 255.0));
            b = Clamp((int)(B * 255.0));
        }

        /// <summary>
        /// Clamp a value to 0-255
        /// </summary>
        static int Clamp(int i)
        {
            if (i < 0) return 0;
            if (i > 255) return 255;
            return i;
        }
    }
}
