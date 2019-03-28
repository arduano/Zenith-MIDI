using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMEngine
{
    public class FastList<T> : IEnumerable
    {
        private class ListItem
        {
            public ListItem Next;
            public T item;
        }

        private ListItem root = new ListItem();
        private ListItem last = null;

        public T First {
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
                if (_ilist.last == curr) _ilist.last = prev;
                prev.Next = curr.Next;
            }

            public void Reset()
            {
                this.prev = null;
                this.curr = _ilist.root;
            }
        }

        public class FastIterator : IEnumerator
        {
            FastList<T> _ilist;
            
            private ListItem curr;

            internal FastIterator(FastList<T> ll)
            {
                _ilist = ll;
                Reset();
            }

            public object Current => curr.item;
            public bool MoveNext()
            {
                curr = curr.Next;
                
                return curr != null;
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
                last.Next = li;
            else
                root.Next = li;

            last = li;
        }

        public bool ZeroLen => root.Next == null;

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

        public IEnumerator FastIterate()
        {
            if (root.Next == null) return new Note[0].GetEnumerator();
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

        public IEnumerator GetEnumerator()
        {
            return FastIterate();
        }
    }
}
