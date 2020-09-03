using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zenith.Models
{
    public class UIException : Exception
    {
        public UIException(string message) : base(message) { }
    }

    public class OutputErrors
    {
        public static UIException OutputNotSpecified => new UIException("Please specify output file");
    }

    public static class Errors
    {

    }
}
