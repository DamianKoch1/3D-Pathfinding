using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
