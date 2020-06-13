using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenithEngine
{
    public class DisposeGroup : IDisposable
    {
        List<IDisposable> items = new List<IDisposable>();

        bool disposed = false;

        public void Dispose()
        {
            if (disposed) return;
            foreach (var i in items)
                i.Dispose();
            disposed = true;
        }

        public T Add<T>(T item) where T : IDisposable
        {
            if (disposed) throw new Exception("Can't add items to a disposed DisposeGroup");
            items.Add(item);
            return item;
        }
    }
}
