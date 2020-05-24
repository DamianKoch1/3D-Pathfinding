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

        var generator = (NodesGenerator)target;

        if (generator.obstacleSettings)
        {
            if (GUILayout.Button("Randomize Obstacles"))
            {
                generator.RandomizeObstacles();
            }
        }

        if (GUILayout.Button("Generate Chunks"))
        {
            generator.GenerateChunks();
        }

        if (generator.chunks == null)
        {
            GUI.enabled = false;
        }

        if (generator.hasOutOfRangeChunks)
        {
            if (GUILayout.Button("Clear out of range chunks"))
            {
                generator.ClearOutOfRangeChunks();
            }
        }


        if (GUILayout.Button("Rebuild Grid"))
        {
            generator.GenerateGrid();
        }

        if (!generator.hasGrid)
        {
            GUI.enabled = false;
        }

        if (GUILayout.Button("Expand Meshes"))
        {
            generator.ExpandMeshes();
        }

        if (GUILayout.Button("March Cubes"))
        {
            generator.MarchCubes();
        }

        if (!generator.start || !generator.end)
        {
            GUI.enabled = false;
        }
        if (GUILayout.Button("Find Path"))
        {
            generator.FindPath();
        }
        GUI.enabled = true;

        if (GUILayout.Button("Clear"))
        {
            generator.Clear();
        }
    }
}