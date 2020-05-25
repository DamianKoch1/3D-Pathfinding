using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Grid
{
    /// <summary>
    /// Used to get iso value of node with corresponding index, checks neighbours if index out of range, 1 if no neighbour
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public float this[int x, int y, int z]
    {
        get
        {
            if (x >= xSize)
            {
                if (xNeighbour == null) return 1;
                return xNeighbour[x - xSize, y, z];
            }
            if (y >= ySize)
            {
                if (yNeighbour == null) return 1;
                return yNeighbour[x, y - ySize, z];
            }
            if (z >= zSize)
            {
                if (zNeighbour == null) return 1;
                return zNeighbour[x, y, z - zSize];
            }
            return nodes[x, y, z].isoValue;
        }
    }

    public Node[,,] nodes;

    public Vector3 center;

    public Vector3 extents;

    public Vector3 step;

    public int xSize;
    public int ySize;
    public int zSize;

    //preventing chunk gaps
    public Grid xNeighbour;
    public Grid yNeighbour;
    public Grid zNeighbour;

    public Grid(Vector3 _center, GridGenerationSettings settings, Func<Vector3, float> GetIsoValue)
    {
        settings.step.x = Mathf.Max(settings.step.x, 0.5f);
        settings.step.y = Mathf.Max(settings.step.y, 0.5f);
        settings.step.z = Mathf.Max(settings.step.z, 0.5f);

        center = _center;
        extents = settings.chunkSize;
        step = settings.step;
        xSize = (int)(extents.x / step.x);
        ySize = (int)(extents.y / step.y);
        zSize = (int)(extents.z / step.z);

        nodes = new Node[xSize, ySize, zSize];

        Vector3 pos = center - extents / 2;
        for (int x = 0; x < xSize; x++)
        {
            for (int y = 0; y < ySize; y++)
            {
                for (int z = 0; z < zSize; z++)
                {
                    nodes[x, y, z] = new Node(pos, GetIsoValue(pos), x, y, z);
                    pos.z += step.z;
                }
                pos.z = center.z - extents.z / 2;
                pos.y += step.y;
            }
            pos.y = center.y - extents.y / 2;
            pos.x += step.x;
        }

        for (int x = 0; x < xSize; x++)
        {
            for (int y = 0; y < ySize; y++)
            {
                for (int z = 0; z < zSize; z++)
                {
                    ref var node = ref nodes[x, y, z];
                    node.neighbours = GetNeighbours(node, settings.allowDiagonalNeighbours);
                }
            }
        }
    }

    public void DrawGizmos(Color color, float isoLevel)
    {
        foreach (var node in nodes)
        {
            node.DrawGizmos(color, isoLevel);
        }
    }

    private void AddNeighbour(Node node, HashSet<Node> neighbours, int dirX, int dirY, int dirZ)
    {
        dirX = Mathf.Clamp(dirX, -1, 1);
        dirY = Mathf.Clamp(dirY, -1, 1);
        dirZ = Mathf.Clamp(dirZ, -1, 1);

        int x = node.iX + dirX;
        if (x < 0 || x > xSize - 1) return;

        int y = node.iY + dirY;
        if (y < 0 || y > ySize - 1) return;

        int z = node.iZ + dirZ;
        if (z < 0 || z > zSize - 1) return;

        neighbours.Add(nodes[x, y, z]);
    }

    public HashSet<Node> GetNeighbours(Node node, bool includeDiagonal = true)
    {
        if (node.iX < 0 || node.iY < 0 || node.iZ < 0) return null;
        HashSet<Node> neighbours = new HashSet<Node>();
        AddNeighbour(node, neighbours, -1, 0, 0);
        AddNeighbour(node, neighbours, 1, 0, 0);
        AddNeighbour(node, neighbours, 0, -1, 0);
        AddNeighbour(node, neighbours, 0, 1, 0);
        AddNeighbour(node, neighbours, 0, 0, -1);
        AddNeighbour(node, neighbours, 0, 0, 1);
        if (includeDiagonal)
        {
            AddNeighbour(node, neighbours, -1, 1, -1);
            AddNeighbour(node, neighbours, -1, 1, 0);
            AddNeighbour(node, neighbours, -1, 1, 1);
            AddNeighbour(node, neighbours, 0, 1, 1);
            AddNeighbour(node, neighbours, 1, 1, 1);
            AddNeighbour(node, neighbours, 1, 1, 0);
            AddNeighbour(node, neighbours, 1, 1, -1);
            AddNeighbour(node, neighbours, 0, 1, -1);
            AddNeighbour(node, neighbours, 1, 0, -1);
            AddNeighbour(node, neighbours, 1, 0, 1);
            AddNeighbour(node, neighbours, -1, 0, 1);
            AddNeighbour(node, neighbours, -1, 0, -1);
            AddNeighbour(node, neighbours, -1, -1, -1);
            AddNeighbour(node, neighbours, -1, -1, 0);
            AddNeighbour(node, neighbours, -1, -1, 1);
            AddNeighbour(node, neighbours, 0, -1, 1);
            AddNeighbour(node, neighbours, 1, -1, 1);
            AddNeighbour(node, neighbours, 1, -1, 0);
            AddNeighbour(node, neighbours, 1, -1, -1);
            AddNeighbour(node, neighbours, 0, -1, -1);
        }
        return neighbours;
    }

    //TODO error if node isnt walkable, should also find adjacent node closest to target instead, probably insert a temporary node here
    public Node GetClosestNode(Vector3 position)
    {
        Vector3 startCorner = center - extents / 2;


        int x = Mathf.RoundToInt((position.x - startCorner.x) / step.x);
        x = Mathf.Clamp(x, 0, xSize - 1);

        int y = Mathf.RoundToInt((position.y - startCorner.y) / step.y);
        y = Mathf.Clamp(y, 0, ySize - 1);

        int z = Mathf.RoundToInt((position.z - startCorner.z) / step.z);
        z = Mathf.Clamp(z, 0, zSize - 1);

        return nodes[x, y, z];
    }


    public void ResetNodes()
    {
        foreach (var node in nodes)
        {
            if (node.F !=  -1)
            {
                node.cost = -1;
                node.heuristic = -1;
            }
        }
    }

    public LinkedList<Node> openNodes;
    public LinkedList<Node> closedNodes;
    public int neighbourChecks;
    public Stack<Vector3> FindPath(Vector3 start, Vector3 end, PathfindingSettings settings, float isoLevel)
    {
        ResetNodes();

        neighbourChecks = 0;
        //distance from start to end
        float distance = 0;

        //full length of path
        float pathLength = 0;

        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

        if (settings.benchmark)
        {
            distance = Vector3.Distance(start, end);
            sw.Start();
        }

        var startNode = GetClosestNode(start);
        var endNode = GetClosestNode(end);

        var startCapacity = nodes.Length / 10;
        Stack<Vector3> path = new Stack<Vector3>(startCapacity);

        //linkedlist vs list
        openNodes = new LinkedList<Node>();
        closedNodes = new LinkedList<Node>();

        Node current = startNode;
        current.costHeuristicBalance = settings.greediness;
        current.cost = 0;
        current.heuristic = settings.Heuristic(current.pos, endNode.pos);
        openNodes.AddLast(startNode);

        while (openNodes.Count != 0 && !closedNodes.Contains(endNode))
        {
            current = openNodes.First();
            openNodes.RemoveFirst();
            closedNodes.AddLast(current);

            foreach (var neighbour in current.neighbours)
            {
                neighbourChecks++;
                if (neighbour.isoValue > isoLevel)
                {
                    if (!closedNodes.Contains(neighbour))
                    {
                        if (!openNodes.Contains(neighbour))
                        {
                            neighbour.costHeuristicBalance = settings.greediness;
                            neighbour.previousPathNode = current;
                            neighbour.heuristic = settings.Heuristic(neighbour.pos, endNode.pos);
                            neighbour.cost = neighbour.previousPathNode.cost + settings.CostIncrease(neighbour);
                            openNodes.AddLast(neighbour);
                            openNodes = new LinkedList<Node>(openNodes.OrderBy(n => n.F));
                        }
                    }
                }
            }
        }

        if (!closedNodes.Contains(endNode))
        {
            return null;
        }

        Node temp = current;
        path.Push(end);
        while (temp != null)
        {
            pathLength += Vector3.Distance(path.Peek(), temp.pos);
            path.Push(temp.pos);
            temp = temp.previousPathNode;
        }
        pathLength += Vector3.Distance(path.Peek(), start);

        if (settings.benchmark)
        {
            sw.Stop();
            Debug.Log("Heuristic: " + settings.heuristic + ", Cost increase: " + settings.costIncrease + ", Path length: " + pathLength * 100 / distance + "%, ms: " + sw.Elapsed.Milliseconds + ", closed: " + closedNodes.Count + ", visited: " + openNodes.Count + ", Neighbour checks: " + neighbourChecks);
        }

        return path;

    }

}
