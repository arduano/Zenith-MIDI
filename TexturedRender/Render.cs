using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ZenithEngine;
using ZenithEngine.DXHelper;
using ZenithEngine.Modules;
using ZenithEngine.ModuleUI;

namespace TexturedRender
{
    public class Render : PureModule
    {
        public override string Name => "Textured";

        public override string Description => "Plugin for loading and rendering custom resource packs, " +
            "with settings defined in a .json file";

        public override string LanguageDictName { get; } = "textured";

        public override ImageSource PreviewImage => null;

        public override double StartOffset => 0;

        protected override NoteColorPalettePick PalettePicker => null;

        UI settings = new UI();
        public override FrameworkElement SettingsControl => settings;

        #region UI
        class UI : UIDockWithPalettes
        {
            //public class Keys : UIDock
            //{
            //    public Keys() : base(Dock.Left) { }

            //    [UIChild]
            //    public UINumber left = new UINumber()
            //    {
            //        Label = new DynamicResourceExtension("firstNote"),
            //        Min = 0,
            //        Max = 255,
            //        Value = 0,
            //    };

            //    [UIChild]
            //    public UINumber right = new UINumber()
            //    {
            //        Label = new DynamicResourceExtension("lastNote"),
            //        Min = 1,
            //        Max = 256,
            //        Value = 128,
            //    };
            //}

            //[UIChild]
            //public Keys keys = new Keys();

            //[UIChild]
            //public UINumberSlider noteScreenTime = new UINumberSlider()
            //{
            //    Label = new DynamicResourceExtension("noteScreenTime"),
            //    SliderMin = 2,
            //    SliderMax = 4096,
            //    Min = 0.1,
            //    Max = 1000000,
            //    DecimalPoints = 2,
            //    Step = 1,
            //    Value = 400,
            //};

            //[UIChild]
            //public UINumberSlider kbHeight = new UINumberSlider()
            //{
            //    Label = new DynamicResourceExtension("pianoHeight"),
            //    SliderMin = 0,
            //    SliderMax = 100,
            //    Min = 0,
            //    Max = 100,
            //    DecimalPoints = 2,
            //    Step = 1,
            //    Value = 16,
            //    SliderWidth = 200,
            //};

            //[UIChild]
            //public UICheckbox sameWidthNotes = new UICheckbox()
            //{
            //    Label = new DynamicResourceExtension("sameWidthNotes"),
            //    IsChecked = true,
            //};
        }
        #endregion

        public override void RenderFrame(DeviceContext context, IRenderSurface renderSurface)
        {
            throw new NotImplementedException();
        }
    }
}
