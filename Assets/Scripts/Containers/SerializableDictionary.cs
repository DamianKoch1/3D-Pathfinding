using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding.Serialization
{
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        /// <summary>
        /// Stores entries into keys / values lists
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="values"></param>
        public void Serialize(ref List<TKey> keys, ref List<TValue> values)
        {
            keys.Clear();
            values.Clear();
            if (keys.Capacity < Count)
            {
                keys.Capacity = Count - 1;
            }
            if (values.Capacity < Count)
            {
                values.Capacity = Count - 1;
            }
            foreach (KeyValuePair<TKey, TValue> pair in this)
            {
                keys.Add(pair.Key);
                values.Add(pair.Value);
            }
        }

        /// <summary>
        /// Adds keys / values from lists to dictionary
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="values"></param>
        public void Deserialize(List<TKey> keys, List<TValue> values)
        {
            if (keys.Count != values.Count)
            {
                Debug.LogError("Couldn't deserialize dictionary, inequal key/value count!");
                return;
            }
            for (int i = 0; i < keys.Count; i++)
            {
                Add(keys[i], values[i]);
            }
        }
    }
}
