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
using ZenithEngine.Modules;

namespace NoteCountRender
{
    public class Render : PureModule
    {
        #region Info
        public override string Name { get; } = "Note Count";
        public override string Description { get; } = "blah blah blah";
        public override ImageSource PreviewImage { get; } = LoadPreviewBitmap(Properties.Resources.preview);
        public override string LanguageDictName { get; } = "notecounter";
        #endregion

        public override FrameworkElement SettingsControl => null;

        public override double StartOffset => 0;

        protected override NoteColorPalettePick PalettePicker => null;

        public Render()
        {
        }

        public override void RenderFrame(DeviceContext context, IRenderSurface renderSurface)
        { 
        }
    }
}
