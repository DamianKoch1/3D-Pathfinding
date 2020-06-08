using Pathfinding;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TempNodeDictionary : Dictionary<Node, Node>
{
    public void AddNode(Node key, Vector3 position)
    {
        var temp = new Node(position, 1);
        temp.neighbours = key.neighbours;
        key.neighbours.Add(temp);
        temp.neighbours.Add(key);
        Add(key, temp);
    }

    public void Cleanup(Action<Node> loopAction = null)
    {
        foreach (var keyNode in Keys)
        {
            loopAction?.Invoke(this[keyNode]);
            keyNode.neighbours.Remove(this[keyNode]);
        }
    }
}
