// https://unity3d.com/learn/tutorials/topics/scripting/extension-methods
using UnityEngine;
namespace Wrj
{
    public static class ExtensionMethods
    {
        // Returns a component of Type T by finding the existing one, or by instantiating one if not found.
        public static T EnsureComponent<T>(this GameObject go) where T : Component
        {
            if (!go.GetComponent<T>())
            {
                go.AddComponent<T>();
            }
            return go.GetComponent<T>();
        }

        // Enable or disable GameObject in hierarchy.
        public static void ToggleActive(this GameObject go)
        {
            go.SetActive(!go.activeInHierarchy);
        }

        // Shorthand transform manipulation extensions.
        public static void EaseMove(this Transform tform, Vector3 to, float duration)
        {
            Utils.MapToCurve.Ease.Move(tform, to, duration, false, 0, 0, 0, false, null);
        }
        public static void EaseRotate(this Transform tform, Vector3 to, float duration)
        {
            Utils.MapToCurve.Ease.Rotate(tform, to, duration, false, 0, 0, 0, false, true, null);
        }
        public static void EaseScale(this Transform tform, Vector3 to, float duration)
        {
            Utils.MapToCurve.Ease.Scale(tform, to, duration, false, 0, 0, 0, false, null);
        }
    }
}