using Microsoft.Xna.Framework;

namespace Merthsoft.Moose.Merthsoft.GravityCa.GameLibrary;

public static class Extensions
{
    public static TEnum Next<TEnum>(this TEnum t) where TEnum : struct, Enum
    {
        var values = Enum.GetValues<TEnum>();
        var index = Array.IndexOf(values, t);
        index = index >= values.Length - 1 ? 0 : index + 1;
        return values[index];
    }
    public static IEnumerable<Color> ColorGradient(Color start, Color end, int steps)
    {
        var stepA = (end.A - start.A) / (steps - 1);
        var stepR = (end.R - start.R) / (steps - 1);
        var stepG = (end.G - start.G) / (steps - 1);
        var stepB = (end.B - start.B) / (steps - 1);

        for (var i = 0; i < steps; i++)
            yield return Color.FromNonPremultiplied(start.R + stepR * i,
                                                    start.G + stepG * i,
                                                    start.B + stepB * i,
                                                    start.A + stepA * i);
    }

    public static Color ColorGradientPercentage(Color start, Color end, double percent)
    {
        var startHsl = end.ToHsl();
        var endHsl = start.ToHsl();
        var alt = 1.0f - percent;

        var h = percent * startHsl.H + alt * endHsl.H;
        var s = percent * startHsl.S + alt * endHsl.S;
        var l = percent * startHsl.L + alt * endHsl.L;

        return FromHsl(h, s, l);
    }

    public static HslColor ToHsl(this Color c)
    {
        float num = (float)(int)c.R / 255f;
        float num2 = (float)(int)c.B / 255f;
        float num3 = (float)(int)c.G / 255f;
        float num4 = Math.Max(Math.Max(num, num3), num2);
        float num5 = Math.Min(Math.Min(num, num3), num2);
        float num6 = num4 - num5;
        float num7 = num4 + num5;
        float num8 = num7 * 0.5f;
        if (num6 == 0f)
        {
            return new HslColor(0f, 0f, num8);
        }

        float h = ((num == num4) ? ((60f * (num3 - num2) / num6 + 360f) % 360f) : ((num3 != num4) ? (60f * (num - num3) / num6 + 240f) : (60f * (num2 - num) / num6 + 120f)));
        float s = ((num8 <= 0.5f) ? (num6 / num7) : (num6 / (2f - num7)));
        return new HslColor(h, s, num8);
    }

    public static Color FromHsl(double h, double s, double l)
    {
        byte r = 0;
        byte g = 0;
        byte b = 0;

        if (s == 0)
            r = g = b = (byte)(l * 255);
        else
        {
            double v1, v2;
            var hue = h / 360.0;

            v2 = l < 0.5 ? l * (1 + s) : l + s - l * s;
            v1 = 2 * l - v2;

            r = (byte)(255 * HueToRGB(v1, v2, hue + 1.0 / 3));
            g = (byte)(255 * HueToRGB(v1, v2, hue));
            b = (byte)(255 * HueToRGB(v1, v2, hue - 1.0 / 3));
        }

        return new(r, g, b);
    }

    private static double HueToRGB(double v1, double v2, double vH)
    {
        if (vH < 0)
            vH += 1;

        if (vH > 1)
            vH -= 1;

        if (6 * vH < 1)
            return v1 + (v2 - v1) * 6 * vH;

        if (2 * vH < 1)
            return v2;

        if (3 * vH < 2)
            return v1 + (v2 - v1) * (2.0f / 3 - vH) * 6;

        return v1;
    }
}
