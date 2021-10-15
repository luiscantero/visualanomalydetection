using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

public static class ImageHelpers
{
    public static TPixel[] GetPixels<TPixel>(this Image<TPixel> image) where TPixel : unmanaged, IPixel<TPixel>
    {

        if (image.TryGetSinglePixelSpan(out Span<TPixel> span))
        {
            return span.ToArray();
        }

        return null;
    }

    public static void SetPixels<TPixel>(this Image<TPixel> image, TPixel[] pixels) where TPixel : unmanaged, IPixel<TPixel>
    {
        if (!image.TryGetSinglePixelSpan(out Span<TPixel> span))
        {
            throw new Exception("Couldn't get pixels.");
        }

        for (int i = 0; i < pixels.Length; i++)
        {
            span[i] = pixels[i];
        }
    }
}