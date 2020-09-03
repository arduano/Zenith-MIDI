using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.IO;

namespace Zenith.Models
{
    public enum OutputType
    {
        H264Bitrate,
        H264CRF,
        H264NVENC,
        Custom
    }

    public class OutputArgsModel : INotifyPropertyChanged
    {
        public string OutputLocation { get; set; } = "";

        public string FFmpegArgs { get; set; } = "";

        public OutputType SelectedOutputType { get; set; } = OutputType.H264CRF;

        public int Bitrate { get; set; } = 20000;
        public int CRF { get; set; } = 17;

        public double StartOffset { get; set; } = 0;

        public bool UseAudio { get; set; } = false;
        public string AudioLocation { get; set; } = "";
        public bool UseMaskOutput { get; set; } = false;
        public string MaskOutputLocation { get; set; } = "";

        public string OutputArgs { get; set; }

        public OutputArgsModel()
        {
            PropertyChanged += OutputArgsModel_PropertyChanged;
            RebuildArgs();
        }

        private void OutputArgsModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (
                e.PropertyName == nameof(Bitrate) ||
                e.PropertyName == nameof(CRF) ||
                e.PropertyName == nameof(AudioLocation) ||
                e.PropertyName == nameof(SelectedOutputType)
                )
            {
                RebuildArgs();
            }

            if(e.PropertyName == nameof(UseMaskOutput) && UseMaskOutput)
            {
                if(OutputLocation != "")
                {
                    try
                    {
                        MaskOutputLocation = Path.Combine(
                                Path.GetDirectoryName(OutputLocation),
                                Path.GetFileNameWithoutExtension(OutputLocation) +
                                ".mask" +
                                Path.GetExtension(OutputLocation)
                            );
                    }
                    catch { }
                }
            }
        }

        void RebuildArgs()
        {
            if (SelectedOutputType == OutputType.Custom) return;

            if (SelectedOutputType == OutputType.H264Bitrate)
            {
                FFmpegArgs = $"-pix_fmt yuv420p -vcodec libx264 -b:v {Bitrate}";
            }
            else if (SelectedOutputType == OutputType.H264CRF)
            {
                FFmpegArgs = $"-pix_fmt yuv420p -vcodec libx264 -crf {CRF}";
            }
            else if (SelectedOutputType == OutputType.H264NVENC)
            {
                FFmpegArgs = $"-pix_fmt yuv420p -vcodec h264_nvenc -b:v {Bitrate}";
            }
        }

        public string ValidateAndGetRenderArgs(double startOffset)
        {
            if (UseMaskOutput)
            {
                if (MaskOutputLocation == "") throw new UIException("Please specify mask output file if rendering with mask");
                if (!Directory.Exists(Path.GetDirectoryName(MaskOutputLocation)))
                {
                    throw new Exception("Directory for mask output does not exist");
                }
            }
            if (OutputLocation == "") throw new UIException("Please specify output file");
            if (!Directory.Exists(Path.GetDirectoryName(OutputLocation)))
            {
                throw new Exception("Directory for video output does not exist");
            }

            string args = "";

            if (UseAudio)
            {
                if (AudioLocation == "") throw new UIException("Please specify input audio file if incuding audio");
                if (!File.Exists(AudioLocation)) throw new UIException("Specified input audio file doesn't exist");
                args += $"-itsoffset {startOffset.ToString().Replace(",", ".")} -i \"{AudioLocation}\" -acodec aac ";
            }

            args += FFmpegArgs;

            return args;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
