using System;
using System.Collections;
using System.Collections.Generic;

namespace Wism.Client.AI.Common
{
    public class SimplePriorityQueue<T> : IEnumerable<T>
    {
        private readonly SortedList<Pair<int>, T> innerList;
        private int hashIndex;

        public SimplePriorityQueue()
        {
            this.innerList = new SortedList<Pair<int>, T>(new PairComparer<int>());
        }

        public int Count => this.innerList.Count;

        public bool IsReadOnly => throw new NotImplementedException();

        public IEnumerator<T> GetEnumerator()
        {
            return (IEnumerator<T>)this.innerList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.innerList.GetEnumerator();
        }

        public void Enqueue(T item, int priority)
        {
            // Hash index is to handle duplicates of priority
            this.innerList.Add(new Pair<int>(priority, this.hashIndex++), item);
        }

        public T Dequeue()
        {
            if (this.innerList.Count == 0)
            {
                throw new InvalidOperationException("No items to dequeue.");
            }

            var item = this.innerList[this.innerList.Keys[0]];
            this.innerList.RemoveAt(0);
            return item;
        }

        public void Remove(T item)
        {
            var index = this.innerList.IndexOfValue(item);
            if (index >= 0)
            {
                this.innerList.RemoveAt(index);
            }
        }

        public bool Contains(T item)
        {
            return this.innerList.ContainsValue(item);
        }
    }

    public class Pair<T>
    {
        public Pair(T first, T second)
        {
            this.First = first;
            this.Second = second;
        }

        public T First { get; }
        public T Second { get; }

        public override int GetHashCode()
        {
            return this.First.GetHashCode() ^ this.Second.GetHashCode();
        }

        public override bool Equals(object other)
        {
            var pair = other as Pair<T>;
            if (pair == null)
            {
                return false;
            }

            return this.First.Equals(pair.First) && this.Second.Equals(pair.Second);
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

            if (x.First.CompareTo(y.First) > 0)
            {
                return -1;
            }

            return x.Second.CompareTo(y.Second);
        }
    }
}