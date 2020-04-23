<h1 align="center">Zenith</h1>

<p align="center">
    <img src="https://i.imgur.com/jshhiL3.png" width="256" />
    <br />
    <strong>The world's most optimised MIDI renderer!</strong>
</p>

<p align="center">
    <a href="https://github.com/arduano/Zenith-MIDI/releases/"><img src="https://img.shields.io/github/release/arduano/Zenith-MIDI.svg?style=flat-square" alt="GitHub release"></a>
    <a href="https://github.com/arduano/Zenith-MIDI/releases/"><img src="https://img.shields.io/github/downloads/arduano/Zenith-MIDI/total.svg?style=flat-square" alt="GitHub release"></a>
    <a href="https://github.com/arduano/Zenith-MIDI/blob/master/LICENSE"><img src="https://img.shields.io/badge/license-DBAD-blue.svg?style=flat-square" alt="DBAD license"></a>
    <a href="http://makeapullrequest.com"><img src="https://img.shields.io/badge/PRs-welcome-brightgreen.svg?style=flat-square" alt="PRs Welcome"></a>
    <a href="https://discord.gg/Aj4cb5"><img src="https://img.shields.io/discord/549344616210628609.svg?color=7289DA&style=flat-square" alt="Discord"></a>
    <a href="https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=M9XRCSPYSMBCA&source=url"><img src="https://img.shields.io/badge/Donate-PayPal-green.svg?style=flat-square" alt="Donate"></a>
</p>

## Features
- Much better performance than any other midi renderer currently out there, and extremely RAM optimised!
- Custom render plugins! Anyone can make a render plugin and use it with my Zenith, just by dropping it into the Plugins folder!
- About the plugins again, the included plugins are extremely customisable and are designed with high quality video production in mind!
- Customisable render settings, with the ability to change them during preview mode to see real time changes
- Option to "Include Audio" with render, and to render a separate "Transparency Mask" video for advanced video editing!
- Customisable render settings, with the ability to change almost all plugin settings, and even switch plugins themselves during preview mode to see real time changes! 

## Plugins
As a general rule, all custom and original plugins are completely free and open for anyone to use. However exact clones (PFA, miditrail, etc.) will be slightly modified to make them distinct from the original. I might release the exact looking copies, however BMT members are highly against them spreading anywhere.

It is possible for anyone else who knows C# to make a plugin themselves as well. Feel free to contact me on my dev server! 

- **Classic:** The original render engine for Zenith. Any new render features in Zenith will always be supported first by this plugin. *Included with the program*
- **Flat:** Very similar to the classic plugin, and more of a proof of concept than anything. Basically the classic version except without shading. *Included with the program*
- **PFA+:** An almost identical visual clone of the original program, with support for some exclusive features, including: same width notes, tick based rendering, and Zenith's classic gradient and transparency support. Original options like same width notes and changing the bar colour are also present. Some extra customisations were also included! *Included with the program*
- **MidiTrail+:**  My most ambitious plugin yet. Taking inspiration from the original MidiTrail program, and going far above and beyond, adding support for many requested features, uncluding: Custom ripple (aura) images, 3d box note support and different width notes. This all comes with Zenith's classic support of transparent notes and note gradients. *Included with the program*
- **Note Count Render:** Renders a highly customisable text label for the midi statistics, including properties such as note count, polyphony, tempo, time, ticks, bars and MANY more. Font and font size are also easily customsiable. *Included with the program*
- **Textured:** If the other plugins aren't enough for you, you can make your own look by using the Textured plugin. Extremely customisable, allowing features such as custom note caps for rounded notes and custom keyboard overlays. *Included with the program*

## Installation
[![](https://img.shields.io/github/v/release/arduano/Zenith-MIDI?label=download%2032-bit&style=flat-square)](https://github.com/arduano/Zenith-MIDI/releases/latest/download/Zenithx86.zip) [![](https://img.shields.io/github/v/release/arduano/Zenith-MIDI?label=download%2064-bit&style=flat-square&color=green)](https://github.com/arduano/Zenith-MIDI/releases/latest/download/Zenithx64.zip)

You can download the latest version of Zenith for Windows 32-bit [here](https://github.com/arduano/Zenith-MIDI/releases/latest/download/Zenithx86.zip) and Windows 64-bit [here](https://github.com/arduano/Zenith-MIDI/releases/latest/download/Zenithx64.zip).

FFmpeg is required for video rendering, which you can get it [here](https://ffmpeg.zeranoe.com/builds/).

## Usage
After downloading the app, extract the .zip archive and run the program. If FFmpeg is downloaded, put it next to the Zenith .exe.

## License
Zenith is licensed under the terms of the [Don't Be a Dick Public License](https://github.com/arduano/Zenith-MIDI/blob/master/LICENSE).

## Screenshots
| ![](https://arduano.github.io/Zenith-MIDI/dist/bmr/assets/plugins/classic.png) |   ![](https://arduano.github.io/Zenith-MIDI/dist/bmr/assets/plugins/flat.png)    |
| :----------------------------------------------------------------------------: | :------------------------------------------------------------------------------: |
|                                 Classic plugin                                 |                                   Flat plugin                                    |
|   ![](https://arduano.github.io/Zenith-MIDI/dist/bmr/assets/plugins/pfa.png)   | ![](https://arduano.github.io/Zenith-MIDI/dist/bmr/assets/plugins/miditrail.png) |
|                                  PFA+ plugin                                   |                                MidiTrail+ plugin                                 |
