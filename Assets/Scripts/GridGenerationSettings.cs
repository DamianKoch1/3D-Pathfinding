using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class GridGenerationSettings : ScriptableObject
{
    public bool drawNodes;

    public Color color;

    public Vector3 size;

    [Range(0, 1), Tooltip("Node is walkable if it has iso value above this")]
    public float isoLevel;

    public bool allowDiagonalNeighbours;

    public Vector3 step;

    public Vector3 chunkSize;

    public Vector3Int chunkCount;

    public Mode mode;

    [Header("Overlap settings")]
    [Range(0, 5)]
    public float navMeshOffset;

    [Header("Noise settings")]
    [Range(0.001f, 0.04f)]
    public float scale;

    public int seed;

    public bool useRandomSeed;

    public Action onValidate;

    public void OnValidate()
    {
        onValidate?.Invoke();
    }
}

public enum Mode
{
    Overlap,
    Noise
}
