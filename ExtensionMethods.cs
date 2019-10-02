// https://unity3d.com/learn/tutorials/topics/scripting/extension-methods
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    /// Returns a Vector3 with an axis forced as specified
    public static Vector3 With(this Vector3 orig, float? x = null, float? y = null, float? z = null)
    {
        return new Vector3(x ?? orig.x, y ?? orig.y, z ?? orig.z);
    }

    /// Returns the transforms position as moved in a direction relative to itself
    public static Vector3 PosInDir(this Transform tform, float forward = 0f, float right = 0f, float up = 0f)
    {
        return tform.position + tform.forward * forward + tform.right * right + tform.up * up;
    }
    
    /// Returns the transforms position as moved in a direction relative to the world
    public static Vector3 PosInWorldDir(this Transform tform, float forward = 0f, float right = 0f, float up = 0f)
    {
        return tform.position + Vector3.forward * forward + Vector3.right * right + Vector3.up * up;
    }

    /// Returns the transforms local position as moved in a direction relative to itself
    public static Vector3 LocalPosInDir(this Transform tform, float forward = 0f, float right = 0f, float up = 0f)
    {
        return tform.localPosition + tform.forward * forward + tform.right * right + tform.up * up;
    }

    /// Returns the transforms local position as moved in a direction relative to the world
    public static Vector3 LocalPosInWorldDir(this Transform tform, float forward = 0f, float right = 0f, float up = 0f)
    {
        return tform.localPosition + Vector3.forward * forward + Vector3.right * right + Vector3.up * up;
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

    

    /// Returns a random element of a list.
    public static T GetRandom<T>(this System.Collections.Generic.List<T> list)
    {
        if (list.Count > 0)
        {
            return list[Random.Range(0, list.Count)];
        }
        return default(T);
    }
    /// Returns a random element of an array.
    public static T GetRandom<T>(this T[] ar)
    {
        if (ar.Length > 0)
        {
            return ar[Random.Range(0, ar.Length)];
        }
        return default(T);
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
    
    /// <summary>
    /// Prepends either "A" or "An" to a word depending on whether the first character is a vowel sound.
    /// </summary>
    /// <param name="capitalize"></param>
    /// <returns>"An owl" or "A bear"</returns>
    public static string PrependAn(this string word, bool capitalize = false)
    {
        string an = "";
        bool isVowel = "aeiouAEIOU".IndexOf(word[0]) >= 0;
        if (HasRulebreakingIndefiniteArticle(word))
        {
            isVowel = !isVowel;
        }

        if (isVowel)
        {
            an = Capitalize("an", capitalize);
        }
        else
        {
            an = Capitalize("a", capitalize);
        }

        return an + " " + word;
    }

    private static bool HasRulebreakingIndefiniteArticle(string word)
    {
        string list = " a f h l m n r s u x 8 11 18 80 81 82 83 84 85 86 87 88 89 honest honesty hour heir honour honourable honor honorable herb use union university unit user unity universe uniform usage utility urine uranium unison euphoria utopia unanimity uterus euthanasia ewe ufo unicorn urea urethra euphemism eugenics usurper usability eunuch uni eucalyptus usury eulogy ubiquity universalism urinal universal ewer euro utensil ufology uniformitarianism upsilon ukulele urinalysis usurer ureter uridine ute eugenist eutectic eukaryote ufologist ululation usufruct eustasy unary uvula urus eucatastrophe uraeus ouabain one using ucalegon oncer usanian usufruction eusebius usar usufructuary amazigh usuress euouae ukase euclidianness uke uke uke ukie ureteroureterostomy usurping eustress unakas eudaemon ukrainian unidirectionality utahn unite uranism uranist eudemonia euth ute uranophobia euphoriant uvular ouija uropygium eugarie eugenesis uw iatmul eutripsia uey eugeny euglena ufo unigeniture univalence univalent utile utilitarian ubac eulachon unique usonian oaxaca uniquity eureka onesie universalism uberty uni ubication utonian ubicity euboean uniate euro utopographer esclop euro-american eumenides eucharist univocal euchologion euchre eunoia unix ";
        return list.IndexOf(" " + word.ToLower()+ " ") >= 0;
    }

    private static string Capitalize(string word, bool capitalize)
    {
        if (!capitalize)
            return char.ToLower(word[0]) + word.Substring(1);

        return char.ToUpper(word[0]) + word.Substring(1);
    }

    /// <summary>
    /// Appends "s" or "es" based on common rules.
    /// </summary>
    /// <returns>"Puma" -> "Pumas" or "Fox" -> "Foxes"</returns>
    public static string Pluralize(this string word, bool capitalize = false)
    {
        string checkedWord = CheckForIrregularPlural(word);
        if (checkedWord != null)
        {
            return Capitalize(checkedWord, capitalize);
        }
        char[] chars = word.ToCharArray();

        char lastLetter = chars[chars.Length - 1];
        char secondToLastLetter = chars[chars.Length - 2];

        if ((lastLetter == 's' || lastLetter == 'z' || lastLetter == 'x') ||
            (lastLetter == 'h' && (secondToLastLetter == 'c' || secondToLastLetter == 's')))
        {
            return word + "es";
        }
        if (lastLetter == 'y' && "aeiouAEIOU".IndexOf(secondToLastLetter) < 0)
        {
            return word.TrimEnd('y') + "ies";
        }

        return Capitalize(word + "s", capitalize);
    }

    private static string CheckForIrregularPlural(string word)
    {
        Dictionary<string, string> pluralMap = new Dictionary<string, string>()
        {
            {"addendum", "addenda"}, {"aircraft", "aircraft"}, {"alumna", "alumnae"}, {"alumnus", "alumni"}, {"analysis", "analyses"}, {"antenna", "antennae"}, {"antithesis", "antitheses"}, {"apex", "apices"}, {"appendix", "appendices"}, {"axis", "axes"}, {"bacillus", "bacilli"}, {"bacterium", "bacteria"}, {"basis", "bases"}, {"beau", "beaux"}, {"bison", "bison"}, {"bureau", "bureaux"}, {"cactus", "cacti"}, {"ch�teau", "ch�teaux"}, {"child", "children"}, {"codex", "codices"}, {"concerto", "concerti"}, {"corpus", "corpora"}, {"crisis", "crises"}, {"criterion", "criteria"}, {"curriculum", "curricula"}, {"datum", "data"}, {"deer", "deer"}, {"diagnosis", "diagnoses"}, {"die", "dice"}, {"dwarf", "dwarves"}, {"ellipsis", "ellipses"}, {"erratum", "errata"}, {"fez", "fezzes"}, {"fish", "fish"}, {"focus", "foci"}, {"foot", "feet"}, {"formula", "formulae"}, {"fungus", "fungi"}, {"genus", "genera"}, {"goose", "geese"}, {"graffito", "graffiti"}, {"grouse", "grouse"}, {"half", "halves"}, {"hoof", "hooves"}, {"hypothesis", "hypotheses"}, {"index", "indices"}, {"larva", "larvae"}, {"libretto", "libretti"}, {"loaf", "loaves"}, {"locus", "loci"}, {"louse", "lice"}, {"man", "men"}, {"matrix", "matrices"}, {"medium", "media"}, {"memorandum", "memoranda"}, {"minutia", "minutiae"}, {"moose", "moose"}, {"mouse", "mice"}, {"nebula", "nebulae"}, {"nucleus", "nuclei"}, {"oasis", "oases"}, {"offspring", "offspring"}, {"opus", "opera"}, {"ovum", "ova"}, {"ox", "oxen"}, {"parenthesis", "parentheses"}, {"phenomenon", "phenomena"}, {"phylum", "phyla"}, {"prognosis", "prognoses"}, {"quiz", "quizzes"}, {"radius", "radii"}, {"referendum", "referenda"}, {"salmon", "salmon"}, {"scarf", "scarves"}, {"self", "selves"}, {"series", "series"}, {"sheep", "sheep"}, {"shrimp", "shrimp"}, {"species", "species"}, {"stimulus", "stimuli"}, {"stratum", "strata"}, {"swine", "swine"}, {"syllabus", "syllabi"}, {"symposium", "symposia"}, {"synopsis", "synopses"}, {"tableau", "tableaux"}, {"thesis", "theses"}, {"thief", "thieves"}, {"tooth", "teeth"}, {"trout", "trout"}, {"tuna", "tuna"}, {"vertebra", "vertebrae"}, {"vertex", "vertices"}, {"vita", "vitae"}, {"vortex", "vortice"}, {"wife", "wives"}, {"wolf", "wolves"}, {"woman", "women"},
        };
        foreach (KeyValuePair<string, string> pair in pluralMap)
        {
            if (pair.Key == word.ToLower())
            {
                return pair.Value;
            }
        }
        return null;
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
        Wrj.Utils.MapToCurve.Linear.MoveWorld(tForm, to, duration, false, 0, 0, 0, false, false, null);
    }
    /// Move transform in local space over time
    public static void Move(this GameObject go, Vector3 to, float duration)
    {
        go.transform.Move(to, duration);
    }
    /// Move transform in local space over time
    public static void EaseMove(this Transform tForm, Vector3 to, float duration)
    {
        Wrj.Utils.MapToCurve.Ease.MoveWorld(tForm, to, duration, false, 0, 0, 0, false, false, null);
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
