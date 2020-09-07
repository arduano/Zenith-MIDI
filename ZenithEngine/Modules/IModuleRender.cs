﻿using OpenTK.Graphics;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ZenithEngine.DXHelper;
using ZenithEngine.MIDI;

namespace ZenithEngine.Modules
{
    public interface IModuleRender : IDisposable
    {
        string Name { get; }
        string Description { get; }
        bool Initialized { get; }
        ImageSource PreviewImage { get; }

        string LanguageDictName { get; }

        FrameworkElement SettingsControl { get; }
        public double StartOffset { get; }

        void Init(Device device, MidiPlayback midi, RenderStatus status);
        void RenderFrame(DeviceContext context, IRenderSurface renderSurface);
        void ReloadTrackColors();
    }
}