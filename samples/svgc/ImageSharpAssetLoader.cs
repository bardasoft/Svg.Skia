using System.Collections.Generic;
using System.IO;

namespace svgc;

internal class ImageSharpAssetLoader : Svg.Model.ISvgAssetLoader
{
    public ShimSkiaSharp.SKImage LoadImage(Stream stream)
    {
        var data = ShimSkiaSharp.SKImage.FromStream(stream);
        using var image = SixLabors.ImageSharp.Image.Load(data);
        return new ShimSkiaSharp.SKImage {Data = data, Width = image.Width, Height = image.Height};
    }

    public List<Svg.Model.TypefaceSpan> FindTypefaces(string? text, ShimSkiaSharp.SKPaint paintPreferredTypeface)
    {
        if (text is null || string.IsNullOrEmpty(text))
        {
            return new List<Svg.Model.TypefaceSpan>();
        }

        // TODO:
        // Font fallback and text advancing code should be generated along with canvas commands instead.
        // Otherwise, some package reference hacking may be needed.
        return new List<Svg.Model.TypefaceSpan>
        {
            new(text, text.Length * paintPreferredTypeface.TextSize, paintPreferredTypeface.Typeface)
        };
    }

    public ShimSkiaSharp.SKFontMetrics GetFontMetrics(ShimSkiaSharp.SKPaint paint)
    {
        throw new System.NotSupportedException("Font metrics are not supported by the ImageSharp asset loader.");
    }

    public float MeasureText(string? text, ShimSkiaSharp.SKPaint paint, ref ShimSkiaSharp.SKRect bounds)
    {
        throw new System.NotSupportedException("Text measurement is not supported by the ImageSharp asset loader.");
    }
}
