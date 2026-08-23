// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Tim
using KekuleHtml.Models;
using KekuleHtml.Services;

namespace KekuleHtml.Tests;

/// <summary>
/// <para>
/// Guards the four <see cref="MaryHillColour"/> values against silently becoming indistinguishable for people with
/// red-green colour vision deficiency. The palette is not a matter of taste. Its values were picked by measurement,
/// and this class keeps that property from being lost by a later "let's freshen up the green" edit.
/// </para>
/// <para>
/// Two kinds of test live here. The palette tests check the actual colours from
/// <see cref="MaryHillColourExtensions.ToHex(MaryHillColour)"/> against fixed thresholds. The simulation tests check the
/// colour maths itself against known properties of dichromatic vision, so a broken port of the transform cannot make the
/// palette tests pass with meaningless numbers.
/// </para>
/// <para>
/// Method: colour vision deficiency is simulated after Vienot, Brettel and Mollon (1999); distances are CIEDE2000.
/// Contrast ratios follow the WCAG relative luminance definition.
/// </para>
/// </summary>
[TestClass]
public sealed class MaryHillPaletteTests
{
    /// <summary>
    /// Minimum CIEDE2000 distance required between any two line colours under simulated colour vision deficiency.
    /// Below roughly 10 two colours are not reliably tellable apart; 25 leaves head room above that.
    /// Current worst pair: 28.6 under deuteranopia, 27.1 under protanopia. The palette used before this threshold
    /// existed scored 6.1 and would fail here.
    /// </summary>
    private const double MinimumCvdDistance = 25.0;

    /// <summary>
    /// Minimum contrast ratio of each line colour against the white page background.
    /// <para>
    /// This is deliberately NOT the WCAG threshold of 3:1 for graphical objects. <see cref="KekuleConsts.YellowHex"/>
    /// sits at 1.91:1 and has done so since long before the palette was measured. Raising the bar would fail the build
    /// rather than describe reality. The value guards against regression below today's state, nothing more.
    /// </para>
    /// </summary>
    private const double MinimumContrastRatio = 1.9;

    /// <summary>
    /// How far a colour on the intact blue-yellow axis may move through the simulation. Pure blue and pure yellow come
    /// through completely untouched, so anything measurable here means the transform is wrong.
    /// </summary>
    private const double MaximumUnaffectedShift = 1.0;

    /// <summary>
    /// Sanity floor for the distance between pure red and pure green in normal vision; they measure 86.6 apart.
    /// </summary>
    private const double MinimumNormalVisionDistance = 80.0;

    private static readonly MaryHillColour[] Palette = Enum.GetValues<MaryHillColour>();

    #region Palette

    [TestMethod]
    public void Every_pair_stays_distinguishable_under_deuteranopia()
        => AssertPairsAreDistinguishable(ColourMath.Deficiency.Deuteranopia);

    [TestMethod]
    public void Every_pair_stays_distinguishable_under_protanopia()
        => AssertPairsAreDistinguishable(ColourMath.Deficiency.Protanopia);

    private static void AssertPairsAreDistinguishable(ColourMath.Deficiency deficiency)
    {
        for (int i = 0; i < Palette.Length; i++)
        {
            for (int j = i + 1; j < Palette.Length; j++)
            {
                var first = Palette[i];
                var second = Palette[j];

                double distance = ColourMath.SimulatedDistance(first.ToHex(), second.ToHex(), deficiency);

                Assert.IsGreaterThanOrEqualTo(
                    MinimumCvdDistance,
                    distance,
                    $"{first} ({first.ToHex()}) and {second} ({second.ToHex()}) are only {distance:F1} apart under " +
                    $"{deficiency}, the minimum is {MinimumCvdDistance:F1}. Someone with this deficiency cannot tell " +
                    "the two family lines apart. See the comment above the colour constants in KekuleConsts.");
            }
        }
    }

    [TestMethod]
    public void Every_colour_keeps_its_contrast_on_the_white_page()
    {
        foreach (var colour in Palette)
        {
            double ratio = ColourMath.ContrastAgainstWhite(colour.ToHex());

            Assert.IsGreaterThanOrEqualTo(
                MinimumContrastRatio,
                ratio,
                $"{colour} ({colour.ToHex()}) only reaches {ratio:F2}:1 against white, the minimum is " +
                $"{MinimumContrastRatio:F2}:1. As a 5px person border or a 16px legend square it would be too faint.");
        }
    }

    #endregion

    #region Simulation sanity

    /// <summary>
    /// Red-green deficiency knocks out the red-green axis but leaves the blue-yellow axis intact, so pure blue and pure
    /// yellow must come through the transform untouched. If this fails, the matrices are wrong - and then every number
    /// the palette tests report is meaningless.
    /// </summary>
    [TestMethod]
    public void Blue_and_yellow_pass_through_the_simulation_unchanged()
    {
        foreach (var deficiency in new[] { ColourMath.Deficiency.Deuteranopia, ColourMath.Deficiency.Protanopia })
        {
            foreach (var hex in new[] { "#0000FF", "#FFFF00" })
            {
                double shift = ColourMath.SimulatedDistance(hex, hex, deficiency, simulateFirstOnly: true);

                Assert.IsLessThan(
                    MaximumUnaffectedShift,
                    shift,
                    $"{hex} shifted by {shift:F1} under {deficiency}, but the blue-yellow axis should be unaffected.");
            }
        }
    }

    /// <summary>
    /// The counterpart: pure red and pure green are far apart in normal vision and must move close together under
    /// deuteranopia. A transform that leaves them where they are would not simulate anything.
    /// </summary>
    [TestMethod]
    public void Red_and_green_move_together_under_deuteranopia()
    {
        double normal = ColourMath.Distance("#FF0000", "#00FF00");
        double simulated = ColourMath.SimulatedDistance("#FF0000", "#00FF00", ColourMath.Deficiency.Deuteranopia);

        Assert.IsGreaterThan(
            MinimumNormalVisionDistance,
            normal,
            $"Pure red and green should be far apart in normal vision, measured {normal:F1}.");
        Assert.IsLessThan(
            normal / 3.0,
            simulated,
            "Under deuteranopia red and green should collapse towards each other, but the distance only went from " +
            $"{normal:F1} to {simulated:F1}.");
    }

    #endregion
}

/// <summary>
/// Colour maths for the palette tests: sRGB conversion, dichromatic simulation, CIEDE2000 and WCAG contrast.
/// Lives in the test project on purpose - none of this is needed at runtime, so it stays out of the production assembly.
/// </summary>
internal static class ColourMath
{
    internal enum Deficiency
    {
        Deuteranopia,
        Protanopia
    }

    /// <summary>Hunt-Pointer-Estevez style transform from linear RGB to LMS cone response, after Vienot et al. (1999).</summary>
    private static readonly double[][] RgbToLms =
    [
        [17.8824, 43.5161, 4.11935],
        [3.45565, 27.1554, 3.86714],
        [0.0299566, 0.184309, 1.46709]
    ];

    /// <summary>Inverse of <see cref="RgbToLms"/>, pre-computed so the test does not carry a matrix inversion.</summary>
    private static readonly double[][] LmsToRgb =
    [
        [0.0809444479, -0.1305044092, 0.1167210664],
        [-0.0102485335, 0.0540193266, -0.1136147082],
        [-0.0003652969, -0.0041216147, 0.6935114049]
    ];

    /// <summary>Projects the LMS space onto the plane a deuteranope can perceive (the M cone is reconstructed from L and S).</summary>
    private static readonly double[][] DeuteranopiaProjection =
    [
        [1.0, 0.0, 0.0],
        [0.494207, 0.0, 1.24827],
        [0.0, 0.0, 1.0]
    ];

    /// <summary>The same for protanopia, where the L cone is the missing one.</summary>
    private static readonly double[][] ProtanopiaProjection =
    [
        [0.0, 2.02344, -2.52581],
        [0.0, 1.0, 0.0],
        [0.0, 0.0, 1.0]
    ];

    /// <summary>CIEDE2000 distance between two hex colours in normal vision.</summary>
    internal static double Distance(string firstHex, string secondHex)
        => Ciede2000(ToLab(ToLinearRgb(firstHex)), ToLab(ToLinearRgb(secondHex)));

    /// <summary>
    /// CIEDE2000 distance between two hex colours as a person with <paramref name="deficiency"/> would see them.
    /// With <paramref name="simulateFirstOnly"/> the second colour is left in normal vision, which measures how far a
    /// single colour moves through the simulation.
    /// </summary>
    internal static double SimulatedDistance(string firstHex, string secondHex, Deficiency deficiency, bool simulateFirstOnly = false)
    {
        var first = ToLab(Simulate(ToLinearRgb(firstHex), deficiency));
        var second = simulateFirstOnly
            ? ToLab(ToLinearRgb(secondHex))
            : ToLab(Simulate(ToLinearRgb(secondHex), deficiency));

        return Ciede2000(first, second);
    }

    /// <summary>Contrast ratio of a hex colour against white, per the WCAG relative luminance definition.</summary>
    internal static double ContrastAgainstWhite(string hex)
    {
        var rgb = ToLinearRgb(hex);
        double luminance = 0.2126 * rgb[0] + 0.7152 * rgb[1] + 0.0722 * rgb[2];

        return 1.05 / (luminance + 0.05);
    }

    #region Colour space plumbing

    /// <summary>Parses "#RRGGBB" and removes the sRGB gamma, yielding linear components in 0..1.</summary>
    private static double[] ToLinearRgb(string hex)
    {
        string digits = hex.TrimStart('#');

        return
        [
            RemoveGamma(Convert.ToInt32(digits.Substring(0, 2), 16) / 255.0),
            RemoveGamma(Convert.ToInt32(digits.Substring(2, 2), 16) / 255.0),
            RemoveGamma(Convert.ToInt32(digits.Substring(4, 2), 16) / 255.0)
        ];
    }

    private static double RemoveGamma(double channel)
        => channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);

    private static double ApplyGamma(double channel)
        => channel <= 0.0031308 ? channel * 12.92 : 1.055 * Math.Pow(channel, 1.0 / 2.4) - 0.055;

    private static double Clamp01(double value) => Math.Clamp(value, 0.0, 1.0);

    private static double[] Multiply(double[][] matrix, double[] vector)
    =>
    [
        matrix[0][0] * vector[0] + matrix[0][1] * vector[1] + matrix[0][2] * vector[2],
        matrix[1][0] * vector[0] + matrix[1][1] * vector[1] + matrix[1][2] * vector[2],
        matrix[2][0] * vector[0] + matrix[2][1] * vector[1] + matrix[2][2] * vector[2]
    ];

    /// <summary>
    /// Runs a linear RGB colour through the dichromatic projection and back. The round trip through gamma mirrors what a
    /// display actually shows, so the result matches what the simulator images look like.
    /// </summary>
    private static double[] Simulate(double[] linearRgb, Deficiency deficiency)
    {
        var projection = deficiency == Deficiency.Deuteranopia ? DeuteranopiaProjection : ProtanopiaProjection;
        var projected = Multiply(LmsToRgb, Multiply(projection, Multiply(RgbToLms, linearRgb)));

        return [.. projected.Select(channel => RemoveGamma(Clamp01(ApplyGamma(Clamp01(channel)))))];
    }

    /// <summary>Linear RGB to CIELAB under a D65 white point.</summary>
    private static double[] ToLab(double[] linearRgb)
    {
        double r = linearRgb[0], g = linearRgb[1], b = linearRgb[2];

        double x = (r * 0.4124 + g * 0.3576 + b * 0.1805) / 0.95047;
        double y = r * 0.2126 + g * 0.7152 + b * 0.0722;
        double z = (r * 0.0193 + g * 0.1192 + b * 0.9505) / 1.08883;

        static double Pivot(double value) => value > 0.008856 ? Math.Cbrt(value) : 7.787 * value + 16.0 / 116.0;

        double fx = Pivot(x), fy = Pivot(y), fz = Pivot(z);

        return [116.0 * fy - 16.0, 500.0 * (fx - fy), 200.0 * (fy - fz)];
    }

    #endregion

    #region CIEDE2000

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;

    /// <summary>
    /// CIEDE2000 colour difference with all weighting factors at 1. The branches on hue exist because hue is an angle:
    /// the difference between 359 degrees and 1 degree is 2, not 358, and hue is undefined when chroma is zero.
    /// </summary>
    private static double Ciede2000(double[] first, double[] second)
    {
        double l1 = first[0], a1 = first[1], b1 = first[2];
        double l2 = second[0], a2 = second[1], b2 = second[2];

        double chroma1 = Math.Sqrt(a1 * a1 + b1 * b1);
        double chroma2 = Math.Sqrt(a2 * a2 + b2 * b2);
        double chromaMean = (chroma1 + chroma2) / 2.0;

        double pow7 = Math.Pow(chromaMean, 7.0);
        double g = chromaMean > 0.0 ? 0.5 * (1.0 - Math.Sqrt(pow7 / (pow7 + Math.Pow(25.0, 7.0)))) : 0.0;

        double a1Prime = (1.0 + g) * a1;
        double a2Prime = (1.0 + g) * a2;

        double chroma1Prime = Math.Sqrt(a1Prime * a1Prime + b1 * b1);
        double chroma2Prime = Math.Sqrt(a2Prime * a2Prime + b2 * b2);

        double hue1 = HueAngle(a1Prime, b1);
        double hue2 = HueAngle(a2Prime, b2);

        double deltaL = l2 - l1;
        double deltaChroma = chroma2Prime - chroma1Prime;

        double deltaHue;
        if (chroma1Prime * chroma2Prime == 0.0)
        {
            deltaHue = 0.0;
        }
        else if (Math.Abs(hue2 - hue1) <= 180.0)
        {
            deltaHue = hue2 - hue1;
        }
        else if (hue2 - hue1 > 180.0)
        {
            deltaHue = hue2 - hue1 - 360.0;
        }
        else
        {
            deltaHue = hue2 - hue1 + 360.0;
        }

        double deltaBigHue = 2.0 * Math.Sqrt(chroma1Prime * chroma2Prime) * Math.Sin(ToRadians(deltaHue) / 2.0);

        double lightnessMean = (l1 + l2) / 2.0;
        double chromaPrimeMean = (chroma1Prime + chroma2Prime) / 2.0;

        double hueMean;
        if (chroma1Prime * chroma2Prime == 0.0)
        {
            hueMean = hue1 + hue2;
        }
        else if (Math.Abs(hue1 - hue2) <= 180.0)
        {
            hueMean = (hue1 + hue2) / 2.0;
        }
        else if (hue1 + hue2 < 360.0)
        {
            hueMean = (hue1 + hue2 + 360.0) / 2.0;
        }
        else
        {
            hueMean = (hue1 + hue2 - 360.0) / 2.0;
        }

        double t = 1.0
            - 0.17 * Math.Cos(ToRadians(hueMean - 30.0))
            + 0.24 * Math.Cos(ToRadians(2.0 * hueMean))
            + 0.32 * Math.Cos(ToRadians(3.0 * hueMean + 6.0))
            - 0.20 * Math.Cos(ToRadians(4.0 * hueMean - 63.0));

        double theta = 30.0 * Math.Exp(-Math.Pow((hueMean - 275.0) / 25.0, 2.0));

        double chromaPrimePow7 = Math.Pow(chromaPrimeMean, 7.0);
        double rotationChroma = chromaPrimeMean > 0.0
            ? 2.0 * Math.Sqrt(chromaPrimePow7 / (chromaPrimePow7 + Math.Pow(25.0, 7.0)))
            : 0.0;

        double lightnessWeight = 1.0
            + 0.015 * Math.Pow(lightnessMean - 50.0, 2.0) / Math.Sqrt(20.0 + Math.Pow(lightnessMean - 50.0, 2.0));
        double chromaWeight = 1.0 + 0.045 * chromaPrimeMean;
        double hueWeight = 1.0 + 0.015 * chromaPrimeMean * t;

        double rotation = -Math.Sin(ToRadians(2.0 * theta)) * rotationChroma;

        double lightnessTerm = deltaL / lightnessWeight;
        double chromaTerm = deltaChroma / chromaWeight;
        double hueTerm = deltaBigHue / hueWeight;

        return Math.Sqrt(
            lightnessTerm * lightnessTerm
            + chromaTerm * chromaTerm
            + hueTerm * hueTerm
            + rotation * chromaTerm * hueTerm);
    }

    private static double HueAngle(double a, double b)
    {
        if (a == 0.0 && b == 0.0)
        {
            return 0.0;
        }

        double degrees = Math.Atan2(b, a) * 180.0 / Math.PI;

        return degrees < 0.0 ? degrees + 360.0 : degrees;
    }

    #endregion
}
