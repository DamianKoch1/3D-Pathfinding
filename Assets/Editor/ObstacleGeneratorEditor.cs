using Pathfinding;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Pathfinding.Editors
{
    [CustomEditor(typeof(ObstacleGenerator))]
    public class ObstacleGeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var generator = (ObstacleGenerator)target;

            if (generator.obstacleSettings)
            {
                if (GUILayout.Button("Randomize Obstacles"))
                {
                    generator.RandomizeObstacles();
                }

                if (GUILayout.Button("Clear"))
                {
                    generator.Clear();
                }
            }
        }

    }
}
