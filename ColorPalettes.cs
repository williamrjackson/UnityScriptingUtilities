using System.Collections.Generic;
using UnityEngine;
namespace Wrj
{
    public class ColorHarmony
    {
        /// <summary>
        /// Get the color directly opposite the input on the color wheel
        /// </summary>
        /// <param name="color"></param>
        /// <returns>Complementary Color</returns>
        public static Color[] Complementary(Color color)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            h = (h + .5f) % 1f;
            Color[] results = new Color[2];
            results[0] = color;
            results[1] = Color.HSVToRGB(h, s, v);
            return results;
        }
        /// <summary>
        /// Get the two colors on each side of the provided color's complement
        /// </summary>
        /// <param name="color"></param>
        /// <returns><para>[0]=Color1</para><para>[1]=Original</para><para>[2]=Color2</para></returns>
        public static Color[] SplitComplementary(Color color)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            Color[] results = new Color[3];
            results[0] = Color.HSVToRGB((h + .5833333f) % 1f, s, v);
            results[1] = color;
            results[2] = Color.HSVToRGB((h + .4166667f) % 1f, s, v);
            return results;
        }
        /// <summary>
        /// Get 2 colors equally spaced from each other on the color wheel 
        /// </summary>
        /// <param name="color"></param>
        /// <returns><para>[0]=Color1</para><para>[1]=Original</para><para>[2]=Color2</para></returns>
        public static Color[] Triadic(Color color)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            Color[] results = new Color[3];
            results[0] = Color.HSVToRGB((h + .333f) % 1f, s, v);
            results[1] = color;
            results[2] = Color.HSVToRGB((h + .666f) % 1f, s, v);
            return results;
        }
        /// <summary>
        /// Get a combination of four colors on the wheel that are two sets of complements, based on input color
        /// </summary>
        /// <param name="color"></param>
        /// <returns><para>[0]=Original</para><para>[1]=Mod one step above original</para><para>[2]=Original's compliment</para><para>[2]=Mod's compliment</para></returns>
        public static Color[] Tetradic(Color color)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            Color comp = Complementary(color)[1];
            Color.RGBToHSV(comp, out float hC, out float sC, out float vC);
            Color[] results = new Color[4];
            results[0] = color;
            results[1] = Color.HSVToRGB((h + .097f) % 1f, s, v);
            results[2] = comp;
            results[3] = Color.HSVToRGB((hC + .097f) % 1f, sC, vC);
            return results;
        }
        /// <summary>
        /// Get a lighter and darker shade of the same color
        /// </summary>
        /// <param name="color"></param>
        /// <returns><para>[0]=Lighter</para><para>[1]=Original</para><para>[2]=Darker</para></returns>
        public static Color[] Monochromatic(Color color)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            Color[] results = new Color[3];
            results[0] = Color.HSVToRGB(h, Mathf.Clamp01(s - .1f), Mathf.Clamp01(v - .2f));
            results[1] = color;
            results[2] = Color.HSVToRGB(h, Mathf.Clamp01(s + .1f), Mathf.Clamp01(v + .2f));
            return results;
        }
        /// <summary>
        /// Get two colors adjacent to the input on the color wheel.
        /// </summary>
        /// <param name="color"></param>
        /// <returns><para>[0]=Left hue</para><para>[1]=Original</para><para>[2]=Right hue</para></returns>
        public static Color[] Analogous(Color color)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            Color[] results = new Color[3];
            results[0] = Color.HSVToRGB((h + .08f) % 1f, s, v);
            results[1] = color;
            results[2] = Color.HSVToRGB((h - .08f) % 1f, s, v);
            return results;
        }
    }
    [System.Serializable]
    public class ExtendedGradient
    {
        private List<Gradient> _gradients;
        private GradientColorKey[] _colorKeys = new GradientColorKey[2];
        private GradientAlphaKey[] _alphaKeys = new GradientAlphaKey[2];
        private GradientMode _gradientMode = GradientMode.Blend;

        public ExtendedGradient()
        {
            _colorKeys = new GradientColorKey[2];
            _alphaKeys = new GradientAlphaKey[2];
            Distribute();
        }
        public ExtendedGradient(GradientColorKey[] colorKeys, GradientAlphaKey[] alphaKeys)
        {
            _colorKeys = colorKeys;
            _alphaKeys = alphaKeys;
            Distribute();
        }
        public ExtendedGradient(GradientColorKey[] colorKeys)
        {
            _colorKeys = colorKeys;
            _alphaKeys = new GradientAlphaKey[2];
            Distribute();
        }
        public void SetKeys(GradientColorKey[] colorKeys, GradientAlphaKey[] alphaKeys)
        {
            _colorKeys = colorKeys;
            _alphaKeys = alphaKeys;
            Distribute();
        }
        public void SetKeys(GradientColorKey[] colorKeys)
        {
            _colorKeys = colorKeys;
            _alphaKeys = new GradientAlphaKey[2];
            Distribute();
        }
        public GradientColorKey[] ColorKeys
        {
            set
            {
                _colorKeys = value;
                Distribute();
            }
            get => _colorKeys;
        }
        public GradientAlphaKey[] AlphaKeys
        {
            set
            {
                _alphaKeys = value;
                Distribute();
            }
            get => _alphaKeys;
        }
        public GradientMode mode
        {
            set
            {
                _gradientMode = value;
                foreach (var gradient in _gradients)
                {
                    gradient.mode = _gradientMode;
                }
            }
        }
        private void Distribute()
        {
            _gradients = new List<Gradient>();
            if (_colorKeys.Length == 0 || _alphaKeys.Length == 0) return;
            int totalKeys = System.Math.Max(_colorKeys.Length, _alphaKeys.Length);
            int gradientsRequired = (totalKeys / 6) + 1;
            List<GradientColorKey>[] listOfColorKeys = new List<GradientColorKey>[gradientsRequired];
            for (int i = 0; i < gradientsRequired; i++)
            {
                Gradient newGradient = new Gradient();
                newGradient.mode = _gradientMode;
                _gradients.Add(newGradient);
                listOfColorKeys[i] = new List<GradientColorKey>();
            }
            int lastGradientIndex = 0;
            float lastKeyTime = 0f;
            for (int i = 0; i < _colorKeys.Length; i++)
            {
                float time = _colorKeys[i].time * gradientsRequired;
                int gradientIndex = System.Math.Min(Mathf.FloorToInt(time), gradientsRequired - 1);
                if (lastGradientIndex != gradientIndex)
                {
                    Color transition = Color.Lerp(_colorKeys[i - 1].color, _colorKeys[i].color, Mathf.InverseLerp(lastKeyTime, time, Mathf.Floor(time)));
                    listOfColorKeys[lastGradientIndex].Add(new GradientColorKey(transition, 1f));
                    listOfColorKeys[gradientIndex].Add(new GradientColorKey(transition, 0f));
                    lastGradientIndex = gradientIndex;
                }
                lastKeyTime = time;
                _colorKeys[i].time = time - gradientIndex;
                listOfColorKeys[gradientIndex].Add(_colorKeys[i]);
            }
            for (int i = 0; i < _gradients.Count; i++)
            {
                _gradients[i].SetKeys(listOfColorKeys[i].ToArray(), _alphaKeys);
            }
        }

        public Color Evaluate(float time)
        {
            if (_gradients.Count == 0) return Color.black;
            float extendedTime = time * _gradients.Count;
            int gradientIndex = System.Math.Min(Mathf.FloorToInt(extendedTime), _gradients.Count - 1);
            return _gradients[gradientIndex].Evaluate(extendedTime - gradientIndex);
        }
        public Texture2D CreateTexture(int width)
        {
            Texture2D texture = new Texture2D(width, 1);
            for (int i = 0; i < width; i++)
            {
                Color color = Evaluate(Mathf.InverseLerp(0f, width, i));
                texture.SetPixel(i, 0, color);
            }
            texture.Apply();
            return texture;
        }
        public static ExtendedGradient Merge(Gradient[] gradients, bool ignoreAlpha = false)
        {
            List<GradientColorKey> colorKeys = new List<GradientColorKey>();
            List<GradientAlphaKey> alphaKeys = new List<GradientAlphaKey>();
            foreach (var item in gradients)
            {
                foreach (var colorKey in item.colorKeys)
                {
                    colorKeys.Add(colorKey);
                }
                foreach (var alphaKey in item.alphaKeys)
                {
                    alphaKeys.Add(alphaKey);
                }
            }

            if (ignoreAlpha)
            {
                return new ExtendedGradient(colorKeys.ToArray());
            }
            return new ExtendedGradient(colorKeys.ToArray(), alphaKeys.ToArray());
        }
        public static ExtendedGradient Abutt(Gradient[] gradients, bool ignoreAlpha = false)
        {
            List<GradientColorKey> colorKeys = new List<GradientColorKey>();
            List<GradientAlphaKey> alphaKeys = new List<GradientAlphaKey>();
            float newRange = (1f / gradients.Length);
            for (int i = 0; i < gradients.Length; i++)
            {
                Gradient item = gradients[i];
                float thisRangeLower = newRange * i;
                float thisRangeUpper = thisRangeLower + newRange;
                foreach (var colorKey in item.colorKeys)
                {
                    float shiftedTime = colorKey.time.Remap(0f, 1f, thisRangeLower, thisRangeUpper);
                    var cKey = new GradientColorKey(colorKey.color, shiftedTime);
                    colorKeys.Add(cKey);
                }
                foreach (var alphaKey in item.alphaKeys)
                {
                    float shiftedAlphaTime = alphaKey.time.Remap(0f, 1f, thisRangeLower, thisRangeUpper);
                    var aKey = new GradientAlphaKey(alphaKey.alpha, shiftedAlphaTime);
                    alphaKeys.Add(alphaKey);
                }
            }

            if (ignoreAlpha)
            {
                return new ExtendedGradient(colorKeys.ToArray());
            }
            return new ExtendedGradient(colorKeys.ToArray(), alphaKeys.ToArray());
        }
    }

    public class FlatUIPalette
    {
        public static Color Turquoise => new Color(0.101960786f, 0.7372549f, 0.6117647f);
        public static Color GreenSea => new Color(0.08627451f, 0.627451f, 0.52156866f);
        public static Color Emerald => new Color(0.18039216f, 0.8f, 0.44313726f);
        public static Color Nephritis => new Color(0.15294118f, 0.68235296f, 0.3764706f);
        public static Color PeterRiver => new Color(0.20392157f, 0.59607846f, 0.85882354f);
        public static Color BelizeHole => new Color(0.16078432f, 0.5019608f, 0.7254902f);
        public static Color WetAsphalt => new Color(0.20392157f, 0.28627452f, 0.3862746f);
        public static Color MidnightBlue => new Color(0.17254902f, 0.24313726f, 0.3137255f);
        public static Color SoftPink => new Color(0.9176471f, 0.29803923f, 0.53333336f);
        public static Color StrongPink => new Color(0.79215693f, 0.17254902f, 0.40784317f);
        public static Color Amethyst => new Color(0.60784316f, 0.34901962f, 0.7137255f);
        public static Color Wisteria => new Color(0.5568628f, 0.26666668f, 0.6784314f);
        public static Color SunFlower => new Color(0.94509804f, 0.76862746f, 0.05882353f);
        public static Color Orange => new Color(0.9529412f, 0.6117647f, 0.07058824f);
        public static Color Carrot => new Color(0.9019608f, 0.49411765f, 0.13333334f);
        public static Color Alizarin => new Color(0.90588236f, 0.29803923f, 0.23529412f);
        public static Color Pomegranate => new Color(0.7529412f, 0.22352941f, 0.16862746f);
        public static Color Clouds => new Color(0.9254902f, 0.9411765f, 0.94509804f);
        public static Color Silver => new Color(0.7411765f, 0.7647059f, 0.78039217f);
        public static Color Concrete => new Color(0.58431375f, 0.64705884f, 0.6509804f);
        public static Color Asbestos => new Color(0.49803922f, 0.54901963f, 0.5529412f);

        public enum FlatGradeColor { Blue, Green, Red, Pink, Purple, Aqua, Orange, Tan, Brown, Metal, Stone, Gray }
        public static Color Grade(FlatGradeColor flatUIColor, float grade)
        {
            grade = Mathf.Clamp01(grade);
            switch (flatUIColor)
            {
                case FlatGradeColor.Blue:
                    return Color.Lerp(BlueHigh, BlueLow, grade);
                case FlatGradeColor.Green:
                    return Color.Lerp(GreenHigh, GreenLow, grade);
                case FlatGradeColor.Red:
                    return Color.Lerp(RedHigh, RedLow, grade);
                case FlatGradeColor.Pink:
                    return Color.Lerp(PinkHigh, PinkLow, grade);
                case FlatGradeColor.Purple:
                    return Color.Lerp(PurpleHigh, PurpleLow, grade);
                case FlatGradeColor.Aqua:
                    return Color.Lerp(AquaHigh, AquaLow, grade);
                case FlatGradeColor.Orange:
                    return Color.Lerp(OrangeHigh, OrangeLow, grade);
                case FlatGradeColor.Tan:
                    return Color.Lerp(TanHigh, TanLow, grade);
                case FlatGradeColor.Brown:
                    return Color.Lerp(BrownHigh, BrownLow, grade);
                case FlatGradeColor.Metal:
                    return Color.Lerp(MetalHigh, MetalLow, grade);
                case FlatGradeColor.Stone:
                    return Color.Lerp(StoneHigh, StoneLow, grade);
                case FlatGradeColor.Gray:
                    return Color.Lerp(GrayHigh, GrayLow, grade);
                default:
                    return Color.white;
            }
        }
        private static Color BlueHigh = new Color(0.22352943f, 0.8352942f, 1f);
        private static Color BlueLow = new Color(0.0627451f, 0.15294118f, 0.43921572f);
        private static Color GreenHigh = new Color(0.5568628f, 1f, 0.7568628f);
        private static Color GreenLow = new Color(0f, 0.36078432f, 0.003921569f);
        private static Color RedHigh = new Color(1f, 0.8000001f, 0.7372549f);
        private static Color RedLow = new Color(0.5294118f, 0f, 0f);
        private static Color PinkHigh = new Color(1f, 0.7372549f, 0.8470589f);
        private static Color PinkLow = new Color(0.5411765f, 0f, 0.15686275f);
        private static Color PurpleHigh = new Color(0.86274517f, 0.77647066f, 0.87843144f);
        private static Color PurpleLow = new Color(0.11764707f, 0f, 0.2392157f);
        private static Color AquaHigh = new Color(0.36862746f, 0.9803922f, 0.9686275f);
        private static Color AquaLow = new Color(0f, 0.35686275f, 0.3137255f);
        private static Color OrangeHigh = new Color(0.9921569f, 0.89019614f, 0.654902f);
        private static Color OrangeLow = new Color(0.65882355f, 0.26666668f, 0.0627451f);
        private static Color TanHigh = new Color(1f, 0.86274517f, 0.70980394f);
        private static Color TanLow = new Color(0.5764706f, 0.13333334f, 0.0627451f);
        private static Color BrownHigh = new Color(0.96470594f, 0.7686275f, 0.6392157f);
        private static Color BrownLow = new Color(0.36862746f, 0.17254902f, 0.043137256f);
        private static Color MetalHigh = new Color(0.7725491f, 0.82745105f, 0.8862746f);
        private static Color MetalLow = new Color(0.10980393f, 0.16470589f, 0.22352943f);
        private static Color StoneHigh = new Color(0.8352942f, 0.8980393f, 0.90196085f);
        private static Color StoneLow = new Color(0.14509805f, 0.20784315f, 0.21176472f);
        private static Color GrayHigh = new Color(0.87843144f, 0.87843144f, 0.87843144f);
        private static Color GrayLow = new Color(0f, 0f, 0f);

    }

    public class ModernPalette
    {
        public static Color Red => new Color(0.8470589f, 0.1137255f, 0.15686275f);
        public static Color Green => new Color(0.2392157f, 0.5647059f, 0.23529413f);
        public static Color Black => new Color(0.1764706f, 0.1764706f, 0.1764706f);
        public static Color BlackBlue => new Color(0.058823533f, 0.16862746f, 0.21568629f);
        public static Color BlackPurple => new Color(0.20000002f, 0.07058824f, 0.1764706f);
        public static Color White => new Color(0.9921569f, 0.98823535f, 0.98823535f);
        public static Color Gray => new Color(0.5882353f, 0.5882353f, 0.5882353f);
        public static Color GrayDark => new Color(0.34117648f, 0.34117648f, 0.34117648f);
        public static Color Orange => new Color(0.8431373f, 0.70980394f, 0.36862746f);
        public static Color OrangeDark => new Color(0.9490197f, 0.5803922f, 0f);
        public static Color Sky => new Color(0f, 0.6431373f, 0.9215687f);
        public static Color Cyan => new Color(0.06666667f, 0.63529414f, 0.627451f);
        public static Color CyanDark => new Color(0.24705884f, 0.3254902f, 0.3647059f);
        public static Color Purple => new Color(0.62352943f, 0.3254902f, 0.48627454f);
        public static Color PurpleDark => new Color(0.3372549f, 0.19215688f, 0.32156864f);
    }
    public class ColorModernized
    {
        public static Color black => ModernPalette.BlackPurple;
        public static Color blue => FlatUIPalette.BelizeHole;
        public static Color clear => Color.clear;
        public static Color cyan => ModernPalette.Cyan;
        public static Color gray => FlatUIPalette.Concrete;
        public static Color green => FlatUIPalette.Nephritis;
        public static Color grey => FlatUIPalette.Concrete;
        public static Color magenta => FlatUIPalette.StrongPink;
        public static Color red => FlatUIPalette.Pomegranate;
        public static Color white => FlatUIPalette.Clouds;
        public static Color yellow => FlatUIPalette.SunFlower;
    }
}
