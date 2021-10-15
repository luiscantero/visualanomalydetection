using MMALSharp.Common;
using MMALSharp.Handlers;
using System;
using System.Collections.Generic;
using System.IO;

namespace VisualFactoryAnomalyDetection
{
    /// <inheritdoc/>
    public class CustomOutputHandler : IOutputCaptureHandler
    {
        private readonly string _picStoragePath;
        private readonly string _picExtension;
        private bool _isFirstPicture = true;
        private List<byte> _currentPicture;

        public int Counter { get; set; }
        public List<PictureWithTimestamp> WorkingData { get; set; } = new List<PictureWithTimestamp>();

        public CustomOutputHandler(string picStoragePath, string picExtension)
        {
            _picStoragePath = picStoragePath;
            _picExtension = picExtension;
        }

        /// <summary>
        /// Process picture (partial) data stream.
        /// </summary>
        public void Process(ImageContext context)
        {
            _currentPicture ??= new List<byte>();

            // Append bytes to current picture.
            _currentPicture.AddRange(context.Data);
        }

        /// <summary>
        /// Process picture after reaching end-of-stream.
        /// </summary>
        public void PostProcess()
        {
            Console.Write(".");

            // Add picture bytes and timestamp to list.
            WorkingData.Add(new PictureWithTimestamp
            {
                Data = _currentPicture.ToArray(),
                Timestamp = DateTime.UtcNow,
            });

            // Save first picture to file for verification.
            if (_isFirstPicture)
            {
                File.WriteAllBytes($"{_picStoragePath}FirstPicture.{_picExtension}", _currentPicture.ToArray());
                _isFirstPicture = false;
            }

            _currentPicture = null;
        }

        /// <inheritdoc/>
        public string TotalProcessed()
        {
            return Counter.ToString();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            WorkingData = null;
            _currentPicture = null;
        }
    }
}