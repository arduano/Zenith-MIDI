using OpenTK.Graphics;
using System;
using System.Collections.Generic;
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

        public bool ffRender = false;
        public string ffPath = "out.mp4";
        public bool imgRender = false;
        public string imgPath = "imgs";
        public bool vsync = true;
        public bool renderStartBlankScreen = false;
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


        public KeyboardRenderers kbrender = KeyboardRenderers.New;
        public NoteRenderers ntrender = NoteRenderers.Shaded;

        public bool running = false;
        
        public double liveFps = 0;
    }
}
