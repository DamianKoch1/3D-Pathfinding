using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
    public Vector3 pos;

    public Node previousPathNode;

    public float heuristic = -1;

    public float cost = -1;

    public Vector3Int idx => new Vector3Int(iX, iY, iZ);

    public int iX;
    public int iY;
    public int iZ;

    public float isoValue;

    public HashSet<Node> neighbours;

    public float costHeuristicBalance = 0.5f;

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

    public Node(Vector3 _pos, float _walkable, int _iX = -1, int _iY = -1, int _iZ = -1)
    {
        neighbours = new HashSet<Node>();
        pos = _pos;
        iX = _iX;
        iY = _iY;
        iZ = _iZ;
        isoValue = _walkable;
    }

    public void DrawGizmos(Color color, float isoLevel)
    {
        Gizmos.color = isoValue >= isoLevel ? color : Color.red;
        Gizmos.DrawWireCube(pos, Vector3.one * 0.1f);
    }

}
