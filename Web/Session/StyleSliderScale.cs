using PaintTranslator.Imaging.Styles;

namespace PaintTranslator.Web.Session;

/// <summary>
/// Every style slider is a 0–100 integer control mapped onto the parameter's
/// declared range, as in MainForm; a hundred steps is finer than any of the
/// declared ranges needs and keeps the range inputs uniform.
/// </summary>
public static class StyleSliderScale
{
    public const int Steps = 100;

    public static int ToPosition(StyleParameter parameter, double value) =>
        (int)Math.Round((value - parameter.Minimum) / (parameter.Maximum - parameter.Minimum) * Steps);

    public static double ToValue(StyleParameter parameter, int position) =>
        parameter.Minimum + (position / (double)Steps) * (parameter.Maximum - parameter.Minimum);

    public static string Caption(StyleParameter parameter, double value) =>
        $"{parameter.Label}: {value:0.##} {parameter.Unit}".TrimEnd();
}
