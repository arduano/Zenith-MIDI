using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Platform;
using ZenithEngine.GLEngine;

namespace ZenithEngine.Output
{
    public class PreviewWindow : NativeWindow, IDisposable
    {
        class WindowRenderSurface : RenderSurface
        {
            PreviewWindow win;

            void SetSize()
            {
                Width = win.Width;
                Height = win.Height;
            }

            public WindowRenderSurface(PreviewWindow window) : base()
            {
                win = window;
                SetSize();
            }

            public override void BindSurfaceNoViewport()
            {
                SetSize();
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            }

            public override void Dispose()
            {
            }
        }

        public RenderSurface RenderTarget { get; }

        private IGraphicsContext glContext;

        private bool isExiting = false;

        /// <summary>Constructs a new PreviewWindow with the specified attributes.</summary>
        /// <param name="width">The width of the PreviewWindow in pixels.</param>
        /// <param name="height">The height of the PreviewWindow in pixels.</param>
        /// <param name="mode">The OpenTK.Graphics.GraphicsMode of the PreviewWindow.</param>
        /// <param name="title">The title of the PreviewWindow.</param>
        /// <param name="options">PreviewWindow options regarding window appearance and behavior.</param>
        /// <param name="device">The OpenTK.Graphics.DisplayDevice to construct the PreviewWindow in.</param>
        /// <param name="major">The major version for the OpenGL GraphicsContext.</param>
        /// <param name="minor">The minor version for the OpenGL GraphicsContext.</param>
        /// <param name="flags">The GraphicsContextFlags version for the OpenGL GraphicsContext.</param>
        public PreviewWindow(int width, int height, GraphicsMode mode, string title, GameWindowFlags options, DisplayDevice device,
                          int major, int minor, GraphicsContextFlags flags)
            : base(width, height, title, options,
                   mode == null ? GraphicsMode.Default : mode,
                   device == null ? DisplayDevice.Default : device)
        {
            RenderTarget = new WindowRenderSurface(this);

            try
            {
                glContext = new GraphicsContext(mode == null ? GraphicsMode.Default : mode, WindowInfo, major, minor, flags);
                glContext.MakeCurrent(WindowInfo);
                (glContext as IGraphicsContextInternal).LoadAll();

                VSync = VSyncMode.On;
            }
            catch (Exception e)
            {
                base.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Disposes of the PreviewWindow, releasing all resources consumed by it.
        /// </summary>
        public override void Dispose()
        {
            try
            {
                Dispose(true);
            }
            finally
            {
                try
                {
                    if (glContext != null)
                    {
                        glContext.Dispose();
                        glContext = null;
                    }
                }
                finally
                {
                    base.Dispose();
                }
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Closes the PreviewWindow. Equivalent to <see cref="NativeWindow.Close"/> method.
        /// </summary>
        /// <remarks>
        /// <para>Override if you are not using <see cref="PreviewWindow.Run()"/>.</para>
        /// <para>If you override this method, place a call to base.Exit(), to ensure proper OpenTK shutdown.</para>
        /// </remarks>
        public virtual void Exit()
        {
            Close();
        }

        /// <summary>
        /// Makes the GraphicsContext current on the calling thread.
        /// </summary>
        public void MakeCurrent()
        {
            EnsureUndisposed();
            Context.MakeCurrent(WindowInfo);
        }

        /// <summary>
        /// Called when the NativeWindow is about to close.
        /// </summary>
        /// <param name="e">
        /// The <see cref="System.ComponentModel.CancelEventArgs" /> for this event.
        /// Set e.Cancel to true in order to stop the PreviewWindow from closing.</param>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            if (!e.Cancel)
            {
                isExiting = true;
                OnUnloadInternal(EventArgs.Empty);
            }
        }


        /// <summary>
        /// Called after an OpenGL context has been established, but before entering the main loop.
        /// </summary>
        /// <param name="e">Not used.</param>
        protected virtual void OnLoad(EventArgs e)
        {
            Load(this, e);
        }

        /// <summary>
        /// Called after PreviewWindow.Exit was called, but before destroying the OpenGL context.
        /// </summary>
        /// <param name="e">Not used.</param>
        protected virtual void OnUnload(EventArgs e)
        {
            Unload(this, e);
        }

        /// <summary>
        /// Enters the game loop of the PreviewWindow updating and rendering at the specified frequency.
        /// </summary>
        /// <remarks>
        /// When overriding the default game loop you should call ProcessEvents()
        /// to ensure that your PreviewWindow responds to operating system events.
        /// <para>
        /// Once ProcessEvents() returns, it is time to call update and render the next frame.
        /// </para>
        /// </remarks>
        /// <param name="updates_per_second">The frequency of UpdateFrame events.</param>
        /// <param name="frames_per_second">The frequency of RenderFrame events.</param>
        public void Run()
        {
            EnsureUndisposed();

            Visible = true;
            OnLoadInternal(EventArgs.Empty);
            OnResize(EventArgs.Empty);

            ProcessEvents();
        }

        /// <summary>
        /// Swaps the front and back buffer, presenting the rendered scene to the user.
        /// </summary>
        public void SwapBuffers()
        {
            EnsureUndisposed();
            this.Context.SwapBuffers();
        }

        public void BindView()
        {
            RenderTarget.BindSurface();
        }

        /// <summary>
        /// Returns the opengl IGraphicsContext associated with the current PreviewWindow.
        /// </summary>
        public IGraphicsContext Context
        {
            get
            {
                EnsureUndisposed();
                return glContext;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the shutdown sequence has been initiated
        /// for this window, by calling PreviewWindow.Exit() or hitting the 'close' button.
        /// If this property is true, it is no longer safe to use any OpenTK.Input or
        /// OpenTK.Graphics.OpenGL functions or properties.
        /// </summary>
        public bool IsExiting
        {
            get
            {
                EnsureUndisposed();
                return isExiting;
            }
        }

        /// <summary>
        /// Gets or sets the VSyncMode.
        /// </summary>
        public VSyncMode VSync
        {
            get
            {
                EnsureUndisposed();
                GraphicsContext.Assert();
                if (Context.SwapInterval < 0)
                {
                    return VSyncMode.Adaptive;
                }
                else if (Context.SwapInterval == 0)
                {
                    return VSyncMode.Off;
                }
                else
                {
                    return VSyncMode.On;
                }
            }
            set
            {
                EnsureUndisposed();
                GraphicsContext.Assert();
                switch (value)
                {
                    case VSyncMode.On:
                        Context.SwapInterval = 1;
                        break;

                    case VSyncMode.Off:
                        Context.SwapInterval = 0;
                        break;

                    case VSyncMode.Adaptive:
                        Context.SwapInterval = -1;
                        break;
                }
            }
        }

        /// <summary>
        /// Gets or states the state of the NativeWindow.
        /// </summary>
        public override WindowState WindowState
        {
            get
            {
                return base.WindowState;
            }
            set
            {
                base.WindowState = value;

                if (Context != null)
                {
                    Context.Update(WindowInfo);
                }
            }
        }
        /// <summary>
        /// Occurs before the window is displayed for the first time.
        /// </summary>
        public event EventHandler<EventArgs> Load = delegate { };

        /// <summary>
        /// Occurs when it is time to render a frame.
        /// </summary>
        public event EventHandler<FrameEventArgs> RenderFrame = delegate { };

        /// <summary>
        /// Occurs before the window is destroyed.
        /// </summary>
        public event EventHandler<EventArgs> Unload = delegate { };

        /// <summary>
        /// Occurs when it is time to update a frame.
        /// </summary>
        public event EventHandler<FrameEventArgs> UpdateFrame = delegate { };

        /// <summary>
        /// If game window is configured to run with a dedicated update thread (by passing isSingleThreaded = false in the constructor),
        /// occurs when the update thread has started. This would be a good place to initialize thread specific stuff (like setting a synchronization context).
        /// </summary>
        public event EventHandler OnUpdateThreadStarted = delegate { };

        /// <summary>
        /// Override to add custom cleanup logic.
        /// </summary>
        /// <param name="manual">True, if this method was called by the application; false if this was called by the finalizer thread.</param>
        protected virtual void Dispose(bool manual) { }

        /// <summary>
        /// Called when the frame is rendered.
        /// </summary>
        /// <param name="e">Contains information necessary for frame rendering.</param>
        /// <remarks>
        /// Subscribe to the <see cref="RenderFrame"/> event instead of overriding this method.
        /// </remarks>
        protected virtual void OnRenderFrame(FrameEventArgs e)
        {
            RenderFrame(this, e);
        }

        /// <summary>
        /// Called when the frame is updated.
        /// </summary>
        /// <param name="e">Contains information necessary for frame updating.</param>
        /// <remarks>
        /// Subscribe to the <see cref="UpdateFrame"/> event instead of overriding this method.
        /// </remarks>
        protected virtual void OnUpdateFrame(FrameEventArgs e)
        {
            UpdateFrame(this, e);
        }

        /// <summary>
        /// Called when the WindowInfo for this PreviewWindow has changed.
        /// </summary>
        /// <param name="e">Not used.</param>
        protected virtual void OnWindowInfoChanged(EventArgs e) { }

        /// <summary>
        /// Called when this window is resized.
        /// </summary>
        /// <param name="e">Not used.</param>
        /// <remarks>
        /// You will typically wish to update your viewport whenever
        /// the window is resized. See the
        /// <see cref="OpenTK.Graphics.OpenGL.GL.Viewport(int, int, int, int)"/> method.
        /// </remarks>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            glContext.Update(base.WindowInfo);
        }

        private void OnLoadInternal(EventArgs e)
        {
            OnLoad(e);
        }

        private void OnRenderFrameInternal(FrameEventArgs e)
        {
            if (Exists && !isExiting)
            {
                OnRenderFrame(e);
            }
        }

        private void OnUnloadInternal(EventArgs e)
        {
            OnUnload(e);
        }

        private void OnUpdateFrameInternal(FrameEventArgs e)
        {
            if (Exists && !isExiting)
            {
                OnUpdateFrame(e);
            }
        }

        private void OnWindowInfoChangedInternal(EventArgs e)
        {
            glContext.MakeCurrent(WindowInfo);

            OnWindowInfoChanged(e);
        }
    }
}
