using OpenTK.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMEngine
{
    public enum KeyboardRenderers
    {
        Legacy,
        New,
        Flat
    }

    public enum NoteRenderers
    {
        Shaded,
        Flat
    }

    public class RenderSettings
    {
        public int fps = 60;

        public int width = 1920;
        public int height = 1080;
        public int downscale = 1;

        public bool ffRender = false;
        public string ffPath = "";
        public bool ffRenderMask = false;
        public string ffMaskPath = "";
        public bool vsync = true;
        public double renderSecondsDelay = 0;

        public bool paused = false;
        public bool forceReRender = true;
        public double tempoMultiplier = 1;

        public bool includeAudio = false;
        public string audioPath = "";

        public int maxTrackBufferSize = 10000;

        public bool useBitrate = true;
        public int bitrate = 20000;
        public int crf = 17;
        public string crfPreset = "medium";
        public bool ffmpegDebug = false;

        public bool showNoteCount = false;
        public bool showNotesRendered = false;
        public int fontSize = 50;
        public string font = "Arial";

        public bool running = false;

        public bool playSound = true;
        public bool playbackEnabled = true;

        public bool realtimePlayback = true;

        public double liveFps = 0;

        public bool timeBasedNotes = false;

        public bool ignoreColorEvents = false;

        public long lastyBGChangeTime = -1;
        public Bitmap BGImage = null;
    }
}
