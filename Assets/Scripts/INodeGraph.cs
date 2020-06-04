using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding
{
    /// <summary>
    /// Interface to enable accepting both grid or graph in functions
    /// </summary>
    public interface INodeGraph
    {
        IEnumerable<Node> Nodes
        {
            get;
        }

        Node GetClosestNode(Vector3 position);

        void ResetNodes();

        void StoreCrossChunkNeighbours();
    }
}
