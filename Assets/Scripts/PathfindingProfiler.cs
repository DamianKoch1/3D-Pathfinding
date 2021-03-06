﻿using UnityEngine;

namespace Pathfinding
{
    /// <summary>
    /// Runs desired algorithm n times after target generator is initialized, stops once no path has been found to prevent crashing from too many opened nodes
    /// </summary>
    public class PathfindingProfiler : MonoBehaviour
    {
        [SerializeField]
        private NodesGenerator generator;

        [SerializeField]
        private Transform target;

        [SerializeField]
        private PathfindingMode mode;

        [SerializeField]
        private PathfindingSettings settings;

        [SerializeField]
        private int iterations;

        void Awake()
        {
            generator.OnInitialize += Profile;
        }
        
        [ContextMenu("Profile")]
        private void Profile()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            int i = 0;
            switch (mode)
            {
                case PathfindingMode.grid:
                    for (i = 0; i < iterations; i++)
                    {
                        if (generator.FindGridPath(transform.position, target.position, settings).Count == 0) break;
                    }
                    break;
                case PathfindingMode.navmesh:
                    for (i = 0; i < iterations; i++)
                    {
                        if (generator.FindGraphPath(transform.position, target.position, settings).Count == 0) break;
                    }
                    break;
            }
            sw.Stop();
            print(i + " " + settings.algorithm + " took " + sw.Elapsed);
        }

    }
}
