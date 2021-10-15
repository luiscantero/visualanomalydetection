using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VisualFactoryAnomalyDetection
{
    class Program
    {
        private static readonly PiCameraService _piCamService = new PiCameraService();

        static async Task Main(string[] args)
        {
            try
            {
                await CapturePicturesProcessDiffAsync(tolerance: 0).ConfigureAwait(false);

                // --------------------------------------------------------------------------------

                //await CaptureBmpAndJpgPictureAsync().ConfigureAwait(false);

                // Saves ~30 pics/s using JPG.
                //await _piCamService.CaptureJpgFromVideoPortAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

                // Video not decodable, tried different profiles.
                //Console.WriteLine(await _piCamService.CaptureVideoAndGetFileNameAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false));

                // The following tests are too slow, only a couple of pics/s.
                var ct = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                //await CaptureBmpPicturesAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (AggregateException e)
            {
                Console.WriteLine(e.Flatten().Message);
            }

            Console.WriteLine("Done");
        }

        private static async Task CapturePicturesProcessDiffAsync(ushort tolerance)
        {
            var pictures = await _piCamService.CaptureJpgFromVideoPortCustomHandlerAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            Console.WriteLine();
            Console.WriteLine($"Captured pictures: {pictures.Count:N0}");

            var imageDiff = new BitmapDiff(tolerance);
            Stream previous = null;
            var sb = new StringBuilder();

            foreach (var picture in pictures)
            {
                // Remember first picture.
                if (previous == null)
                {
                    previous = new MemoryStream(picture.Data);
                    continue;
                }

                Stream current = new MemoryStream(picture.Data);

                // Calculate diff.
                using var diff = await imageDiff.GetDiffImageAsync(previous, current).ConfigureAwait(false);
                var diffNumber = await imageDiff.GetDiffNumberFromImageAsync(diff).ConfigureAwait(false);

                Console.WriteLine($"Diff between previous and current pic: {diffNumber:N0}");
                sb.Append($"{picture.Timestamp},{diffNumber}");

                previous = current;
                previous.Position = 0; // Seek to beginning of stream.
            }

            // Write file for anomaly detection.
            await File.WriteAllTextAsync("diff.csv", sb.ToString()).ConfigureAwait(false);
        }

        private static async Task CaptureBmpAndJpgPictureAsync()
        {
            Console.WriteLine(await _piCamService.CaptureBmpAndGetFileNameAsync().ConfigureAwait(false));
            Console.WriteLine(await _piCamService.CaptureJpgAndGetFileNameAsync().ConfigureAwait(false));
        }

        private static async Task CaptureBmpPicturesAsync(CancellationTokenSource ct)
        {
            while (true)
            {
                ct.Token.ThrowIfCancellationRequested();

                Console.WriteLine(await _piCamService.CaptureBmpAndGetFileNameAsync().ConfigureAwait(false));
            }
        }
    }
}
