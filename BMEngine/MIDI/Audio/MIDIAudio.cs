using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZenithEngine.MIDI.Audio
{
    public interface IMidiOutput : IDisposable
    {
        void SendEvent(uint e);
        void Reset();
    }

    public class MIDIAudio : IDisposable
    {
        IMidiOutput output;
        MidiPlayback playback;
        Task runner;

        bool ended = false;

        public bool Paused { get; set; }

        public MIDIAudio(MidiPlayback playback, IMidiOutput output)
        {
            this.output = output;
            this.playback = playback;
            runner = Task.Run(Loop);
        }

        void SendNotesOff()
        {
            byte cc = 120;
            byte vv = 0;
            for (int i = 0; i < 16; i++)
            {
                var command = 0b10110000 & i;
                output.SendEvent((uint)(command | (cc << 8) | (vv << 16)));
            }
        }

        void Loop()
        {
            var events = playback.PlaybackEvents;

            double lastTime = playback.PlayerPositionSeconds;
            var watch = new Stopwatch();
            watch.Start();
            double time() => watch.Elapsed.TotalSeconds + lastTime;

            void testSync(bool force = false)
            {
                if (playback.PlayerPositionSeconds != lastTime || force)
                {
                    lastTime = playback.PlayerPositionSeconds;
                    watch.Reset();
                    watch.Start();
                }
            }

            void wait(Func<bool> cond, bool paused = false, int ms = 10)
            {
                while (!cond() && !ended && (!Paused || paused))
                {
                    testSync();
                    Thread.Sleep(ms);
                }
                testSync();
            }

            while (!ended)
            {
                if (events.ZeroLen)
                {
                    wait(() => !events.ZeroLen);
                    if (ended) continue;
                }
                var ev = events.Pop();

                var t = time();
                if (ev.time > t)
                {
                    wait(() => time() > ev.time - 0.2);
                    var ms = (ev.time - time()) * 1000;
                    if (ms > 0) Thread.Sleep((int)ms);
                }
                else
                {
                    if (t - ev.time > 2)
                    {
                        var e = ev.val & 0xF0;
                        if (e == 0x90 || e == 0x90) continue;
                    }
                }

                if (Paused)
                {
                    SendNotesOff();
                    wait(() => !Paused, true);
                    testSync(true);
                    continue;
                }

                if (!playback.PushPlaybackEvents)
                {
                    events.Unlink();
                    wait(() => playback.PushPlaybackEvents);
                    testSync(true);
                    continue;
                }

                output.SendEvent((uint)ev.val);
            }
            SendNotesOff();
        }

        public void Dispose()
        {
            ended = true;
            if (!runner.IsCompleted) runner.Wait();
        }
    }
}
