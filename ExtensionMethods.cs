// https://unity3d.com/learn/tutorials/topics/scripting/extension-methods
using UnityEngine;

public static class ExtensionMethods
{
    /// Returns a component of Type T by finding the existing one, or by instantiating one if not found.
    public static T EnsureComponent<T>(this GameObject go) where T : Component
    {
        return  Wrj.Utils.EnsureComponent<T>(go);
    }
    /// Returns a component of Type T by finding the existing one, or by instantiating one if not found.
    public static T EnsureComponent<T>(this Transform tform) where T : Component
    {
        return tform.gameObject.EnsureComponent<T>();
    }
    /// Returns a component of Type T by finding the existing one, or by instantiating one if not found.
    public static T EnsureComponent<T>(this Component comp) where T : Component
    {
        return comp.gameObject.EnsureComponent<T>();
    }
    
    /// Runs an operation on every child of the game object recursively.
    ///
    /// Argument is a method that takes a GameObject.
    public static void PerChild(this GameObject go, Wrj.Utils.GameObjectAffector goa)
    {
        Wrj.Utils.AffectGORecursively(go, goa, true);
    }

    /// Enable or disable GameObject in hierarchy.
    public static void ToggleActive(this GameObject go)
    {
        go.SetActive(!go.activeInHierarchy);
    }
    /// Enable or disable GameObject in hierarchy.
    public static void ToggleActive(this Transform tForm)
    {
        tForm.gameObject.SetActive(!tForm.gameObject.activeInHierarchy);
    }

    /// Returns a Vector3 using x, y and 0
    public static Vector3 ToVector3(this Vector2 v2)
    {
        return new Vector3(v2.x, v2.y, 0);
    }
    /// Returns a Vector2 using x and y of the Vector3
    public static Vector2 ToVector2(this Vector3 v3)
    {
        return new Vector2(v3.x, v3.y);
    }

    /// Returns conversion to Unity Units/Meters from Feet
    public static float FeetToUnits(this float feet)
    {
        return Wrj.Utils.FromFeet(feet);
    }
    /// Returns conversion to Feet from Unity Units/Meters
    public static float UnitsToFeet(this float units)
    {
        return Wrj.Utils.ToFeet(units);
    }
    /// Returns conversion to Inches from Unity Units/Meters
    public static float InchesToUnits(this float inches)
    {
        return Wrj.Utils.FromInches(inches);
    }
    /// Returns conversion to Unity Units/Meters from Inches
    public static float UnitsToInches(this float units)
    {
        return Wrj.Utils.ToInches(units);
    }
    
    // Shorthand transform manipulation extensions, using MapToCurve
    
    /// Move, rotate and scale the transform to the position of another over time
    ///
    /// Strongly recommended that the target transform shares the parent of this transform.
    public static void SnapToSibling(this Transform tForm, Transform to, float duration)
    {
        Wrj.Utils.MapToCurve.Linear.MatchSibling(tForm, to, duration, false, 0, 0, 0, false, null);
    }
    /// Move, rotate and scale the transform to the position of another over time
    ///
    /// Strongly recommended that the target transform shares the parent of this transform.
    public static void SnapToSibling(this GameObject go, Transform to, float duration)
    {
        go.transform.SnapToSibling(to, duration);
    }
    /// Move, rotate and scale the transform to the position of another over time
    ///
    /// Strongly recommended that the target transform shares the parent of this transform.
    public static void EaseSnapToSibling(this Transform tForm, Transform to, float duration)
    {
        Wrj.Utils.MapToCurve.Ease.MatchSibling(tForm, to, duration, false, 0, 0, 0, false, null);
    }
    /// Move, rotate and scale the transform to the position of another over time
    ///
    /// Strongly recommended that the target transform shares the parent of this transform.
    public static void EaseSnapToSibling(this GameObject go, Transform to, float duration)
    {
        go.transform.EaseSnapToSibling(to, duration);
    }

    /// Move transform in local space over time
    public static void Move(this Transform tForm, Vector3 to, float duration)
    {
        Wrj.Utils.MapToCurve.Linear.Move(tForm, to, duration, false, 0, 0, 0, false, false, null);
    }
    /// Move transform in local space over time
    public static void Move(this GameObject go, Vector3 to, float duration)
    {
        go.transform.Move(to, duration);
    }
    /// Move transform in local space over time
    public static void EaseMove(this Transform tForm, Vector3 to, float duration)
    {
        Wrj.Utils.MapToCurve.Ease.Move(tForm, to, duration, false, 0, 0, 0, false, false, null);
    }
    /// Move transform in local space over time
    public static void EaseMove(this GameObject go, Vector3 to, float duration)
    {
        go.transform.EaseMove(to, duration);
    }
    
    /// Rotate transform over time
    public static void LinearRotate(this Transform tForm, Vector3 to, float duration)
    {
        Wrj.Utils.MapToCurve.Linear.Rotate(tForm, to, duration, false, 0, 0, 0, false, true, false, null);
    }
    /// Rotate transform over time
    public static void Rotate(this GameObject go, Vector3 to, float duration)
    {
        go.transform.LinearRotate(to, duration);
    }
    /// Rotate transform over time
    public static void EaseRotate(this Transform tForm, Vector3 to, float duration)
    {
        Wrj.Utils.MapToCurve.Ease.Rotate(tForm, to, duration, false, 0, 0, 0, false, true, false, null);
    }
    /// Rotate transform over time
    public static void EaseRotate(this GameObject go, Vector3 to, float duration)
    {
        go.transform.EaseRotate(to, duration);
    }
    
    /// Change transforms scale over time
    public static void Scale(this Transform tForm, Vector3 to, float duration)
    {
        Wrj.Utils.MapToCurve.Linear.Scale(tForm, to, duration, false, 0, 0, 0, false, null);
    }
    /// Change transforms scale over time using a multiplier
    public static void Scale(this Transform tForm, float to, float duration)
    {
        Wrj.Utils.MapToCurve.Linear.Scale(tForm, tForm.localScale * to, duration, false, 0, 0, 0, false, null);
    }
    /// Change transforms scale over time
    public static void Scale(this GameObject go, Vector3 to, float duration)
    {
        go.transform.Scale(to, duration);
    }
    /// Change transforms scale over time using a multiplier
    public static void Scale(this GameObject go, float to, float duration)
    {
        go.transform.Scale(to, duration);
    }
    /// Change transforms scale over time
    public static void EaseScale(this Transform tForm, Vector3 to, float duration)
    {
        Wrj.Utils.MapToCurve.Ease.Scale(tForm, to, duration, false, 0, 0, 0, false, null);
    }
    /// Change transforms scale over time using a multiplier
    public static void EaseScale(this Transform tForm, float to, float duration)
    {
        Wrj.Utils.MapToCurve.Ease.Scale(tForm, tForm.localScale * to, duration, false, 0, 0, 0, false, null);
    }
    /// Change transforms scale over time
    public static void EaseScale(this GameObject go, Vector3 to, float duration)
    {
        go.transform.EaseScale(to, duration);
    }
    /// Change transforms scale over time using a multiplier
    public static void EaseScale(this GameObject go, float to, float duration)
    {
        go.transform.EaseScale(to, duration);
    }
    
    /// Change Color Over Time
    public static void Color(this Transform tForm, Color to, float duration)
    {
        Wrj.Utils.MapToCurve.Linear.ChangeColor(tForm, to, duration, false, 0, 0, 0, false, null);
    }
    /// Change Color Over Time
    public static void Color(this GameObject go, Color to, float duration)
    {
        go.transform.Color(to, duration);
    }

    /// Change transparency over time
    public static void Alpha(this Transform tForm, float to, float duration)
    {
        Wrj.Utils.MapToCurve.Linear.FadeAlpha(tForm, to, duration, false, 0, 0, 0, false, null);
    }
    /// Change transparency over time
    public static void Alpha(this GameObject go, float to, float duration)
    {
        go.transform.Alpha(to, duration);
    }

    /// Remap Float value from one range to another.
    public static float Remap(this float value, float sourceMin, float sourceMax, float destMin, float destMax )
    {
        return Wrj.Utils.Remap(value, sourceMin, sourceMax, destMin, destMax);
    }
    /// Remap Float value from one range to another.
    public static float Remap(this int value, float sourceMin, float sourceMax, float destMin, float destMax )
    {
        return Wrj.Utils.Remap(value, sourceMin, sourceMax, destMin, destMax);
    }
}
