using UnityEngine;

namespace Pathfinding.Algorithms
{
    /// <summary>
    /// Different heuristics functions to estimate minimum distance from a to b
    /// </summary>
    public static class Heuristics
    {
        public static readonly float Sqrt2 = Mathf.Sqrt(2);

        public static readonly float Sqrt3 = Mathf.Sqrt(3);

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
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z);
        }

        /// <summary>
        /// Largest coordinate delta of a and b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static float Chebyshev(Vector3 a, Vector3 b)
        {
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y), Mathf.Abs(a.z - b.z));
        }

        /// <summary>
        /// Minimum total length of steps from a to b on a 3D grid allowing diagonal movement
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static float Octile(Vector3 a, Vector3 b)
        {
            var dx = Mathf.Abs(a.x - b.x);
            var dy = Mathf.Abs(a.y - b.y);
            var dz = Mathf.Abs(a.z - b.z);

            float min1 = Mathf.Min(dx, dy, dz);
            float min2 = dx;
            if (min1 == dx)
            {
                min2 = Mathf.Min(dy, dz);
            }
            else if (min1 == dy)
            {
                min2 = Mathf.Min(dx, dz);
            }
            else if (min1 == dz)
            {
                min2 = Mathf.Min(dx, dy);
            }

            return dx + dy + dz - min1 * (3 - Sqrt3) - min2 * (2 - Sqrt2);
        }
    }
}
