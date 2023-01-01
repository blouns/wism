using System;
using System.Collections;
using System.Collections.Generic;

namespace Wism.Client.AI.Common
{
    public class SimplePriorityQueue<T> : IEnumerable<T>
    {
        private SortedList<Pair<int>, T> innerList;
        private int hashIndex = 0;

        public int Count { get => innerList.Count; }

        public bool IsReadOnly => throw new NotImplementedException();

        public SimplePriorityQueue()
        {
            innerList = new SortedList<Pair<int>, T>(new PairComparer<int>());
        }

        public void Enqueue(T item, int priority)
        {
            // Hash index is to handle duplicates of priority
            innerList.Add(new Pair<int>(priority, hashIndex++), item);
        }

        public T Dequeue()
        {
            if (innerList.Count == 0)
            {
                throw new InvalidOperationException("No items to dequeue.");
            }    

            T item = innerList[innerList.Keys[0]];
            innerList.RemoveAt(0);
            return item;
        }

        public void Remove(T item)
        {
            int index = this.innerList.IndexOfValue(item);
            if (index >= 0)
            {
                this.innerList.RemoveAt(index);
            }
        }

        public bool Contains(T item)
        {
            return this.innerList.ContainsValue(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return (IEnumerator<T>)this.innerList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.innerList.GetEnumerator();
        }
    }

    public class Pair<T>
    {
        public T First { get; private set; }
        public T Second { get; private set; }

        public Pair(T first, T second)
        {
            First = first;
            Second = second;
        }

        public override int GetHashCode()
        {
            return First.GetHashCode() ^ Second.GetHashCode();
        }

        public override bool Equals(object other)
        {
            Pair<T> pair = other as Pair<T>;
            if (pair == null)
            {
                return false;
            }
            return (this.First.Equals(pair.First) && this.Second.Equals(pair.Second));
        }
    }

    // Sort highest item first
    public class PairComparer<T> : IComparer<Pair<T>> where T : IComparable
    {
        public int Compare(Pair<T> x, Pair<T> y)
        {
            if (x.First.CompareTo(y.First) < 0)
            {
                return 1;
            }
            else if (x.First.CompareTo(y.First) > 0)
            {
                return -1;
            }
            else
            {
                return x.Second.CompareTo(y.Second);
            }
        }
    }


}
