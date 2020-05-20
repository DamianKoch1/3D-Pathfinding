using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NodesGenerator))]
public class OctreeGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Randomize Obstacles"))
        {
            ((NodesGenerator)target).RandomizeObstacles();
        }

        if (GUILayout.Button("Rebuild Grid"))
        {
            ((NodesGenerator)target).GenerateGrid();
        }

        if (GUILayout.Button("Expand Meshes"))
        {
            ((NodesGenerator)target).ExpandMeshes();
        }

        if (GUILayout.Button("March Cubes"))
        {
            ((NodesGenerator)target).MarchCubes();
        }

        if (GUILayout.Button("Find Path"))
        {
            ((NodesGenerator)target).FindPath(10);
        }

        if (GUILayout.Button("Clear"))
        {
            ((NodesGenerator)target).Clear();
        }
    }
}