using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node : IComparable<Node>
{
    public Vector3 pos;

    public Node previousPathNode;

    public float heuristic = -1;

    public float cost = -1;

    public Vector3Int idx => new Vector3Int(x, y, z);

    public int x;
    public int y;
    public int z;

    public float isoValue;

    //grid doesn't have duplicates, maybe changing mesh graph from vertices to triangle centers so no HashSet needed here
    public List<Node> neighbours;

    public float costHeuristicBalance = 0.5f;

    /// <summary>
    /// temp function used while mesh vertex graph uses vertices as nodes
    /// </summary>
    /// <param name="node"></param>
    public void AddUniqueNeighbour(Node node)
    {
        if (neighbours.Contains(node)) return;
        neighbours.Add(node);
    }

    public float F
    {
        get
        {
            if (heuristic != -1 && cost != -1)
            {
                return heuristic * costHeuristicBalance + cost * (1 - costHeuristicBalance);
            }
            return -1;
        }
    }

    public Node(Vector3 _pos, float _isoValue, int iX = -1, int iY = -1, int iZ = -1)
    {
        neighbours = new List<Node>(26);
        pos = _pos;
        x = iX;
        y = iY;
        z = iZ;
        isoValue = _isoValue;
    }

    public void DrawGizmos(Color color, float isoLevel)
    {
        Gizmos.color = isoValue >= isoLevel ? color : Color.red;
        Gizmos.DrawWireCube(pos, Vector3.one * 0.1f);
    }


    public int CompareTo(Node other)
    {
        if (F > other.F) return 1;
        if (F < other.F) return -1;
        //experiment with tie breakers, 1 for older or -1 first, also try comparing cost / heuristic
        return 1;
    }
}
