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
        }

        private static Request GetSeriesFromFile(string path)
        {
            List<Point> list = File.ReadLines(path, Encoding.UTF8)
                // Ignore empty lines.
                .Where(e => e.Trim().Length != 0)
                // Split at the comma.
                .Select(e => e.Split(','))
                // Take only lines with two columns.
                .Where(e => e.Length == 2)
                // Parse Timestamp and Value.
                .Select(e => new Point(DateTime.Parse(e[0]), double.Parse(e[1])))
                // Group by the Timestamp's second component to match the Granularity.Secondly.
                .GroupBy(p => new { p.Timestamp.Date, p.Timestamp.Hour, p.Timestamp.Minute, p.Timestamp.Second })
                // Select the Timestamp with the max. millisecond component within a specific second as the key,
                // and the max. value within that second as the Value.
                .Select(g => new Point(g.Single(p => p.Timestamp.Millisecond == g.Max(p => p.Timestamp.Millisecond)).Timestamp, g.Max(p => p.Value)))
                .ToList();

            return new Request(list, Granularity.Secondly);
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
    }
}
