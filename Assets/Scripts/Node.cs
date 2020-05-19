using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
    public Vector3 pos;

    public Node previousPathNode;

    public float targetDistance = -1;

    public float cost = 1;

    public int iX;
    public int iY;
    public int iZ;

    public float isoValue;

    public List<Node> neighbours;

    public float F
    {
        get
        {
            if (targetDistance != -1 && cost != -1)
            {
                return targetDistance + cost;
            }
            return -1;
        }
    }

    public Node(Vector3 _pos, float _walkable, int _iX = -1, int _iY = -1, int _iZ = -1)
    {
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
