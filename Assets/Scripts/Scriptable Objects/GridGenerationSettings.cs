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

    [Range(0, 5)]
    public float navMeshOffset;

    public Vector3 step;

    public Mode mode;

    [Header("Noise mode")]
    [Range(0.001f, 0.03f)]
    public float scale;

    [Range(0, 255)]
    public float threshold;

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
    Physics,
    Noise
}
