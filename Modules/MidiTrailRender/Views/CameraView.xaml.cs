using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MIDITrailRender.Views
{
    /// <summary>
    /// Interaction logic for CameraView.xaml
    /// </summary>
    public partial class CameraView : UserControl
    {
        double StoN(double v)
        {
            return Math.Sign(v) * Math.Pow(Math.Abs(v), 2);
        }

        double NtoS(double v)
        {
            return Math.Sign(v) * Math.Pow(Math.Abs(v), 1.0 / 2);
        }

        public CameraView()
        {
            InitializeComponent();

            xPos.NudToSlider = NtoS;
            xPos.SliderToNud = StoN;

            yPos.NudToSlider = NtoS;
            yPos.SliderToNud = StoN;

            zPos.NudToSlider = NtoS;
            zPos.SliderToNud = StoN;

            zOffset.NudToSlider = NtoS;
            zOffset.SliderToNud = StoN;
        }
    }
}
