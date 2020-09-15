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
            for (int i = items.Count - 1; i >= 0; i--)
            {
                items[i].Dispose();
            }
            disposed = true;
        }

        public T Add<T>(T item) where T : IDisposable
        {
            if (disposed) throw new Exception("Can't add items to a disposed DisposeGroup");
            items.Add(item);
            return item;
        }

        public T Replace<T>(T prevItem, T newItem) where T : IDisposable
        {
            if (disposed) throw new Exception("Can't add items to a disposed DisposeGroup");
            if (prevItem != null)
            {
                if (!items.Contains(prevItem)) throw new ArgumentException("Previous item not found in items array");
                items.Remove(prevItem);
                prevItem.Dispose();
            }
            items.Add(newItem);
            return newItem;
        }

        public T Replace<T>(ref T prevItem, T newItem) where T : IDisposable
        {
            prevItem = Replace(prevItem, newItem);
            return newItem;
        }

        public void Remove<T>(T item) where T : IDisposable
        {
            if (item != null)
            {
                if (!items.Contains(item)) throw new ArgumentException("Item not found in items array");
                items.Remove(item);
                item.Dispose();
            }
        }

        public void Remove<T>(ref T item) where T : IDisposable
        {
            Remove(item);
            item = default(T);
        }
    }
}
