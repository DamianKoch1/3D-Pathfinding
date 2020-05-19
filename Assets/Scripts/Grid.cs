using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Grid
{

    public Node[,,] nodes;

    public Vector3 center;

    public Vector3 size;

    public Vector3 step;

    public int maxX;
    public int maxY;
    public int maxZ;

    public Grid(Vector3 _center, GridGenerationSettings settings, Func<Vector3, float> GetIsoValue)
    {
        settings.step.x = Mathf.Max(settings.step.x, 0.5f);
        settings.step.y = Mathf.Max(settings.step.y, 0.5f);
        settings.step.z = Mathf.Max(settings.step.z, 0.5f);

        center = _center;
        size = settings.size;
        step = settings.step;
        maxX = (int)(size.x / step.x);
        maxY = (int)(size.y / step.y);
        maxZ = (int)(size.z / step.z);

        nodes = new Node[maxX, maxY, maxZ];

        Vector3 pos = center - size / 2;
        for (int x = 0; x < maxX; x++)
        {
            for (int y = 0; y < maxY; y++)
            {
                for (int z = 0; z < maxZ; z++)
                {
                    nodes[x, y, z] = new Node(pos, GetIsoValue(pos), x, y, z);
                    pos.z += step.z;
                }
                pos.z = center.z - size.z / 2;
                pos.y += step.y;
            }
            pos.y = center.y - size.y / 2;
            pos.x += step.x;
        }

        for (int x = 0; x < maxX; x++)
        {
            for (int y = 0; y < maxY; y++)
            {
                for (int z = 0; z < maxZ; z++)
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
        if (x < 0 || x > maxX - 1) return;

        int y = node.iY + dirY;
        if (y < 0 || y > maxY - 1) return;

        int z = node.iZ + dirZ;
        if (z < 0 || z > maxZ - 1) return;

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
        Vector3 startCorner = center - size / 2;


        int x = Mathf.RoundToInt((position.x - startCorner.x) / step.x);
        x = Mathf.Clamp(x, 0, maxX - 1);

        int y = Mathf.RoundToInt((position.y - startCorner.y) / step.y);
        y = Mathf.Clamp(y, 0, maxY - 1);

        int z = Mathf.RoundToInt((position.z - startCorner.z) / step.z);
        z = Mathf.Clamp(z, 0, maxZ - 1);

        return nodes[x, y, z];
    }

    public Stack<Vector3> FindPath(Vector3 start, Vector3 end, PathfindingSettings settings, float isoLevel)
    {
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

        Stack<Vector3> path = new Stack<Vector3>();

        List<Node> openNodes = new List<Node>();
        List<Node> closedNodes = new List<Node>();

        Node current = startNode;
        openNodes.Add(startNode);

        while (openNodes.Count != 0 && !closedNodes.Contains(endNode))
        {
            current = openNodes[0];
            openNodes.RemoveAt(0);
            closedNodes.Add(current);

            foreach (var neighbour in current.neighbours)
            {
                if (neighbour.isoValue > isoLevel)
                {
                    if (!closedNodes.Contains(neighbour))
                    {
                        if (!openNodes.Contains(neighbour))
                        {
                            neighbour.previousPathNode = current;
                            if (settings.manhattanDistance)
                            {
                                neighbour.targetDistance = Mathf.Abs(neighbour.pos.x - endNode.pos.x) 
                                    + Mathf.Abs(neighbour.pos.y - endNode.pos.y) 
                                    + Mathf.Abs(neighbour.pos.z - endNode.pos.z);
                            }
                            else
                            {
                                neighbour.targetDistance = Vector3.Distance(neighbour.pos, endNode.pos);
                            }
                            neighbour.cost = neighbour.previousPathNode.cost + 1;
                            openNodes.Add(neighbour);
                            openNodes = openNodes.OrderBy(n => n.F).ToList();
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
            Debug.Log("Distance: " + distance + ", path length: " + pathLength + " (" + pathLength * 100 / distance + "%), time: " + sw.Elapsed.Milliseconds);
        }

        return path;

    }

}
