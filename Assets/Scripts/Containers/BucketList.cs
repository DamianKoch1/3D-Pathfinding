using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding.Containers
{
    public class BucketList<T> : IEnumerable<T> where T : IBucketElement
    {
        public List<HashSet<T>> buckets;

        public readonly int bucketRange;

        public int Count { private set; get; }

        public BucketList(int _bucketRange)
        {
            bucketRange = _bucketRange;
            buckets = new List<HashSet<T>>();
            Count = 0;
        }

        public void Add(T item)
        {
            var bucket = (int)item.GetBucketValue() / bucketRange;
            if (bucket >= buckets.Count)
            {
                for (int i = buckets.Count; i <= bucket; i++)
                {
                    buckets.Add(new HashSet<T>());
                }
            }
            buckets[bucket].Add(item);
            Count++;
        }

        public bool Contains(T item)
        {
            var bucket = (int)item.GetBucketValue() / bucketRange;
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

        public T GetLowest()
        {
            for (int i = 0; i < buckets.Count; i++)
            {
                foreach (var item in buckets[i])
                {
                    float lowest = Mathf.Infinity;
                    T lowestItem = default;
                    if (item.GetBucketValue() < lowest)
                    {
                        lowest = item.GetBucketValue();
                        lowestItem = item;
                    }
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
    public interface IBucketElement
    {
        float GetBucketValue();
    }
}
