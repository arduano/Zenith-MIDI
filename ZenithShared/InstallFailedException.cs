using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenithShared
{
    class InstallFailedException : Exception
    {
        public InstallFailedException(string message) : base(message) { }
    }
}
