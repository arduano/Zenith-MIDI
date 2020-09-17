using ZenithEngine;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using ZenithShared;
using ZenithEngine.UI;
using ZenithEngine.ModuleUI;
using Newtonsoft.Json.Linq;
using Zenith.Models;

namespace Zenith
{
    public partial class ZenithWindow : Window, ISerializableContainer
    {
        #region Chrome Window scary code
        private static IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case 0x0024:
                    WmGetMinMaxInfo(hwnd, lParam);
                    handled = true;
                    break;
            }
            return (IntPtr)0;
        }

        private static void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
        {
            MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));
            int MONITOR_DEFAULTTONEAREST = 0x00000002;
            IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (monitor != IntPtr.Zero)
            {
                MONITORINFO monitorInfo = new MONITORINFO();
                GetMonitorInfo(monitor, monitorInfo);
                RECT rcWorkArea = monitorInfo.rcWork;
                RECT rcMonitorArea = monitorInfo.rcMonitor;
                mmi.ptMaxPosition.x = Math.Abs(rcWorkArea.left - rcMonitorArea.left);
                mmi.ptMaxPosition.y = Math.Abs(rcWorkArea.top - rcMonitorArea.top);
                mmi.ptMaxSize.x = Math.Abs(rcWorkArea.right - rcWorkArea.left);
                mmi.ptMaxSize.y = Math.Abs(rcWorkArea.bottom - rcWorkArea.top);
            }
            Marshal.StructureToPtr(mmi, lParam, true);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
            public POINT(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        };

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MONITORINFO
        {
            public int cbSize = Marshal.SizeOf(typeof(MONITORINFO));
            public RECT rcMonitor = new RECT();
            public RECT rcWork = new RECT();
            public int dwFlags = 0;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
            public static readonly RECT Empty = new RECT();
            public int Width { get { return Math.Abs(right - left); } }
            public int Height { get { return bottom - top; } }
            public RECT(int left, int top, int right, int bottom)
            {
                this.left = left;
                this.top = top;
                this.right = right;
                this.bottom = bottom;
            }
            public RECT(RECT rcSrc)
            {
                left = rcSrc.left;
                top = rcSrc.top;
                right = rcSrc.right;
                bottom = rcSrc.bottom;
            }
            public bool IsEmpty { get { return left >= right || top >= bottom; } }
            public override string ToString()
            {
                if (this == Empty) { return "RECT {Empty}"; }
                return "RECT { left : " + left + " / top : " + top + " / right : " + right + " / bottom : " + bottom + " }";
            }
            public override bool Equals(object obj)
            {
                if (!(obj is Rect)) { return false; }
                return (this == (RECT)obj);
            }
            /// <summary>Return the HashCode for this struct (not garanteed to be unique)</summary>
            public override int GetHashCode() => left.GetHashCode() + top.GetHashCode() + right.GetHashCode() + bottom.GetHashCode();
            /// <summary> Determine if 2 RECT are equal (deep compare)</summary>
            public static bool operator ==(RECT rect1, RECT rect2) { return (rect1.left == rect2.left && rect1.top == rect2.top && rect1.right == rect2.right && rect1.bottom == rect2.bottom); }
            /// <summary> Determine if 2 RECT are different(deep compare)</summary>
            public static bool operator !=(RECT rect1, RECT rect2) { return !(rect1 == rect2); }
        }

        [DllImport("user32")]
        internal static extern bool GetMonitorInfo(IntPtr hMonitor, MONITORINFO lpmi);

        [DllImport("User32")]
        internal static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);
        #endregion

        #region Dependency Properties
        public bool MidiLoaded
        {
            get { return (bool)GetValue(MidiLoadedProperty); }
            set { SetValue(MidiLoadedProperty, value); }
        }

        public static readonly DependencyProperty MidiLoadedProperty =
            DependencyProperty.Register("MidiLoaded", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));


        public RenderPipeline ActivePipeline
        {
            get { return (RenderPipeline)GetValue(ActivePipelineProperty); }
            set { SetValue(ActivePipelineProperty, value); }
        }

        public static readonly DependencyProperty ActivePipelineProperty =
            DependencyProperty.Register("ActivePipeline", typeof(RenderPipeline), typeof(MainWindow), new PropertyMetadata(null));


        public bool Rendering
        {
            get { return (bool)GetValue(RenderingProperty); }
            set { SetValue(RenderingProperty, value); }
        }

        public static readonly DependencyProperty RenderingProperty =
            DependencyProperty.Register("Rendering", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));


        public bool RenderingOrPreviewing
        {
            get { return (bool)GetValue(RenderingOrPreviewingProperty); }
            set { SetValue(RenderingOrPreviewingProperty, value); }
        }

        public static readonly DependencyProperty RenderingOrPreviewingProperty =
            DependencyProperty.Register("RenderingOrPreviewing", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        public bool NotRenderingOrPreviewing
        {
            get { return (bool)GetValue(NotRenderingOrPreviewingProperty); }
            set { SetValue(NotRenderingOrPreviewingProperty, value); }
        }

        public static readonly DependencyProperty NotRenderingOrPreviewingProperty =
            DependencyProperty.Register("NotRenderingOrPreviewing", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        public bool CanLoadMidi
        {
            get { return (bool)GetValue(CanLoadMidiProperty); }
            set { SetValue(CanLoadMidiProperty, value); }
        }

        public static readonly DependencyProperty CanLoadMidiProperty =
            DependencyProperty.Register("CanLoadMidi", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        public bool CanUnloadMidi
        {
            get { return (bool)GetValue(CanUnloadMidiProperty); }
            set { SetValue(CanUnloadMidiProperty, value); }
        }

        public static readonly DependencyProperty CanUnloadMidiProperty =
            DependencyProperty.Register("CanUnloadMidi", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        public bool CanStart
        {
            get { return (bool)GetValue(CanStartProperty); }
            set { SetValue(CanStartProperty, value); }
        }

        public static readonly DependencyProperty CanStartProperty =
            DependencyProperty.Register("CanStart", typeof(bool), typeof(MainWindow), new PropertyMetadata(true));

        void InitBindings()
        {
            new InplaceConverter<RenderPipeline, bool>(
                new BBinding(ActivePipelineProperty, this),
                (p) => p != null
            ).Set(this, RenderingOrPreviewingProperty);

            new InplaceConverter<bool, bool>(
                new BBinding(RenderingOrPreviewingProperty, this),
                (p) => !p
            ).Set(this, NotRenderingOrPreviewingProperty);

            new InplaceConverter<bool, bool, bool>(
                new BBinding(RenderingOrPreviewingProperty, this),
                new BBinding(MidiLoadedProperty, this),
                (r, m) => !r && !m
            ).Set(this, CanLoadMidiProperty);

            new InplaceConverter<bool, bool, bool>(
                new BBinding(RenderingOrPreviewingProperty, this),
                new BBinding(MidiLoadedProperty, this),
                (r, m) => !r && m
            ).Set(this, CanUnloadMidiProperty);

            new InplaceConverter<bool, bool, bool>(
                new BBinding(RenderingOrPreviewingProperty, this),
                new BBinding(MidiLoadedProperty, this),
                (r, m) => !r && m
            ).Set(this, CanStartProperty);

            new InplaceConverter<RenderPipeline, bool>(
                new BBinding(ActivePipelineProperty, this),
                (p) => p != null && p.Rendering
            ).Set(this, RenderingProperty);
        }

        #endregion

        public BaseModel DataBase = new BaseModel();

        public ZenithWindow()
        {
            InitBindings();

            InitializeComponent();

            DataContext = DataBase;

            Resources.MergedDictionaries.Add(DataBase.Lang.MergedLanguages);

            if (!DataBase.HasTriedLoadingModules)
                Task.Run(() => DataBase.LoadAllModules());
            if (!DataBase.KdmapiConnected) 
                Task.Run(() => DataBase.LoadKdmapi());

            SourceInitialized += (s, e) =>
            {
                IntPtr handle = (new WindowInteropHelper(this)).Handle;
                HwndSource.FromHwnd(handle).AddHook(new HwndSourceHook(WindowProc));
            };

            //Task.Run(async () =>
            //{
            //    await DataBase.Midi.LoadMidi(@"D:\Midi\tau2.5.9.mid");
            //    await DataBase.ModuleLoadTask.Await();
            //    DataBase.SelectedModule = DataBase.RenderModules.Where(m => m.Name.ToLower().Contains("trail")).First();
            //    await DataBase.StartPreview();
            //});
        }

        private void updateDownloaded_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ZenithUpdates.KillAllProcesses();
            Process.Start(ZenithUpdates.InstallerPath, "update -Reopen");
            Close();
        }


        #region Window
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MinimiseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
            }
            catch { }
            WindowState = WindowState.Minimized;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }
        #endregion

        #region Serialize
        public void Parse(JObject data)
        {
            throw new NotImplementedException();
        }

        public JObject Serialize()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
