using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding.Serialization
{
    public class Flattened3DArray<T>
    {
        public int[] dimensions;
        public T[] items;

        public ref T this[int i] => ref items[i];

        public ref T this[int x, int y, int z] => ref Get(x, y, z);

        public Flattened3DArray()
        { }

        public Flattened3DArray(Flattened3DArray<T> other)
        {
            dimensions = other.dimensions;
            items = other.items;
        }

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
        public static Flattened3DArray<T> New<T>(int x, int y, int z)
        {
            var retVal = new Flattened3DArray<T>();
            retVal.dimensions = new int[] { x, y, z };
            retVal.items = new T[x * y * z];
            return retVal;
        }

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
