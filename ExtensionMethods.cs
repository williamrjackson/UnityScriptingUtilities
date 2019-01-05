// https://unity3d.com/learn/tutorials/topics/scripting/extension-methods
using UnityEngine;

public static class ExtensionMethods
{
    // Returns a component of Type T by finding the existing one, or by instantiating one if not found.
    public static T EnsureComponent<T>(this GameObject go) where T : Component
    {
        return  Wrj.Utils.EnsureComponent<T>(go);
    }
    public static T EnsureComponent<T>(this Transform tform) where T : Component
    {
        return tform.gameObject.EnsureComponent<T>();
    }
    public static T EnsureComponent<T>(this Component comp) where T : Component
    {
        return comp.gameObject.EnsureComponent<T>();
    }

    // Enable or disable GameObject in hierarchy.
    public static void ToggleActive(this GameObject go)
    {
        go.SetActive(!go.activeInHierarchy);
    }
    public static void ToggleActive(this Transform tForm)
    {
        tForm.gameObject.SetActive(!tForm.gameObject.activeInHierarchy);
    }

    // Shorthand transform manipulation extensions, using MapToCurve
    // Move over time
    public static void Move(this Transform tForm, Vector3 to, float duration)
    {
        Wrj.Utils.MapToCurve.Linear.Move(tForm, to, duration, false, 0, 0, 0, false, null);
    }
    public static void Move(this GameObject go, Vector3 to, float duration)
    {
        go.transform.Move(to, duration);
    }
    public static void EaseMove(this Transform tForm, Vector3 to, float duration)
    {
        Wrj.Utils.MapToCurve.Ease.Move(tForm, to, duration, false, 0, 0, 0, false, null);
    }
    public static void EaseMove(this GameObject go, Vector3 to, float duration)
    {
        go.transform.EaseMove(to, duration);
    }
    
    // Rotate over time
    public static void Rotate(this Transform tForm, Vector3 to, float duration)
    {
        Wrj.Utils.MapToCurve.Linear.Rotate(tForm, to, duration, false, 0, 0, 0, false, true, null);
    }
    public static void Rotate(this GameObject go, Vector3 to, float duration)
    {
        go.transform.EaseRotate(to, duration);
    }
    public static void EaseRotate(this Transform tForm, Vector3 to, float duration)
    {
        Wrj.Utils.MapToCurve.Ease.Rotate(tForm, to, duration, false, 0, 0, 0, false, true, null);
    }
    public static void EaseRotate(this GameObject go, Vector3 to, float duration)
    {
        go.transform.EaseRotate(to, duration);
    }
    
    // Change scale over time
    public static void Scale(this Transform tForm, Vector3 to, float duration)
    {
        Wrj.Utils.MapToCurve.Linear.Scale(tForm, to, duration, false, 0, 0, 0, false, null);
    }
    public static void Scale(this Transform tForm, float to, float duration)
    {
        Wrj.Utils.MapToCurve.Linear.Scale(tForm, Vector3.one * to, duration, false, 0, 0, 0, false, null);
    }
    public static void Scale(this GameObject go, Vector3 to, float duration)
    {
        go.transform.Scale(to, duration);
    }
    public static void Scale(this GameObject go, float to, float duration)
    {
        go.transform.Scale(to, duration);
    }
    public static void EaseScale(this Transform tForm, Vector3 to, float duration)
    {
        Wrj.Utils.MapToCurve.Ease.Scale(tForm, to, duration, false, 0, 0, 0, false, null);
    }
    public static void EaseScale(this Transform tForm, float to, float duration)
    {
        Wrj.Utils.MapToCurve.Ease.Scale(tForm, Vector3.one * to, duration, false, 0, 0, 0, false, null);
    }
    public static void EaseScale(this GameObject go, Vector3 to, float duration)
    {
        go.transform.EaseScale(to, duration);
    }
    public static void EaseScale(this GameObject go, float to, float duration)
    {
        go.transform.EaseScale(to, duration);
    }
    
    // Change Color Over Time
    public static void Color(this Transform tForm, Color to, float duration)
    {
        Wrj.Utils.MapToCurve.Linear.ChangeColor(tForm, to, duration, false, 0, 0, 0, false, null);
    }
    public static void Color(this GameObject go, Color to, float duration)
    {
        go.transform.Color(to, duration);
    }

    public static void Alpha(this Transform tForm, float to, float duration)
    {
        Wrj.Utils.MapToCurve.Linear.FadeAlpha(tForm, to, duration, false, 0, 0, 0, false, null);
    }
    public static void Alpha(this GameObject go, float to, float duration)
    {
        go.transform.Alpha(to, duration);
    }

    // Remap Float or int.
    public static float Remap(this float value, float sourceMin, float sourceMax, float destMin, float destMax )
    {
        return Wrj.Utils.Remap(value, sourceMin, sourceMax, destMin, destMax);
    }
    public static float Remap(this int value, float sourceMin, float sourceMax, float destMin, float destMax )
    {
        return Wrj.Utils.Remap(value, sourceMin, sourceMax, destMin, destMax);
    }
}
