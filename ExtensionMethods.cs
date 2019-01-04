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
        public static void EaseMove(this GameObject go, Vector3 to, float duration)
        {
            Utils.MapToCurve.Ease.Move(go.transform, to, duration, false, 0, 0, 0, false, null);
        }
        public static void EaseRotate(this GameObject go, Vector3 to, float duration)
        {
            Utils.MapToCurve.Ease.Rotate(go.transform, to, duration, false, 0, 0, 0, false, true, null);
        }
        public static void EaseScale(this GameObject go, Vector3 to, float duration)
        {
            Utils.MapToCurve.Ease.Scale(go.transform, to, duration, false, 0, 0, 0, false, null);
        }
        public static void EaseColor(this GameObject go, Color to, float duration)
        {
            Utils.MapToCurve.Ease.ChangeColor(go.transform, to, duration, false, 0, 0, 0, false, null);
        }
        public static void EaseAlpha(this GameObject go, float to, float duration)
        {
            Utils.MapToCurve.Ease.FadeAlpha(go.transform, to, duration, false, 0, 0, 0, false, null);
        }
    }
}