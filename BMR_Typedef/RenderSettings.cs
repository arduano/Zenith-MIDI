using OpenTK.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Black_Midi_Render
{
    enum KeyboardRenderers
    {
        Legacy,
        New,
        Flat
    }

    enum NoteRenderers
    {
        Shaded,
        Flat
    }

    class RenderSettings
    {

        public int firstNote = 0;
        public int lastNote = 128;
        public double pianoHeight = 0.2;
        public int deltaTimeOnScreen = 300;
        
        public int fps = 60;

        public int width = 1920;
        public int height = 1080;

        public bool ffRender = false;
        public string ffPath = "out.mp4";
        public bool imgRender = false;
        public string imgPath = "imgs";
        public bool vsync = true;

        public bool paused = false;
        public bool forceReRender = false;
        public double tempoMultiplier = 1;

        public bool clickDebug = true;

        public bool includeAudio = false;
        public string audioPath = "";

        public int maxTrackBufferSize = 10000;

        public bool useBitrate = true;
        public int bitrate = 20000;
        public int crf = 17;
        public string crfPreset = "medium";

        public bool glowEnabled = false;
        public int glowRadius = 200;

        public float noteBrightness = 1;

        public KeyboardRenderers kbrender = KeyboardRenderers.New;
        public NoteRenderers ntrender = NoteRenderers.Shaded;

        public Color4[] keyColors;

        public bool running = false;

        public long notesOnScreen = 0;
        public double liveFps = 0;

        public void ResetVariableState()
        {
            keyColors = new Color4[512];
            for (int i = 0; i < 512; i++) keyColors[i] = Color4.Transparent;
        }

        public IKeyboardRender GetKeyboardRenderer()
        {
            if (kbrender == KeyboardRenderers.Legacy) return new BaseKeyboardRender(this);
            if (kbrender == KeyboardRenderers.New) return new NewKeyboardRender(this);
            if (kbrender == KeyboardRenderers.Flat) return new FlatKeyboardRender(this);
            throw new Exception("No renderer selected");
        }

        public INoteRender GetNoteRenderer()
        {
            if (ntrender == NoteRenderers.Shaded) return new ShadedNoteRender(this);
            if (ntrender == NoteRenderers.Flat) return new FlatNoteRender(this);
            throw new Exception("No renderer selected");
        }
    }
}
