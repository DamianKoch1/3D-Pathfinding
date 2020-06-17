using System;
using System.Collections;
using System.Collections.Generic;

namespace Pathfinding.Containers
{
    /// <summary>
    /// Binary tree where children are always larger than parent, items store their tree indices to improve Contains check speed
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MinHeap<T> : IEnumerable<T> where T : IHeapItem<T>
    {
        private List<T> items;

        public int Count => items.Count;

        private int GetLeftChildIndex(int itemIndex) => 2 * itemIndex + 1;

        private int GetRightChildIndex(int itemIndex) => 2 * itemIndex + 2;

        private int GetParentIndex(int itemIndex) => (itemIndex - 1) / 2;

        public MinHeap(int capacity = 16)
        {
            items = new List<T>(capacity);
        }

        public void Add(T item)
        {
            int count = Count;
            item.HeapIndex = count;
            items.Add(item);
            BubbleUp(count);
        }

        public bool Contains(T item)
        {
            if (item.HeapIndex < 0 || item.HeapIndex >= Count) return false;
            return items[item.HeapIndex].Equals(item);
        }

        public T ExtractMin()
        {
            T min = items[0];
            items[0] = items[Count - 1];
            items[0].HeapIndex = 0;
            items.RemoveAt(Count - 1);
            if (Count > 1)
            {
                BubbleDown(0);
            }
            return min;
        }

        public void Update(T item)
        {
            BubbleUp(item.HeapIndex);
        }

        private void BubbleDown(int index)
        {
            while (true)
            {
                int left = GetLeftChildIndex(index);
                int right = GetRightChildIndex(index);
                int child = 0;

                if (left >= Count) return;

                child = left;

                if (right < Count)
                {
                    if (items[left].CompareTo(items[right]) < 0)
                    {
                        child = right;
                    }
                }

                if (items[index].CompareTo(items[child]) <= 0) return;

                Swap(index, child);
                index = child;
            }
        }

        private void BubbleUp(int index)
        {
            int parent = GetParentIndex(index);

            while (index > 0 && index < Count)
            {
                if (items[parent].CompareTo(items[index]) >= 0) return;
                Swap(index, parent);
                index = parent;
                parent = GetParentIndex(parent);
            }
        }

        void Swap(int index1, int index2)
        {
            var temp = items[index1];
            items[index1] = items[index2];
            items[index2] = temp;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

    }
    public interface IHeapItem<T> : IEquatable<T>, IComparable<T>
    {
        int HeapIndex { set; get; }
    }
}
