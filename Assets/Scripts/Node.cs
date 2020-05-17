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

    public bool walkable;

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

    public Node(Vector3 _pos, bool _walkable, int _iX = -1, int _iY = -1, int _iZ = -1)
    {
        pos = _pos;
        iX = _iX;
        iY = _iY;
        iZ = _iZ;
        walkable = _walkable;
    }

    public static bool[,,] ToBoolArray(Node[,,] nodes)
    {
        int maxX = nodes.GetUpperBound(0);
        int maxY = nodes.GetUpperBound(1);
        int maxZ = nodes.GetUpperBound(2);
        var retVal = new bool[maxX + 1, maxY + 1, maxZ + 1];
        for (int x = 0; x <= maxX; x++)
        {
            for (int y = 0; y <= maxY; y++)
            {
                for (int z = 0; z <= maxZ; z++)
                {
                    retVal[x, y, z] = nodes[x, y, z]?.walkable == true;
                }
            }
        }
        return retVal;
    }

}
