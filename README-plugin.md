# Black-Midi-Render

## Creating a plugin project:
- Create a new class library project
- Add references to: BMEngine project, PresentationCore, PresentationFramework, OpenTK (through nuget)
- Optionally, add references (though nuget) to Newtonsoft.Json, Extended.Wpf.Toolkit
- Add a **public** "Render" class to the project that implements the IPluginRender interface from BMEngine

- Name, Description and PreviewImage are static fields that show the information on the plugins tab
- Initialized needs to be set to true when Init() is called, and to false when Dispose is called
- NoteScreenTime is the time the window of time (in ticks) that the notes can spend on the screen before getting deleted
- LastNoteCount is the last number of notes rendered. 
- SettingsControl is a WPF control that will be visible on the Plugin Settings page

- Init() can be used to initialise frame buffers and vertex buffers
- Dispose() removed them
- SetTrackColors() sets the initial colors of the midi tracks/channels (for custom note color schemes)
- RenderFrame() renders a frame, the final image has to be loaded into the framebuffer "finalCompositeBuff"
- For those new to framebuffers, this is done by: `GL.BindFramebuffer(FramebufferTarget.Framebuffer, finalCompositeBuff);`