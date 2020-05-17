using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class GridGenerationSettings : ScriptableObject
{
    public bool drawNodes;

    public Color color;

    public Vector3 size;

    [Range(0, 5)]
    public float navMeshOffset;

    public Vector3 step;
}
