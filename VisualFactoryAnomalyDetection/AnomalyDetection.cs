using Microsoft.Azure.CognitiveServices.AnomalyDetector;
using Microsoft.Azure.CognitiveServices.AnomalyDetector.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisualFactoryAnomalyDetection
{
    public class AnomalyDetection
    {
        public static async Task DetectAnomalyAsync(string anomalyDetectorKey, string anomalyDetectorEndpoint, string diffFilePath)
        {
            IAnomalyDetectorClient client = new AnomalyDetectorClient(new ApiKeyServiceClientCredentials(anomalyDetectorKey))
            {
                Endpoint = anomalyDetectorEndpoint,
            };

            Request request = GetSeriesFromFile(diffFilePath);

            // Batch anomaly detection
            await EntireDetectSampleAsync(client, request).ConfigureAwait(false);

            // Analyze the latest data point in the set.
            await LastDetectSampleAsync(client, request).ConfigureAwait(false);

            var cpdRequest = new ChangePointDetectRequest
            {
                Series = request.Series,
                Granularity = Granularity.Daily,
            };

            // Change point detection.
            await DetectChangePoint(client, cpdRequest).ConfigureAwait(false);
        }

        private static Request GetSeriesFromFile(string path)
        {
            List<Point> list = File.ReadAllLines(path, Encoding.UTF8)
                .Where(e => e.Trim().Length != 0)
                .Select(e => e.Split(','))
                .Where(e => e.Length == 2)
                .Select(e => new Point(DateTime.Parse(e[0]), double.Parse(e[1]))).ToList();

            return new Request(list, Granularity.Daily);
        }

        /// <summary>
        ///  If the time series contains any anomalies, iterate through the response's IsAnomaly values and print any that are true.
        ///  These values correspond to the index of anomalous data points, if any were found.
        /// </summary>
        private static async Task EntireDetectSampleAsync(IAnomalyDetectorClient client, Request request)
        {
            Console.WriteLine("Detecting anomalies in the entire time series ...");

            EntireDetectResponse result = await client.EntireDetectAsync(request).ConfigureAwait(false);

            if (result.IsAnomaly.Contains(true))
            {
                Console.WriteLine("An anomaly was detected at index:");
                for (int i = 0; i < request.Series.Count; ++i)
                {
                    if (result.IsAnomaly[i])
                    {
                        Console.Write(i);
                        Console.Write(" ");
                    }
                }
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("No anomalies detected in the series.");
            }
        }

        /// <summary>
        /// Check the response's IsAnomaly attribute to determine if the latest data point sent was an anomaly or not.
        /// </summary>
        private static async Task LastDetectSampleAsync(IAnomalyDetectorClient client, Request request)
        {

            Console.WriteLine("Detecting the anomaly status of the latest point in the series ...");

            LastDetectResponse result = await client.LastDetectAsync(request).ConfigureAwait(false);

            if (result.IsAnomaly)
            {
                Console.WriteLine("The latest point was detected as an anomaly.");
            }
            else
            {
                Console.WriteLine("The latest point was not detected as an anomaly.");
            }
        }

        /// <summary>
        /// Check the response's IsChangePoint values and print any that are true.
        /// These values correspond to trend change points, if any were found.
        /// </summary>
        private static async Task DetectChangePoint(IAnomalyDetectorClient client, ChangePointDetectRequest request)
        {
            Console.WriteLine("Detecting the change points in the series ...");

            ChangePointDetectResponse result = await client.ChangePointDetectAsync(request).ConfigureAwait(false);

            if (result.IsChangePoint.Contains(true))
            {
                Console.WriteLine("A change point was detected at index:");
                for (int i = 0; i < request.Series.Count; ++i)
                {
                    if (result.IsChangePoint[i])
                    {
                        Console.Write(i);
                        Console.Write(" ");
                    }
                }
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("No change point detected in the series.");
            }
        }
    }
}
