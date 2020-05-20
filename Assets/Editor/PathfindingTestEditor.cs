using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PathfindingTest))]
public class PathfindingTestEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Build Graph"))
        {
            ((PathfindingTest)target).BuildGraph();
        }

        if (GUILayout.Button("Find Path"))
        {
            ((PathfindingTest)target).FindPath();
        }

        if (GUILayout.Button("Clear"))
        {
            ((PathfindingTest)target).Clear();
        }
    }
}
