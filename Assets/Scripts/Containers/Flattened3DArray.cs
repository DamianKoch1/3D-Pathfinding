using System;

namespace Pathfinding.Containers
{
    /// <summary>
    /// Flat representation of a T[,,] array, can be accessed with T[i] and T[x, y, z]
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class Flattened3DArray<T>
    {
        public int[] dimensions;
        public T[] items;

        public ref T this[int i] => ref items[i];

        public ref T this[int x, int y, int z] => ref Get(x, y, z);

        public int Length => items.Length;

        public Flattened3DArray()
        { }

        public Flattened3DArray(Flattened3DArray<T> other)
        {
            dimensions = other.dimensions;
            items = other.items;
        }

        public void Clear()
        {
            dimensions = new int[0];
            items = new T[0];
        }

        /// <summary>
        /// Returns a T[,,] array from this arrays items
        /// </summary>
        /// <returns></returns>
        public T[,,] Unflatten()
        {
            int width = dimensions[0];
            int height = dimensions[1];
            int depth = dimensions[2];
            var retVal = new T[width, height, depth];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        retVal[x, y, z] = Get(x, y, z);
                    }
                }
            }

            return retVal;
        }

        public ref T Get(int x, int y, int z)
        {
            return ref items[x + dimensions[0] * (y + dimensions[1] * z)];
        }

        public void Set(T value, int x, int y, int z)
        {
            Get(x, y, z) = value;
        }
    }

    /// <summary>
    /// Avoids using constructors to reduce boilerplate code in dummy derived classes for serialization
    /// </summary>
    public static class FlattenedArrayUtils
    {
        /// <summary>
        /// Creates a flattened array with given dimensions
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="x">width</param>
        /// <param name="y">height</param>
        /// <param name="z">depth</param>
        /// <returns></returns>
        public static Flattened3DArray<T> New<T>(int x, int y, int z)
        {
            var retVal = new Flattened3DArray<T>();
            retVal.dimensions = new int[] { x, y, z };
            retVal.items = new T[x * y * z];
            return retVal;
        }

        /// <summary>
        /// Creates a flat representation of given 3D array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <returns></returns>
        public static Flattened3DArray<T> MakeFrom<T>(T[,,] array)
        {
            var retVal = new Flattened3DArray<T>();
            retVal.dimensions = new int[3];
            retVal.items = new T[array.Length];
            for (int i = 0; i < 3; i++)
            {
                retVal.dimensions[i] = array.GetLength(i);
            }
            int width = retVal.dimensions[0];
            int height = retVal.dimensions[1];
            int depth = retVal.dimensions[2];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        retVal.Set(array[x, y, z], x, y, z);
                    }
                }
            }
            return retVal;
        }

    }
}
