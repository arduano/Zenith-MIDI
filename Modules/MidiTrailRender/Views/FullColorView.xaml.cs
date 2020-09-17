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
    /// Interaction logic for FullColorView.xaml
    /// </summary>
    public partial class FullColorView : UserControl
    {
        public bool ShowWater
        {
            get { return (bool)GetValue(ShowWaterProperty); }
            set { SetValue(ShowWaterProperty, value); }
        }

        public static readonly DependencyProperty ShowWaterProperty =
            DependencyProperty.Register("ShowWater", typeof(bool), typeof(FullColorView), new PropertyMetadata(true));


        public FullColorView()
        {
            InitializeComponent();
        }
    }
}
