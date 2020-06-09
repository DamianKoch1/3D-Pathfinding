using Pathfinding;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding
{
    /// <summary>
    /// Inserts temporary pathfinding nodes near real nodes on a graph and sets their neighbours, removes temp nodes from neighbours on CleanUp to free for GC
    /// </summary>
    public class TempNodeDictionary : Dictionary<Node, Node>
    {
        /// <summary>
        /// Creates temporary node at position, lets node and key save eachother as neighbours
        /// </summary>
        /// <param name="key"></param>
        /// <param name="position"></param>
        public void AddNode(Node key, Vector3 position)
        {
            var temp = new Node(position, 1);
            temp.neighbours = new List<Node>(key.neighbours);
            key.neighbours.Add(temp);
            temp.neighbours.Add(key);
            Add(key, temp);
        }

        /// <summary>
        /// Removes all temp nodes from the original nodes neighbours, invokes given action to e.g. remove temp nodes from open list
        /// </summary>
        /// <param name="clearReferences"></param>
        public void Cleanup(Action<Node> clearReferences = null)
        {
            foreach (var keyNode in Keys)
            {
                clearReferences?.Invoke(this[keyNode]);
                keyNode.neighbours.Remove(this[keyNode]);
            }
        }
    }
}
