using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding.Containers
{
    public class BucketList<T> : IEnumerable<T> where T : IBucketItem
    {
        public List<List<T>> buckets;

        public readonly int bucketRange;

        public readonly float minBucketValue;

        public int Count { private set; get; }

        public BucketList(int _bucketRange, float _minBucketValue)
        {
            bucketRange = _bucketRange;
            minBucketValue = _minBucketValue;
            buckets = new List<List<T>>();
            Count = 0;
        }

        public void Add(T item)
        {
            var bucket = (int)(item.GetBucketValue() - minBucketValue) / bucketRange;
            if (bucket >= buckets.Count)
            {
                for (int i = buckets.Count; i <= bucket; i++)
                {
                    buckets.Add(new List<T>());
                }
            }
            buckets[bucket].Add(item);
            Count++;
        }

        public bool Contains(T item)
        {
            var bucket = (int)(item.GetBucketValue() - minBucketValue) / bucketRange;
            if (bucket < 0) return false;
            if (bucket >= buckets.Count) return false;
            if (buckets[bucket].Count == 0) return false;
            return buckets[bucket].Contains(item);
        }

        public bool Remove(T item)
        {
            var bucket = (int)item.GetBucketValue() / bucketRange;
            if (bucket >= buckets.Count) return false;
            if (buckets[bucket].Count == 0) return false;
            if (buckets[bucket].Remove(item))
            {
                Count--;
                return true;
            }
            return false;
        }

        public T ExtractMin()
        {
            for (int i = 0; i < buckets.Count; i++)
            {
                if (buckets[i].Count == 0) continue;
                for (int j = 0; j < buckets[i].Count; j++)
                {
                    float lowest = Mathf.Infinity;
                    T lowestItem = default;
                    var currItem = buckets[i][j];
                    if (currItem.GetBucketValue() < lowest)
                    {
                        lowest = currItem.GetBucketValue();
                        lowestItem = currItem;
                    }
                    buckets[i].RemoveAt(j);
                    return lowestItem;
                }
            }
            return default;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < buckets.Count; i++)
            {
                foreach (var item in buckets[i])
                {
                    yield return item;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
    public interface IBucketItem
    {
        float GetBucketValue();
    }
}
