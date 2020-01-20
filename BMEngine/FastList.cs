using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenithEngine
{
    public class FastList<T> : IEnumerable<T>
    {
        private class ListItem
        {
            public ListItem Next;
            public T item;
        }

        private ListItem root = new ListItem();
        private ListItem last = null;

        public T First
        {
            get
            {
                if (root.Next != null) return root.Next.item;
                else return default(T);
            }
        }
        public class Iterator
        {
            FastList<T> _ilist;

            private ListItem prev;
            private ListItem curr;

            internal Iterator(FastList<T> ll)
            {
                _ilist = ll;
                Reset();
            }

            public bool MoveNext(out T v)
            {
                ListItem ll = curr.Next;

                if (ll == null)
                {
                    v = default(T);
                    _ilist.last = curr;
                    return false;
                }

                v = ll.item;

                prev = curr;
                curr = ll;

                return true;
            }

            public void Remove()
            {
                if (_ilist.last.Equals(curr)) _ilist.last = prev;
                prev.Next = curr.Next;
            }

            public void Insert(T item)
            {
                var i = new ListItem()
                {
                    item = item,
                    Next = curr
                };
                if (prev == null)
                    _ilist.root.Next = i;
                else
                    prev.Next = i;
                //if (curr.Equals(_ilist.last))
                //{
                //    _ilist.last = curr;
                //}
            }

            public void Reset()
            {
                this.prev = null;
                this.curr = _ilist.root;
            }
        }

        public class FastIterator : IEnumerator<T>
        {
            FastList<T> _ilist;

            private ListItem curr;

            internal FastIterator(FastList<T> ll)
            {
                _ilist = ll;
                Reset();
            }

            public object Current => curr.item;

            T IEnumerator<T>.Current => curr.item;

            public void Dispose()
            {

            }

            public bool MoveNext()
            {
                try
                {
                    curr = curr.Next;

                    return curr != null;
                }
                catch { return false; }
            }

            public void Reset()
            {
                this.curr = _ilist.root;
            }
        }

        public void Add(T item)
        {
            ListItem li = new ListItem();
            li.item = item;

            if (root.Next != null && last != null)
            {
                while (last.Next != null) last = last.Next;
                last.Next = li;
            }
            else
                root.Next = li;

            last = li;
        }

        public T Pop()
        {
            ListItem el = root.Next;
            root.Next = el.Next;
            return el.item;
        }

        public Iterator Iterate()
        {
            return new Iterator(this);
        }

        public bool ZeroLen => root.Next == null;

        public IEnumerator<T> FastIterate()
        {
            return new FastIterator(this);
        }

        public void Unlink()
        {
            root.Next = null;
            last = null;
        }

        public int Count()
        {
            int cnt = 0;

            ListItem li = root.Next;
            while (li != null)
            {
                cnt++;
                li = li.Next;
            }

            return cnt;
        }

        public bool Any()
        {
            return root.Next != null;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return FastIterate();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return FastIterate();
        }
    }
}
