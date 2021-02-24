using UnityEngine;
namespace Wrj
{
    public class ColorHarmony
    {
        public static Color Complementary(Color color)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            h = (h + .5f) % 1f;
            return Color.HSVToRGB(h, s, v);
        }
        public static Color[] SplitComplementary(Color color)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            Color[] results = new Color[3];
            results[0] = Color.HSVToRGB((h + .5833333f) % 1f, s, v);
            results[1] = color;
            results[2] = Color.HSVToRGB((h + .4166667f) % 1f, s, v);
            return results;
        }
        public static Color[] Triadic(Color color)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            Color[] results = new Color[3];
            results[0] = Color.HSVToRGB((h + .333f) % 1f, s, v);
            results[1] = color;
            results[2] = Color.HSVToRGB((h + .666f) % 1f, s, v);
            return results;
        }
        public static Color[] Tetradic(Color color)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            Color comp = Complementary(color);
            Color.RGBToHSV(comp, out float hC, out float sC, out float vC);
            Color[] results = new Color[4];
            results[0] = color;
            results[1] = Color.HSVToRGB((h + .097f) % 1f, s, v);
            results[2] = comp;
            results[3] = Color.HSVToRGB((hC + .097f) % 1f, sC, vC);
            return results;
        }
        public static Color Monochromatic(Color color)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            v = Mathf.Clamp01(v - .2f); 
            s = Mathf.Clamp01(v - .1f);
            return Color.HSVToRGB(h, s, v);
        }
        public static Color[] Analogous(Color color)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            Color[] results = new Color[3];
            results[0] = Color.HSVToRGB((h + .08f) % 1f, s, v);
            results[2] = Color.HSVToRGB((h - .08f) % 1f, s, v);
            return results;
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
}
