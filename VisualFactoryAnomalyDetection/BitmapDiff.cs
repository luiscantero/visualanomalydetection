using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;

public class BitmapDiff
{
    private readonly int _tolerance = ushort.MaxValue / 4;

    public BitmapDiff(ushort tolerance = ushort.MaxValue / 4)
    {
        _tolerance = tolerance;
    }

    public Task<Image<L16>> GetDiffImageAsync(Stream imageStream1, Stream imageStream2)
    {
        return Task.Run(() =>
        {

            using Image<L16> image1 = Image.Load<L16>(imageStream1, new JpegDecoder());
            using Image<L16> image2 = Image.Load<L16>(imageStream2, new JpegDecoder());

            if (image1.Width != image2.Width || image1.Height != image2.Height)
            {
                throw new InvalidOperationException("Height and width of the two bitmaps to compare should match.");
            }

            if (image1.PixelType.BitsPerPixel != image2.PixelType.BitsPerPixel)
            {
                throw new InvalidOperationException("PixelType has to match.");
            }

            var pixels1 = image1.GetPixels();
            var pixels2 = image2.GetPixels();

            var resultPixels = new L16[pixels1.Length];

            for (int i = 0; i < pixels1.Length; i++)
            {
                unchecked
                {
                    resultPixels[i] = new L16((ushort)Math.Abs(pixels1[i].PackedValue - pixels2[i].PackedValue));
                }
            }

            Image<L16> result = new Image<L16>(image1.Width, image1.Height);
            result.SetPixels(resultPixels);

            return result;
        });
    }

    public Task<double> GetDiffNumberFromImageAsync(Image<L16> image)
    {
        return Task.Run(() =>
            {
                var pixels = image.GetPixels();
                var deviations = new List<L16>();

                for (int i = 0; i < pixels.Length; i++)
                {
                    if (pixels[i].PackedValue >= _tolerance)
                    {
                        deviations.Add(pixels[i]);
                        //System.Console.Write($"{bytes[i]} ");
                    }
                }

                if (deviations.Count == 0)
                {
                    //System.Console.WriteLine("No deviations!");
                    return 0;
                }

                // System.Console.WriteLine($"Count: {deviations.Count}");
                // System.Console.WriteLine($"Average: {deviations.Average(d => d.PackedValue - _treshold)}");
                // System.Console.WriteLine($"Maximum: {deviations.Max(d => d.PackedValue - _treshold)}");

                return deviations.Average(d => d.PackedValue - _tolerance);
            });
    }
}