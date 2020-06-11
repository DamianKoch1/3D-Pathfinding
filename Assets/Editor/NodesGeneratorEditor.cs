using Pathfinding.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

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

            if (GUILayout.Button("Generate Chunks"))
            {
                generator.GenerateChunks();
            }

            if (generator.chunks == null) GUI.enabled = false;

            if (GUILayout.Button("Generate Nodes"))
            {
                generator.GenerateNodes();
            }

            if (GUILayout.Button("Assign Neighbours"))
            {
                generator.AssignNeighbours();
            }

            if (GUILayout.Button("March Cubes"))
            {
                generator.MarchCubes(10);
            }

            if (!generator.start || !generator.goal) GUI.enabled = false;

            if (GUILayout.Button("Find Grid Path"))
            {
                generator.FindGridPath(generator.start.position, generator.goal.position, generator.pathfindingSettings);
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

            if (generator.chunks != null)
            {
                if (generator.chunks.Length == 0) GUI.enabled = false;
                else if (generator.chunks[0].grid == null && generator.chunks[0].graph == null) GUI.enabled = false;

                if (GUILayout.Button("Save"))
                {
                    //var data = Resources.Load<GeneratorData>("Generator Data/" + SceneManager.GetActiveScene().name + "_" + generator.name);
                    //if (!data)
                    //{
                    //    data = CreateInstance<GeneratorData>();
                    //    AssetDatabase.CreateAsset(data, "Assets/Resources/Generator Data/" + SceneManager.GetActiveScene().name + "_" + generator.name + ".asset");
                    //}
                    //generator.SerializeInto(data);
                    //EditorUtility.SetDirty(data);
                    //AssetDatabase.SaveAssets();
                    //Resources.UnloadAsset(data);

                    //generator.SerializeBinary();

                    generator.Serialize();
                }

                GUI.enabled = true;

                if (GUILayout.Button("Load"))
                {
                    //var data = Resources.Load<GeneratorData>("Generator Data/" + SceneManager.GetActiveScene().name + "_" + generator.name);
                    //if (data)
                    //{
                    //    generator.DeserializeFrom(data);
                    //    Resources.UnloadAsset(data);
                    //}

                    //generator.DeserializeBinary();

                    generator.Deserialize();
                }

                GUI.enabled = true;
            }
        }
    }
}