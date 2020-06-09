using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Pathfinding.Editors
{
    /// <summary>
    /// Displays buttons to call commonly used functions like generating NavMesh in Editor
    /// </summary>
    [CustomEditor(typeof(NodesGenerator))]
    public class NodesGeneratorEditor : Editor
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
                if (GUILayout.Button("Clear outdated chunks"))
                {
                    generator.ClearOutdatedChunks();
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

            if (GUILayout.Button("March Cubes"))
            {
                generator.MarchCubes();
            }

            if (GUILayout.Button("Rebuild Graph"))
            {
                generator.GenerateGraph();
            }

            if (GUILayout.Button("Assign Neighbours"))
            {
                generator.AssignNeighbours();
            }

            if (!generator.start || !generator.goal)
            {
                GUI.enabled = false;
            }
            if (GUILayout.Button("Find Grid Path"))
            {
                generator.FindGridPath(generator.start.position, generator.goal.position, generator.pathfindingSettings);
            }

            if (!generator.hasGraph)
            {
                GUI.enabled = false;
            }
            if (GUILayout.Button("Find Graph Path"))
            {
                generator.FindGraphPath(generator.start.position, generator.goal.position, generator.pathfindingSettings);
            }


            GUI.enabled = true;

            if (GUILayout.Button("Clear"))
            {
                generator.Clear();
            }

            if (GUILayout.Button("Toggle NavMesh"))
            {
                generator.ToggleNavMesh();
            }
        }
    }
}