using UnityEngine;
using UnityEditor;

namespace Pathfinding.Editors
{
    /// <summary>
    /// Dummy custom editor to allow ScriptableObjectDrawer to get a valid editor for any edited type
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Object), true)]
    public class UnityObjectEditor : Editor
    {
    }
}