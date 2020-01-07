using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace BMEngine.UI
{
    public class BBinding : Binding
    {
        public BBinding(DependencyProperty dp, object source) : base()
        {
            Path = new PropertyPath(dp);
            Source = source;
        }
    }
}
