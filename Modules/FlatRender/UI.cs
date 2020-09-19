using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ZenithEngine.ModuleUI;
using ZenithEngine.UI;

namespace FlatRender
{
    class UI : UIDockWithPalettes
    {
        public class Keys : UIDock
        {
            public Keys() : base(Dock.Left) { }

            [UIChild]
            public UINumber left = new UINumber("leftKey", new LangText("mods.common.firstNote"), 0, 0, 255);

            [UIChild]
            public UINumber right = new UINumber("rightKey", new LangText("mods.common.lastNote"), 128, 1, 256);
        }

        [UIChild]
        public Keys keys = new Keys() { Margin = new Thickness(0) };

        [UIChild]
        public UINumberSlider noteScreenTime = new UINumberSlider(
            "noteScreenTime",
            new LangText("mods.common.noteScreenTime"),
            400, 1, 10000, 0.1m, 1000000, true
        )
        { SliderWidth = 700, MinNUDWidth = 120 };

        [UIChild]
        public UINumberSlider kbHeight = new UINumberSlider(
            "keyboardHeight",
            new LangText("mods.common.pianoHeight"),
            16, 0, 100
        );

        [UIChild]
        public UICheckbox sameWidthNotes = new UICheckbox("sameWidthNotes", new LangText("mods.common.sameWidthNotes"), true);
    }
}
