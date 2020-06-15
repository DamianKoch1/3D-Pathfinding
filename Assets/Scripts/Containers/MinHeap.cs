using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding.Containers
{
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
                BubbleDown(items[0]);
            }
            return min;
        }

        public void Update(T item)
        {
            BubbleUp(item.HeapIndex);
        }

        private void BubbleDown(T item)
        {
            while (true)
            {
                int childIndexLeft = item.HeapIndex * 2 + 1;
                int childIndexRight = item.HeapIndex * 2 + 2;
                int child = 0;

                if (childIndexLeft < Count)
                {
                    child = childIndexLeft;

                    if (childIndexRight < Count)
                    {
                        if (items[childIndexLeft].CompareTo(items[childIndexRight]) < 0)
                        {
                            child = childIndexRight;
                        }
                    }

                    if (item.CompareTo(items[child]) > 0)
                    {
                        Swap(item.HeapIndex, child);
                    }
                    else return;
                }
                else return;
            }
        }

        private void BubbleUp(int index)
        {
            int parent = GetParentIndex(index);

            while (index > 0 && index < Count)
            {
                if (items[parent].CompareTo(items[index]) < 0)
                {
                    Swap(index, parent);
                }
                else return;
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
