using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class PathfindingSettings : ScriptableObject
{

    [Tooltip("Euclidean: line distance \nManhattan: total coordinate delta \nChebyshev: highest coordinate delta \nOctile: minimum grid path length allowing diagonal movement")]
    public HeuristicsFunction heuristic;

    public CostIncreaseFunction costIncrease;

    public bool benchmark;


    [Range(0, 1), Tooltip("Higher value means greedier depth-first search, lower means breadth-first")]
    public float greediness = 0.5f;

    public float Heuristic(Vector3 a, Vector3 b)
    {
        switch (heuristic)
        {
            case HeuristicsFunction.euclidean:
                return Heuristics.Euclidean(a, b);
            case HeuristicsFunction.manhattan:
                return Heuristics.Manhattan(a, b);
            case HeuristicsFunction.chebyshev:
                return Heuristics.Chebyshev(a, b);
            case HeuristicsFunction.octile:
                return Heuristics.Octile(a, b);
        }
        return 0;
    }

    public float CostIncrease(Node node)
    {
        switch (costIncrease)
        {
            case CostIncreaseFunction.one:
                return 1;
            case CostIncreaseFunction.euclidean:
                return Heuristics.Euclidean(node.pos, node.previousPathNode.pos);
            case CostIncreaseFunction.manhattan:
                return Heuristics.Manhattan(node.pos, node.previousPathNode.pos);
        }

        return 0;
    }
}

public enum HeuristicsFunction
{
    euclidean,
    manhattan,
    chebyshev,
    octile
}

public enum CostIncreaseFunction
{
    one,
    euclidean,
    manhattan
}
