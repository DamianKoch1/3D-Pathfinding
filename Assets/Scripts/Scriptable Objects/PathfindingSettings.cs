using UnityEngine;
using Pathfinding.Algorithms;
using System.Collections.Generic;
using Pathfinding.Containers;

namespace Pathfinding
{
    [CreateAssetMenu]
    public class PathfindingSettings : ScriptableObject
    {
        [Tooltip("AStar: Combines cost with estimated goal distance to find optimal path \nThetaStar: Similar to AStar, but uses LineOfSight checks to skip some nodes")]
        public Algorithm algorithm;

        [Tooltip("Euclidean: line distance \nManhattan: total coordinate delta \nChebyshev: highest coordinate delta \nOctile: minimum grid path length allowing diagonal movement")]
        public HeuristicsFunction heuristic;

        public HeuristicsFunction costIncrease;


        public bool benchmark;


        [Range(0, 1), Tooltip("Higher value means greedier depth-first search, lower means breadth-first, CHANGE IN SMALL STEPS, usually doesnt't need to go below 0.7f")]
        public float greediness = 0.5f;

        /// <summary>
        /// Estimates path cost between a and b using selected heuristics function
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
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

        public float CostIncrease(Vector3 a, Vector3 b)
        {
            switch (costIncrease)
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

        public Stack<Vector3> RunAlgorithm(Node start, Node goal, float isoLevel, out BucketList<Node> openNodes, out HashSet<Node> closedNodes, int maxIterations = 50000, int nodeCount = 3000)
        {
            switch (algorithm)
            {
                case Algorithm.aStar:
                    return AStar.FindPath(start, goal, this, isoLevel, out openNodes, out closedNodes, maxIterations, nodeCount);
                case Algorithm.thetaStar:
                    return ThetaStar.FindPath(start, goal, this, isoLevel, out openNodes, out closedNodes, maxIterations, nodeCount);
            }
            openNodes = null;
            closedNodes = null;
            return null;
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

        public enum Algorithm
        {
            aStar,
            thetaStar,
        }
    }
}