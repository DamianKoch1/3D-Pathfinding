using UnityEngine;
using static UnityEngine.Mathf;

namespace Pathfinding.Algorithms
{
    /// <summary>
    /// Different heuristics functions to estimate minimum path length from a to b
    /// </summary>
    public static class Heuristics
    {
        public static readonly float Sqrt2 = Sqrt(2);

        public static readonly float Sqrt3 = Sqrt(3);

        /// <summary>
        /// Line distance from a to b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static float Euclidean(Vector3 a, Vector3 b)
        {
            return Vector3.Distance(a, b);
        }

        /// <summary>
        /// Sum of coordinate deltas of a and b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static float Manhattan(Vector3 a, Vector3 b)
        {
            return Abs(a.x - b.x) + Abs(a.y - b.y) + Abs(a.z - b.z);
        }

        /// <summary>
        /// Largest coordinate delta of a and b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static float Chebyshev(Vector3 a, Vector3 b)
        {
            return Max(Abs(a.x - b.x), Abs(a.y - b.y), Abs(a.z - b.z));
        }

        /// <summary>
        /// Minimum total length of steps from a to b on a 3D grid allowing diagonal movement
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static float Octile(Vector3 a, Vector3 b)
        {
            var dx = Abs(a.x - b.x);
            var dy = Abs(a.y - b.y);
            var dz = Abs(a.z - b.z);

            float min1 = Min(dx, dy, dz);
            float min2 = Max(Min(dx, dy), Min(Max(dx, dy), dz));

            return dx + dy + dz - min1 * (3 - Sqrt3) - min2 * (2 - Sqrt2);
        }
    }
}
