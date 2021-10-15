using MMALSharp;
using MMALSharp.Common;
using MMALSharp.Common.Utility;
using MMALSharp.Components;
using MMALSharp.Handlers;
using MMALSharp.Native;
using MMALSharp.Ports;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace VisualFactoryAnomalyDetection
{
    public class PiCameraService
    {
        public MMALCamera MMALCamera;
        private readonly string _picStoragePath = "/home/pi/images/";
        private readonly string _viseoStoragePath = "/home/pi/video/";
        private readonly string _videoExtension = "mp4";

        public PiCameraService()
        {
            MMALCamera = MMALCamera.Instance;

            // Photo.
            // https://picamera.readthedocs.io/en/release-1.12/fov.html
            //MMALCameraConfig.StillResolution = new Resolution(3280, 2464); // 8 MP.
            //MMALCameraConfig.StillResolution = new Resolution(1640, 1232); // 4 MP (Binning).
            //MMALCameraConfig.StillResolution = new Resolution(1640, 922); // (Binning).
            //MMALCameraConfig.StillResolution = new Resolution(1920, 1080); // 2 MP.
            //MMALCameraConfig.StillResolution = Resolution.As720p; // 1280x720 (Binning).
            MMALCameraConfig.StillResolution = Resolution.As03MPixel; // 640x480 (Binning).
            MMALCameraConfig.ISO = 100;
            //MMALCameraConfig.Rotation = 180;

            // --------------------------------------------------------------------------------

            // Video and rapid capture mode settings.
            //MMALCameraConfig.VideoResolution = Resolution.As1080p; // 1920x1080.

            // JPEG: 43 pics/s as file | 53 pics/s in memory.
            //MMALCameraConfig.VideoResolution = Resolution.As720p; // 1280x720 (Binning).
            //MMALCameraConfig.VideoFramerate = new MMAL_RATIONAL_T(60, 1);

            // JPEG: 75 pics/s as file | 89 pics/s in memory.
            // BMP: 3 pics/s in memory.
            MMALCameraConfig.VideoResolution = Resolution.As03MPixel; // 640x480 (Binning).
            MMALCameraConfig.VideoFramerate = new MMAL_RATIONAL_T(90, 1);

            //MMALCameraConfig.VideoProfile = MMALParametersVideo.MMAL_VIDEO_PROFILE_T.MMAL_VIDEO_PROFILE_;
        }

        /// <summary>
        /// Capture JPG from video port (rapid capture mode) in memory using custom handler.
        /// </summary>
        public async Task<List<PictureWithTimestamp>> CaptureJpgFromVideoPortCustomHandlerAsync(TimeSpan captureTime)
        {
            List<PictureWithTimestamp> pictureList = null;

            using (var imgCaptureHandler = new CustomOutputHandler(_picStoragePath, "jpg"))
            using (var splitter = new MMALSplitterComponent())
            // Resolution controlled by MMALCameraConfig.VideoResolution.
            using (var imgEncoder = new MMALImageEncoder(continuousCapture: true))
            using (var nullSink = new MMALNullSinkComponent())
            {
                MMALCamera.ConfigureCameraSettings();

                var encInputConfig = new MMALPortConfig(MMALEncoding.OPAQUE, MMALEncoding.I420);
                var encOutputConfig = new MMALPortConfig(MMALEncoding.JPEG, MMALEncoding.I420);

                // Create our component pipeline.
                imgEncoder
                        .ConfigureInputPort(encInputConfig, null)
                        .ConfigureOutputPort(encOutputConfig, imgCaptureHandler);

                MMALCamera.Camera.VideoPort.ConnectTo(splitter);
                splitter.Outputs[0].ConnectTo(imgEncoder);
                MMALCamera.Camera.PreviewPort.ConnectTo(nullSink);

                // Camera warm up time
                await Task.Delay(2000).ConfigureAwait(false);

                var cts = new CancellationTokenSource(captureTime);

                // Process images for x seconds.
                await MMALCamera.ProcessAsync(MMALCamera.Camera.VideoPort, cts.Token).ConfigureAwait(false);

                pictureList = imgCaptureHandler.WorkingData;
            }

            // Only call when you no longer require the camera, i.e. on app shutdown.
            MMALCamera.Cleanup();

            return pictureList;
        }

        #region Capture picture
        public async Task<byte[]> CaptureRawPictureInMemoryAsync()
        {
            byte[] pictureBytes = null;
            using (var imgCaptureHandler = new InMemoryCaptureHandler())
            {
                await MMALCamera.TakeRawPicture(
                    imgCaptureHandler).ConfigureAwait(false);

                pictureBytes = imgCaptureHandler.WorkingData.ToArray();
            }
            return pictureBytes;
        }

        public async Task<string> CaptureBmpAndGetFileNameAsync()
        {
            string fileName = null;
            using (var imgCaptureHandler = new ImageStreamCaptureHandler(_picStoragePath, "bmp"))
            {
                await MMALCamera.TakePicture(
                    imgCaptureHandler,
                    encodingType: MMALEncoding.BMP,
                    pixelFormat: MMALEncoding.I420).ConfigureAwait(false);

                fileName = imgCaptureHandler.GetFilename();
            }
            return fileName;
        }

        public async Task<string> CaptureJpgAndGetFileNameAsync()
        {
            string fileName = null;
            using (var imgCaptureHandler = new ImageStreamCaptureHandler(_picStoragePath, "jpg"))
            {
                await MMALCamera.TakePicture(
                    imgCaptureHandler,
                    encodingType: MMALEncoding.JPEG,
                    pixelFormat: MMALEncoding.I420).ConfigureAwait(false);

                fileName = imgCaptureHandler.GetFilename();
            }
            return fileName;
        }

        public async Task<byte[]> CaptureBmpInMemoryAsync()
        {
            byte[] pictureBytes = null;
            using (var imgCaptureHandler = new InMemoryCaptureHandler())
            {
                await MMALCamera.TakePicture(
                   imgCaptureHandler,
                   encodingType: MMALEncoding.BMP,
                   pixelFormat: MMALEncoding.I420).ConfigureAwait(false);

                pictureBytes = imgCaptureHandler.WorkingData.ToArray();
            }
            return pictureBytes;
        }

        public async Task<byte[]> CaptureJpgInMemoryAsync()
        {
            byte[] pictureBytes = null;
            using (var imgCaptureHandler = new InMemoryCaptureHandler())
            {
                await MMALCamera.TakePicture(
                   imgCaptureHandler,
                   encodingType: MMALEncoding.JPEG,
                   pixelFormat: MMALEncoding.I420).ConfigureAwait(false);

                pictureBytes = imgCaptureHandler.WorkingData.ToArray();
            }
            return pictureBytes;
        }

        /// <summary>
        /// Capture JPG from video port (rapid capture mode) to files.
        /// </summary>
        public async Task CaptureJpgFromVideoPortAsync(TimeSpan captureTime)
        {
            using (var imgCaptureHandler = new ImageStreamCaptureHandler(_picStoragePath, "jpg"))
            using (var splitter = new MMALSplitterComponent())
            // Resolution controlled by MMALCameraConfig.VideoResolution.
            using (var imgEncoder = new MMALImageEncoder(continuousCapture: true))
            using (var nullSink = new MMALNullSinkComponent())
            {
                MMALCamera.ConfigureCameraSettings();

                var portConfig = new MMALPortConfig(MMALEncoding.JPEG, MMALEncoding.I420);

                // Create our component pipeline.
                imgEncoder.ConfigureOutputPort(portConfig, imgCaptureHandler);

                MMALCamera.Camera.VideoPort.ConnectTo(splitter);
                splitter.Outputs[0].ConnectTo(imgEncoder);
                MMALCamera.Camera.PreviewPort.ConnectTo(nullSink);

                // Camera warm up time
                await Task.Delay(2000).ConfigureAwait(false);

                var cts = new CancellationTokenSource(captureTime);

                // Process images for x seconds.
                await MMALCamera.ProcessAsync(MMALCamera.Camera.VideoPort, cts.Token).ConfigureAwait(false);
            }

            // Only call when you no longer require the camera, i.e. on app shutdown.
            MMALCamera.Cleanup();
        }
        #endregion

        #region Capture video
        public async Task<string> CaptureVideoAndGetFileNameAsync(TimeSpan captureTime)
        {
            string fileName = null;
            using (var vidCaptureHandler = new VideoStreamCaptureHandler(_viseoStoragePath, _videoExtension))
            {
                var cts = new CancellationTokenSource(captureTime);

                await MMALCamera.TakeVideo(vidCaptureHandler, cts.Token).ConfigureAwait(false);
                fileName = vidCaptureHandler.GetFilename();
            }
            return fileName;
        }
        #endregion
    }
}