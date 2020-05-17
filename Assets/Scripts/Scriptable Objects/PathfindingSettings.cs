﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class PathfindingSettings : ScriptableObject
{
    public bool allowDiagonal;

    [Tooltip("If true, uses sum of coordinate distances instead of point distance")]
    public bool manhattanDistance;

    public bool benchmark;
}
