using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using CapturePanorama.Internals;
using UnityEngine;
using VRGIN.Core;

namespace CapturePanorama
{
    public class CapturePanorama : ProtectedBehaviour
    {
        public enum ImageFormat
        {
            PNG = 0,
            JPEG = 1,
            BMP = 2
        }

        public enum AntiAliasing
        {
            _1 = 1,
            _2 = 2,
            _4 = 4,
            _8 = 8
        }

        public string panoramaName;

        public string qualitySetting;

        public KeyCode captureKey = KeyCode.P;

        public ImageFormat imageFormat;

        public bool captureStereoscopic;

        public float interpupillaryDistance = 0.0635f;

        public int numCirclePoints = 128;

        public int panoramaWidth = 8192;

        public AntiAliasing antiAliasing = AntiAliasing._8;

        public int ssaaFactor = 1;

        public string saveImagePath = "";

        public bool saveCubemap;

        public bool uploadImages;

        public bool useDefaultOrientation;

        public bool useGpuTransform = true;

        public float cpuMillisecondsPerFrame = 8.333333f;

        public bool captureEveryFrame;

        public int frameRate = 30;

        public int maxFramesToRecord;

        public int frameNumberDigits = 6;

        public AudioClip startSound;

        public AudioClip doneSound;

        public AudioClip failSound;

        public bool fadeDuringCapture = true;

        public float fadeTime = 0.25f;

        public UnityEngine.Color fadeColor = new UnityEngine.Color(0f, 0f, 0f, 1f);

        public Material fadeMaterial;

        public ComputeShader convertPanoramaShader;

        public ComputeShader convertPanoramaStereoShader;

        public ComputeShader textureToBufferShader;

        public bool enableDebugging;

        private string apiUrl = "http://alpha.vrchive.com/api/1/";

        private string apiKey = "0b26e4dca20793a83fd92ad83e3e859e";

        private GameObject[] camGos;

        private Camera cam;

        private ImageEffectCopyCamera copyCameraScript;

        private bool capturingEveryFrame;

        private bool usingGpuTransform;

        private CubemapFace[] faces;

        private int panoramaHeight;

        private int cameraWidth;

        private int cameraHeight;

        private RenderTexture cubemapRenderTexture;

        private Texture2D forceWaitTexture;

        private int convertPanoramaKernelIdx = -1;

        private int convertPanoramaYPositiveKernelIdx = -1;

        private int convertPanoramaYNegativeKernelIdx = -1;

        private int textureToBufferIdx = -1;

        private int renderStereoIdx = -1;

        private int[] convertPanoramaKernelIdxs;

        private byte[] imageFileBytes;

        private string videoBaseName = "";

        private int frameNumber;

        private const int ResultBufferSlices = 8;

        private float hFov = -1f;

        private float vFov = -1f;

        private float hFovAdjustDegrees = -1f;

        private float vFovAdjustDegrees = -1f;

        private float circleRadius = -1f;

        private int threadsX = 32;

        private int threadsY = 32;

        private int numCameras;

        private const int CamerasPerCirclePoint = 4;

        private uint[] cameraPixels;

        private uint[] resultPixels;

        private float tanHalfHFov;

        private float tanHalfVFov;

        private float hFovAdjust;

        private float vFovAdjust;

        private int overlapTextures;

        private bool initializeFailed = true;

        private AudioSource audioSource;

        private const uint BufferSentinelValue = 1419455993u;

        private int lastConfiguredPanoramaWidth;

        private int lastConfiguredNumCirclePoints;

        private int lastConfiguredSsaaFactor;

        private float lastConfiguredInterpupillaryDistance;

        private bool lastConfiguredCaptureStereoscopic;

        private bool lastConfiguredSaveCubemap;

        private bool lastConfiguredUseGpuTransform;

        private AntiAliasing lastConfiguredAntiAliasing = AntiAliasing._1;

        private static CapturePanorama instance;

        internal bool Capturing;

        private static List<Process> resizingProcessList = new List<Process>();

        private static List<string> resizingFilenames = new List<string>();

        private System.Drawing.Imaging.ImageFormat FormatToDrawingFormat(ImageFormat format)
        {
            return format switch
            {
                ImageFormat.PNG => System.Drawing.Imaging.ImageFormat.Png,
                ImageFormat.JPEG => System.Drawing.Imaging.ImageFormat.Jpeg,
                ImageFormat.BMP => System.Drawing.Imaging.ImageFormat.Bmp,
                _ => System.Drawing.Imaging.ImageFormat.Png
            };
        }

        private string FormatMimeType(ImageFormat format)
        {
            return format switch
            {
                ImageFormat.PNG => "image/png",
                ImageFormat.JPEG => "image/jpeg",
                ImageFormat.BMP => "image/bmp",
                _ => ""
            };
        }

        private string FormatToExtension(ImageFormat format)
        {
            return format switch
            {
                ImageFormat.PNG => "png",
                ImageFormat.JPEG => "jpg",
                ImageFormat.BMP => "bmp",
                _ => ""
            };
        }

        protected override void OnAwake()
        {
            if (instance == null)
                instance = this;
            else
                UnityEngine.Debug.LogError("More than one CapturePanorama instance detected.");
        }

        protected override void OnStart()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
            audioSource.Play();
            Reinitialize();
            VRLog.Info("Started panorama");
        }

        private float IpdScaleFunction(float latitudeNormalized)
        {
            return 1.5819767f * Mathf.Exp((0f - latitudeNormalized) * latitudeNormalized) - 0.5819767f;
        }

        public virtual void OnDestroy()
        {
            Cleanup();
        }

        private void Cleanup()
        {
            faces = null;
            Destroy(copyCameraScript);
            Destroy(cam);
            if (camGos != null)
            {
                for (var num = camGos.Length - 1; num >= 0; num--)
                {
                    if (camGos[num] != null) Destroy(camGos[num]);
                }
            }

            camGos = null;
            numCameras = -1;
            hFov = vFov = -1f;
            if (cubemapRenderTexture != null) Destroy(cubemapRenderTexture);
            cubemapRenderTexture = null;
            convertPanoramaKernelIdx = renderStereoIdx = textureToBufferIdx = -1;
            convertPanoramaKernelIdxs = null;
            resultPixels = cameraPixels = null;
            if (forceWaitTexture != null) Destroy(forceWaitTexture);
            forceWaitTexture = new Texture2D(1, 1);
        }

        private void Reinitialize()
        {
            try
            {
                ReinitializeBody();
            }
            catch (Exception)
            {
                Cleanup();
                throw;
            }
        }

        private void ReinitializeBody()
        {
            Log("Settings changed, calling Reinitialize()");
            initializeFailed = true;
            if (!SystemInfo.supportsComputeShaders)
            {
                UnityEngine.Debug.LogWarning(
                    "CapturePanorama requires compute shaders. Your system does not support them. On PC, compute shaders require DirectX 11, Windows Vista or later, and a GPU capable of Shader Model 5.0.");
                return;
            }

            lastConfiguredCaptureStereoscopic = captureStereoscopic;
            lastConfiguredPanoramaWidth = panoramaWidth;
            lastConfiguredInterpupillaryDistance = interpupillaryDistance;
            lastConfiguredNumCirclePoints = numCirclePoints;
            lastConfiguredSsaaFactor = ssaaFactor;
            lastConfiguredAntiAliasing = antiAliasing;
            lastConfiguredSaveCubemap = saveCubemap;
            lastConfiguredUseGpuTransform = useGpuTransform;
            Cleanup();
            faces = new CubemapFace[6]
            {
                CubemapFace.PositiveX,
                CubemapFace.NegativeX,
                CubemapFace.PositiveY,
                CubemapFace.NegativeY,
                CubemapFace.PositiveZ,
                CubemapFace.NegativeZ
            };
            panoramaHeight = panoramaWidth / 2;
            camGos = new GameObject[3];
            for (var i = 0; i < 3; i++)
            {
                camGos[i] = new GameObject("PanoramaCaptureCamera" + i);
                camGos[i].hideFlags = HideFlags.HideAndDontSave;
                if (i > 0) camGos[i].transform.parent = camGos[i - 1].transform;
            }

            camGos[2].AddComponent<Camera>();
            cam = camGos[2].GetComponent<Camera>();
            cam.enabled = false;
            camGos[2].AddComponent<ImageEffectCopyCamera>();
            copyCameraScript = camGos[2].GetComponent<ImageEffectCopyCamera>();
            copyCameraScript.enabled = false;
            numCameras = faces.Length;
            hFov = vFov = 90f;
            if (captureStereoscopic)
            {
                var num = 360f / (float)numCirclePoints;
                var num2 = 0.001f;
                var b = 2f * ((float)Math.PI / 2f - Mathf.Acos(IpdScaleFunction(0.5f))) * 360f / ((float)Math.PI * 2f);
                hFov = Mathf.Max(90f + num, b) + num2;
                vFov = 90f;
                numCameras = 2 + numCirclePoints * 4;
                circleRadius = interpupillaryDistance / 2f;
                hFovAdjustDegrees = hFov / 2f;
                vFovAdjustDegrees = vFov / 2f;
            }

            var num3 = (double)panoramaWidth * 90.0 / 360.0;
            cameraWidth = (int)Math.Ceiling(Math.Tan(hFov * ((float)Math.PI * 2f) / 360f / 2f) * num3 * (double)ssaaFactor);
            cameraHeight = (int)Math.Ceiling(Math.Tan(vFov * ((float)Math.PI * 2f) / 360f / 2f) * num3 * (double)ssaaFactor);
            Log("Number of cameras: " + numCameras);
            Log("Camera dimensions: " + cameraWidth + "x" + cameraHeight);
            usingGpuTransform = useGpuTransform && convertPanoramaShader != null;
            cubemapRenderTexture = new RenderTexture(cameraWidth, cameraHeight, 24, RenderTextureFormat.ARGB32);
            cubemapRenderTexture.antiAliasing = (int)antiAliasing;
            cubemapRenderTexture.Create();
            if (usingGpuTransform)
            {
                convertPanoramaKernelIdx = convertPanoramaShader.FindKernel("CubeMapToEquirectangular");
                convertPanoramaYPositiveKernelIdx = convertPanoramaShader.FindKernel("CubeMapToEquirectangularPositiveY");
                convertPanoramaYNegativeKernelIdx = convertPanoramaShader.FindKernel("CubeMapToEquirectangularNegativeY");
                convertPanoramaKernelIdxs = new int[3] { convertPanoramaKernelIdx, convertPanoramaYPositiveKernelIdx, convertPanoramaYNegativeKernelIdx };
                convertPanoramaShader.SetInt("equirectangularWidth", panoramaWidth);
                convertPanoramaShader.SetInt("equirectangularHeight", panoramaHeight);
                convertPanoramaShader.SetInt("ssaaFactor", ssaaFactor);
                convertPanoramaShader.SetInt("cameraWidth", cameraWidth);
                convertPanoramaShader.SetInt("cameraHeight", cameraHeight);
                var num4 = (panoramaHeight + 8 - 1) / 8;
                var num5 = panoramaWidth;
                var num6 = captureStereoscopic ? 2 * panoramaHeight : num4;
                resultPixels = new uint[num5 * num6 + 1];
            }

            textureToBufferIdx = textureToBufferShader.FindKernel("TextureToBuffer");
            textureToBufferShader.SetInt("width", cameraWidth);
            textureToBufferShader.SetInt("height", cameraHeight);
            textureToBufferShader.SetFloat("gamma", QualitySettings.activeColorSpace == ColorSpace.Linear ? 0.45454544f : 1f);
            renderStereoIdx = convertPanoramaStereoShader.FindKernel("RenderStereo");
            if ((saveCubemap || !usingGpuTransform) && (cameraPixels == null || cameraPixels.Length != numCameras * cameraWidth * cameraHeight))
                cameraPixels = new uint[numCameras * cameraWidth * cameraHeight + 1];
            tanHalfHFov = Mathf.Tan(hFov * ((float)Math.PI * 2f) / 360f / 2f);
            tanHalfVFov = Mathf.Tan(vFov * ((float)Math.PI * 2f) / 360f / 2f);
            hFovAdjust = hFovAdjustDegrees * ((float)Math.PI * 2f) / 360f;
            vFovAdjust = vFovAdjustDegrees * ((float)Math.PI * 2f) / 360f;
            if (captureStereoscopic && usingGpuTransform)
            {
                convertPanoramaStereoShader.SetFloat("tanHalfHFov", tanHalfHFov);
                convertPanoramaStereoShader.SetFloat("tanHalfVFov", tanHalfVFov);
                convertPanoramaStereoShader.SetFloat("hFovAdjust", hFovAdjust);
                convertPanoramaStereoShader.SetFloat("vFovAdjust", vFovAdjust);
                convertPanoramaStereoShader.SetFloat("interpupillaryDistance", interpupillaryDistance);
                convertPanoramaStereoShader.SetFloat("circleRadius", circleRadius);
                convertPanoramaStereoShader.SetInt("numCirclePoints", numCirclePoints);
                convertPanoramaStereoShader.SetInt("equirectangularWidth", panoramaWidth);
                convertPanoramaStereoShader.SetInt("equirectangularHeight", panoramaHeight);
                convertPanoramaStereoShader.SetInt("cameraWidth", cameraWidth);
                convertPanoramaStereoShader.SetInt("cameraHeight", cameraHeight);
                convertPanoramaStereoShader.SetInt("ssaaFactor", ssaaFactor);
            }

            initializeFailed = false;
        }

        private void Log(string s)
        {
            VRLog.Info(s);
            if (enableDebugging) UnityEngine.Debug.Log(s, this);
        }

        protected override void OnUpdate()
        {
            var keyDown = Input.GetKeyDown(captureKey);
            if (initializeFailed || panoramaWidth < 4 || (captureStereoscopic && numCirclePoints < 8))
            {
                if (keyDown)
                {
                    if (panoramaWidth < 4) UnityEngine.Debug.LogError("Panorama Width must be at least 4. No panorama captured.");
                    if (captureStereoscopic && numCirclePoints < 8) UnityEngine.Debug.LogError("Num Circle Points must be at least 8. No panorama captured.");
                    if (initializeFailed) UnityEngine.Debug.LogError("Initialization of Capture Panorama script failed. Cannot capture content.");
                    if (failSound != null && Camera.main != null) audioSource.PlayOneShot(failSound);
                }

                return;
            }

            if (captureStereoscopic != lastConfiguredCaptureStereoscopic || panoramaWidth != lastConfiguredPanoramaWidth || interpupillaryDistance != lastConfiguredInterpupillaryDistance ||
                numCirclePoints != lastConfiguredNumCirclePoints || ssaaFactor != lastConfiguredSsaaFactor || antiAliasing != lastConfiguredAntiAliasing || saveCubemap != lastConfiguredSaveCubemap ||
                useGpuTransform != lastConfiguredUseGpuTransform) Reinitialize();
            if (capturingEveryFrame)
            {
                if ((captureKey != KeyCode.None && keyDown) || (maxFramesToRecord > 0 && frameNumber >= maxFramesToRecord))
                {
                    StopCaptureEveryFrame();
                    return;
                }

                CaptureScreenshotSync(videoBaseName + "_" + frameNumber.ToString(new string('0', frameNumberDigits)));
                frameNumber++;
            }
            else if (captureKey != KeyCode.None && keyDown && !Capturing)
            {
                if (captureEveryFrame)
                {
                    StartCaptureEveryFrame();
                    return;
                }

                var text = $"{panoramaName}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}";
                Log("Panorama capture key pressed, capturing " + text);
                CaptureScreenshotAsync(text);
            }
        }

        public void StartCaptureEveryFrame()
        {
            Time.captureFramerate = frameRate;
            videoBaseName = $"{panoramaName}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}";
            frameNumber = 0;
            capturingEveryFrame = true;
        }

        public void StopCaptureEveryFrame()
        {
            Time.captureFramerate = 0;
            capturingEveryFrame = false;
        }

        public void CaptureScreenshotSync(string filenameBase)
        {
            var enumerator = CaptureScreenshotAsyncHelper(filenameBase, false);
            while (enumerator.MoveNext()) { }
        }

        public void CaptureScreenshotAsync(string filenameBase)
        {
            StartCoroutine(CaptureScreenshotAsyncHelper(filenameBase, true));
        }

        private void SetFadersEnabled(IEnumerable<ScreenFadeControl> fadeControls, bool value)
        {
            foreach (var fadeControl in fadeControls) fadeControl.enabled = value;
        }

        public IEnumerator FadeOut(IEnumerable<ScreenFadeControl> fadeControls)
        {
            Log("Doing fade out");
            var elapsedTime = 0f;
            var color = fadeColor;
            color.a = 0f;
            fadeMaterial.color = color;
            SetFadersEnabled(fadeControls, true);
            while (elapsedTime < fadeTime)
            {
                yield return new WaitForEndOfFrame();
                elapsedTime += Time.deltaTime;
                color.a = Mathf.Clamp01(elapsedTime / fadeTime);
                fadeMaterial.color = color;
            }
        }

        public IEnumerator FadeIn(IEnumerable<ScreenFadeControl> fadeControls)
        {
            Log("Fading back in");
            var elapsedTime = 0f;
            var color3 = fadeMaterial.color = fadeColor;
            var color = color3;
            while (elapsedTime < fadeTime)
            {
                yield return new WaitForEndOfFrame();
                elapsedTime += Time.deltaTime;
                color.a = 1f - Mathf.Clamp01(elapsedTime / fadeTime);
                fadeMaterial.color = color;
            }

            SetFadersEnabled(fadeControls, false);
        }

        public IEnumerator CaptureScreenshotAsyncHelper(string filenameBase, bool async)
        {
            if (async)
            {
                while (Capturing) yield return null;
            }

            Capturing = true;
            if (!OnCaptureStart())
            {
                audioSource.PlayOneShot(failSound);
                Capturing = false;
                yield break;
            }

            var cameras = GetCaptureCameras();
            Array.Sort(cameras, (Camera x, Camera y) => x.depth.CompareTo(y.depth));
            if (cameras.Length == 0)
            {
                UnityEngine.Debug.LogWarning("No cameras found to capture");
                audioSource.PlayOneShot(failSound);
                Capturing = false;
                yield break;
            }

            Camera[] array;
            if (antiAliasing != AntiAliasing._1)
            {
                array = cameras;
                foreach (var camera in array)
                {
                    if (camera.actualRenderingPath == RenderingPath.DeferredLighting || camera.actualRenderingPath == RenderingPath.DeferredShading)
                    {
                        UnityEngine.Debug.LogWarning("CapturePanorama: Setting Anti Aliasing=1 because at least one camera in deferred mode. Use SSAA setting or Antialiasing image effect if needed.");
                        antiAliasing = AntiAliasing._1;
                        Reinitialize();
                        break;
                    }
                }
            }

            Log("Starting panorama capture");
            if (!captureEveryFrame && startSound != null && Camera.main != null) audioSource.PlayOneShot(startSound);
            var fadeControls = new List<ScreenFadeControl>();
            array = Camera.allCameras;
            foreach (var camera2 in array)
            {
                if (camera2.isActiveAndEnabled && camera2.targetTexture == null)
                {
                    var screenFadeControl = camera2.gameObject.AddComponent<ScreenFadeControl>();
                    screenFadeControl.fadeMaterial = fadeMaterial;
                    fadeControls.Add(screenFadeControl);
                }
            }

            SetFadersEnabled(fadeControls, false);
            if (fadeDuringCapture && async) yield return StartCoroutine(FadeOut(fadeControls));
            for (var j = 0; j < 2; j++) yield return new WaitForEndOfFrame();
            ComputeBuffer convertPanoramaResultBuffer = null;
            ComputeBuffer forceWaitResultConvertPanoramaStereoBuffer = null;
            if (usingGpuTransform)
            {
                if (captureStereoscopic)
                {
                    convertPanoramaResultBuffer = new ComputeBuffer(panoramaWidth * panoramaHeight * 2 + 1, 4);
                    convertPanoramaStereoShader.SetBuffer(renderStereoIdx, "result", convertPanoramaResultBuffer);
                    forceWaitResultConvertPanoramaStereoBuffer = new ComputeBuffer(1, 4);
                    convertPanoramaStereoShader.SetBuffer(renderStereoIdx, "forceWaitResultBuffer", forceWaitResultConvertPanoramaStereoBuffer);
                }
                else
                {
                    var num = (panoramaHeight + 8 - 1) / 8;
                    convertPanoramaResultBuffer = new ComputeBuffer(panoramaWidth * num + 1, 4);
                    var array2 = convertPanoramaKernelIdxs;
                    foreach (var kernelIndex in array2) convertPanoramaShader.SetBuffer(kernelIndex, "result", convertPanoramaResultBuffer);
                }
            }

            var num2 = numCameras;
            overlapTextures = 0;
            var num3 = 0;
            if (captureStereoscopic && usingGpuTransform)
            {
                overlapTextures = ssaaFactor == 1 ? 1 : 2;
                num3 = 1 + overlapTextures;
                num2 = Math.Min(numCameras, 2 + 2 * num3);
            }

            var cameraPixelsBuffer = new ComputeBuffer(num2 * cameraWidth * cameraHeight + 1, 4);
            textureToBufferShader.SetBuffer(textureToBufferIdx, "result", cameraPixelsBuffer);
            textureToBufferShader.SetInt("sentinelIdx", cameraPixelsBuffer.count - 1);
            if (usingGpuTransform && !captureStereoscopic)
            {
                convertPanoramaShader.SetInt("cameraPixelsSentinelIdx", cameraPixelsBuffer.count - 1);
                convertPanoramaShader.SetInt("sentinelIdx", convertPanoramaResultBuffer.count - 1);
                var array2 = convertPanoramaKernelIdxs;
                foreach (var kernelIndex2 in array2) convertPanoramaShader.SetBuffer(kernelIndex2, "cameraPixels", cameraPixelsBuffer);
            }

            if (usingGpuTransform && captureStereoscopic)
            {
                convertPanoramaStereoShader.SetInt("cameraPixelsSentinelIdx", cameraPixelsBuffer.count - 1);
                convertPanoramaStereoShader.SetBuffer(renderStereoIdx, "cameraPixels", cameraPixelsBuffer);
            }

            var forceWaitResultTextureToBufferBuffer = new ComputeBuffer(1, 4);
            textureToBufferShader.SetBuffer(textureToBufferIdx, "forceWaitResultBuffer", forceWaitResultTextureToBufferBuffer);
            var startTime = Time.realtimeSinceStartup;
            var identity = Quaternion.identity;
            Log("Rendering camera views");
            array = cameras;
            foreach (var camera3 in array) Log("Camera name: " + camera3.gameObject.name);
            var dictionary = new Dictionary<Camera, List<ImageEffectCopyCamera.InstanceMethodPair>>();
            array = cameras;
            foreach (var camera4 in array) dictionary[camera4] = ImageEffectCopyCamera.GenerateMethodList(camera4);
            var suffix = "." + FormatToExtension(imageFormat);
            var filePath = "";
            var imagePath = saveImagePath;
            if (imagePath == null || imagePath == "") imagePath = Application.dataPath + "/..";
            convertPanoramaStereoShader.SetInt("circlePointCircularBufferSize", num3);
            var num4 = 0;
            var num5 = 0;
            var num6 = 0;
            var num7 = usingGpuTransform ? numCameras + overlapTextures * 4 : numCameras;
            var num8 = (num7 - 2) / 2 + 2;
            var num9 = 0;
            var num10 = 0;
            Log("Changing quality level");
            var qualityLevel = QualitySettings.GetQualityLevel();
            var flag = false;
            var names = QualitySettings.names;
            if (qualitySetting != names[qualityLevel])
            {
                for (var l = 0; l < names.Length; l++)
                {
                    if (names[l] == qualitySetting)
                    {
                        QualitySettings.SetQualityLevel(l, false);
                        flag = true;
                    }
                }

                if (qualitySetting != "" && !flag) UnityEngine.Debug.LogError("Quality setting specified for CapturePanorama is invalid, ignoring.", this);
            }

            BeforeRenderPanorama();
            RenderTexture.active = null;
            for (var m = 0; m < num7; m++)
            {
                if (captureStereoscopic)
                {
                    if (m < 2)
                    {
                        camGos[1].transform.localPosition = Vector3.zero;
                        camGos[1].transform.localRotation = Quaternion.Euler(m == 0 ? 90f : -90f, 0f, 0f);
                    }
                    else
                    {
                        int num11;
                        int num12;
                        if (m < num8)
                        {
                            num11 = m - 2;
                            num12 = 0;
                        }
                        else
                        {
                            num11 = m - num8;
                            num12 = 2;
                        }

                        var num13 = num11 / 2 % numCirclePoints;
                        var num14 = num11 % 2 + num12;
                        var num15 = 360f * (float)num13 / (float)numCirclePoints;
                        camGos[1].transform.localPosition = Quaternion.Euler(0f, num15, 0f) * Vector3.forward * circleRadius;
                        if (num14 < 2)
                            camGos[1].transform.localRotation = Quaternion.Euler(0f, num15 + (num14 == 0 ? 0f - hFovAdjustDegrees : hFovAdjustDegrees), 0f);
                        else
                            camGos[1].transform.localRotation = Quaternion.Euler(num14 == 2 ? 0f - vFovAdjustDegrees : vFovAdjustDegrees, num15, 0f);
                        if (num14 == 1 || num14 == 3) num9++;
                    }
                }
                else
                {
                    switch ((CubemapFace)m)
                    {
                        case CubemapFace.PositiveX:
                            camGos[1].transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                            break;
                        case CubemapFace.NegativeX:
                            camGos[1].transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
                            break;
                        case CubemapFace.PositiveY:
                            camGos[1].transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                            break;
                        case CubemapFace.NegativeY:
                            camGos[1].transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                            break;
                        case CubemapFace.PositiveZ:
                            camGos[1].transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                            break;
                        case CubemapFace.NegativeZ:
                            camGos[1].transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                            break;
                    }
                }

                array = cameras;
                foreach (var camera5 in array)
                {
                    camGos[2].transform.parent = null;
                    cam.CopyFrom(camera5);
                    camGos[0].transform.localPosition = cam.transform.localPosition;
                    camGos[0].transform.localRotation = cam.transform.localRotation;
                    camGos[2].transform.parent = camGos[1].transform;
                    cam.transform.localPosition = Vector3.zero;
                    cam.transform.localRotation = Quaternion.identity;
                    copyCameraScript.enabled = dictionary[camera5].Count > 0;
                    copyCameraScript.onRenderImageMethods = dictionary[camera5];
                    cam.fieldOfView = vFov;
                    camGos[0].transform.rotation *= Quaternion.Inverse(identity);
                    if (useDefaultOrientation) camGos[0].transform.rotation = Quaternion.identity;
                    cam.targetTexture = cubemapRenderTexture;
                    cam.ResetAspect();
                    var position = camera5.transform.position;
                    var rotation = camera5.transform.rotation;
                    var fieldOfView = camera5.fieldOfView;
                    var targetTexture = camera5.targetTexture;
                    camera5.transform.position = cam.transform.position;
                    camera5.transform.rotation = cam.transform.rotation;
                    camera5.fieldOfView = cam.fieldOfView;
                    cam.Render();
                    camera5.transform.position = position;
                    camera5.transform.rotation = rotation;
                    camera5.fieldOfView = fieldOfView;
                    camera5.targetTexture = targetTexture;
                }

                RenderTexture.active = cubemapRenderTexture;
                forceWaitTexture.ReadPixels(new Rect(cameraWidth - 1, cameraHeight - 1, 1f, 1f), 0, 0);
                var num16 = 1000000 + m;
                textureToBufferShader.SetInt("forceWaitValue", num16);
                textureToBufferShader.SetTexture(textureToBufferIdx, "source", cubemapRenderTexture);
                textureToBufferShader.SetInt("startIdx", num6 * cameraWidth * cameraHeight);
                textureToBufferShader.Dispatch(textureToBufferIdx, (cameraWidth + threadsX - 1) / threadsX, (cameraHeight + threadsY - 1) / threadsY, 1);
                var array3 = new uint[1];
                forceWaitResultTextureToBufferBuffer.GetData(array3);
                if (array3[0] != num16) UnityEngine.Debug.LogError("TextureToBufferShader: Unexpected forceWaitResult value " + array3[0] + ", should be " + num16);
                if (saveCubemap && (m < 2 || (m >= 2 && m < 2 + numCirclePoints * 2) || (m >= num8 && m < num8 + numCirclePoints * 2)))
                {
                    cameraPixelsBuffer.GetData(cameraPixels);
                    if (cameraPixels[cameraPixelsBuffer.count - 1] != 1419455993) ReportOutOfGraphicsMemory();
                    SaveCubemapImage(cameraPixels, filenameBase, suffix, imagePath, num10, num6);
                    num10++;
                }

                num6++;
                if (num6 >= num2) num6 = 2;
                if (captureStereoscopic && usingGpuTransform && (m - 2 + 1) % 2 == 0 && (num9 - num5 >= num3 || m + 1 == 2 + (num7 - 2) / 2 || m + 1 == num7))
                {
                    num16 = 2000000 + m;
                    convertPanoramaStereoShader.SetInt("forceWaitValue", num16);
                    convertPanoramaStereoShader.SetInt("leftRightPass", m < num8 ? 1 : 0);
                    convertPanoramaStereoShader.SetInt("circlePointStart", num5);
                    convertPanoramaStereoShader.SetInt("circlePointEnd", num2 < numCameras ? num9 : num9 + 1);
                    convertPanoramaStereoShader.SetInt("circlePointCircularBufferStart", num4);
                    convertPanoramaStereoShader.Dispatch(renderStereoIdx, (panoramaWidth + threadsX - 1) / threadsX, (panoramaHeight + threadsY - 1) / threadsY, 2);
                    forceWaitResultConvertPanoramaStereoBuffer.GetData(array3);
                    if (array3[0] != num16) UnityEngine.Debug.LogError("ConvertPanoramaStereoShader: Unexpected forceWaitResult value " + array3[0] + ", should be " + num16);
                    if (m + 1 == num8)
                    {
                        num4 = (num4 + num3) % num3;
                        num5 = 0;
                        num9 = 0;
                    }
                    else
                    {
                        num5 = num9 - overlapTextures;
                        num4 = (num4 + num3 - overlapTextures) % num3;
                    }
                }

                RenderTexture.active = null;
            }

            AfterRenderPanorama();
            Log("Resetting quality level");
            if (flag) QualitySettings.SetQualityLevel(qualityLevel, false);
            if (saveCubemap || !usingGpuTransform)
            {
                cameraPixelsBuffer.GetData(cameraPixels);
                if (cameraPixels[cameraPixelsBuffer.count - 1] != 1419455993) ReportOutOfGraphicsMemory();
            }

            RenderTexture.active = null;
            if (saveCubemap && (!captureStereoscopic || !usingGpuTransform))
            {
                for (var n = 0; n < numCameras; n++)
                {
                    var bufferIdx = n;
                    SaveCubemapImage(cameraPixels, filenameBase, suffix, imagePath, n, bufferIdx);
                }
            }

            for (var j = 0; j < 2; j++) yield return new WaitForEndOfFrame();
            if (async && !usingGpuTransform && fadeDuringCapture) yield return StartCoroutine(FadeIn(fadeControls));
            filePath = imagePath + "/" + filenameBase + suffix;
            var bitmap = new Bitmap(panoramaWidth, panoramaHeight * (!captureStereoscopic ? 1 : 2), PixelFormat.Format32bppArgb);
            var bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
            var ptr = bmpData.Scan0;
            var pixelValues = new byte[Math.Abs(bmpData.Stride) * bitmap.Height];
            if (async)
                yield return StartCoroutine(CubemapToEquirectangular(cameraPixelsBuffer, cameraPixels, convertPanoramaResultBuffer, cameraWidth, cameraHeight, pixelValues, bmpData.Stride, panoramaWidth,
                                                                     panoramaHeight, ssaaFactor, async));
            else
            {
                var enumerator = CubemapToEquirectangular(cameraPixelsBuffer, cameraPixels, convertPanoramaResultBuffer, cameraWidth, cameraHeight, pixelValues, bmpData.Stride, panoramaWidth, panoramaHeight,
                                                          ssaaFactor, async);
                while (enumerator.MoveNext()) { }
            }

            var producedImageSuccess = pixelValues[3] == byte.MaxValue;
            yield return null;
            Marshal.Copy(pixelValues, 0, ptr, pixelValues.Length);
            bitmap.UnlockBits(bmpData);
            yield return null;
            Log("Time to take panorama screenshot: " + (Time.realtimeSinceStartup - startTime) + " sec");
            if (producedImageSuccess)
            {
                var thread = new Thread((ThreadStart)delegate
                {
                    Log("Saving equirectangular image");
                    bitmap.Save(filePath, FormatToDrawingFormat(imageFormat));
                });
                thread.Start();
                while (thread.ThreadState == System.Threading.ThreadState.Running)
                {
                    if (async)
                        yield return null;
                    else
                        Thread.Sleep(0);
                }
            }

            bitmap.Dispose();
            var array4 = new ComputeBuffer[4] { convertPanoramaResultBuffer, cameraPixelsBuffer, forceWaitResultConvertPanoramaStereoBuffer, forceWaitResultTextureToBufferBuffer };
            for (var k = 0; k < array4.Length; k++) array4[k]?.Release();
            if (async && usingGpuTransform && fadeDuringCapture) yield return StartCoroutine(FadeIn(fadeControls));
            foreach (var item in fadeControls) Destroy(item);
            fadeControls.Clear();
            if (producedImageSuccess && uploadImages && !captureEveryFrame)
            {
                Log("Uploading image");
                imageFileBytes = File.ReadAllBytes(filePath);
                var mimeType = FormatMimeType(imageFormat);
                if (async)
                {
                    yield return StartCoroutine(UploadImage(imageFileBytes, filenameBase + suffix, mimeType, async));
                    yield break;
                }

                var enumerator3 = UploadImage(imageFileBytes, filenameBase + suffix, mimeType, async);
                while (enumerator3.MoveNext()) { }

                yield break;
            }

            if (!producedImageSuccess)
            {
                if (failSound != null && Camera.main != null) audioSource.PlayOneShot(failSound);
            }
            else if (!captureEveryFrame && doneSound != null && Camera.main != null) audioSource.PlayOneShot(doneSound);

            Capturing = false;
        }

        public virtual bool OnCaptureStart()
        {
            return true;
        }

        public virtual Camera[] GetCaptureCameras()
        {
            var allCameras = Camera.allCameras;
            var list = new List<Camera>();
            var array = allCameras;
            foreach (var camera in array)
            {
                VRLog.Info("Camera found: " + camera.name);
                list.Add(camera);
            }

            return list.ToArray();
        }

        public virtual void BeforeRenderPanorama() { }

        public virtual void AfterRenderPanorama() { }

        private static void ReportOutOfGraphicsMemory()
        {
            throw new OutOfMemoryException(
                "Exhausted graphics memory while capturing panorama. Lower Panorama Width, increase Num Circle Points for stereoscopic images, disable Anti Aliasing, or disable Stereoscopic Capture.");
        }

        private void SaveCubemapImage(uint[] cameraPixels, string filenameBase, string suffix, string imagePath, int i, int bufferIdx)
        {
            var bitmap = new Bitmap(cameraWidth, cameraHeight, PixelFormat.Format32bppArgb);
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
            var scan = bitmapData.Scan0;
            var array = new byte[Math.Abs(bitmapData.Stride) * bitmap.Height];
            var stride = bitmapData.Stride;
            var height = bitmapData.Height;
            var num = bufferIdx * cameraWidth * cameraHeight;
            for (var j = 0; j < cameraHeight; j++)
            {
                var num2 = stride * (height - 1 - j);
                for (var k = 0; k < cameraWidth; k++)
                {
                    var num3 = cameraPixels[num];
                    array[num2] = (byte)(num3 & 0xFFu);
                    array[num2 + 1] = (byte)((num3 >> 8) & 0xFFu);
                    array[num2 + 2] = (byte)(num3 >> 16);
                    array[num2 + 3] = byte.MaxValue;
                    num2 += 4;
                    num++;
                }
            }

            Marshal.Copy(array, 0, scan, array.Length);
            bitmap.UnlockBits(bitmapData);
            string text;
            if (captureStereoscopic)
            {
                text = i.ToString();
                Log("Saving lightfield camera image number " + text);
            }
            else
            {
                var cubemapFace = (CubemapFace)i;
                text = cubemapFace.ToString();
                Log("Saving cubemap image " + text);
            }

            var filename = imagePath + "/" + filenameBase + "_" + text + suffix;
            bitmap.Save(filename, FormatToDrawingFormat(imageFormat));
            bitmap.Dispose();
        }

        private Color32 GetCameraPixelBilinear(uint[] cameraPixels, int cameraNum, float u, float v)
        {
            u *= (float)cameraWidth;
            v *= (float)cameraHeight;
            var num = (int)Math.Floor(u);
            var num2 = Math.Min(cameraWidth - 1, num + 1);
            var num3 = (int)Math.Floor(v);
            var num4 = Math.Min(cameraHeight - 1, num3 + 1);
            var t = u - (float)num;
            var t2 = v - (float)num3;
            var num5 = cameraNum * cameraWidth * cameraHeight;
            var num6 = num5 + num3 * cameraWidth;
            var num7 = num5 + num4 * cameraWidth;
            var num8 = cameraPixels[num6 + num];
            var num9 = cameraPixels[num6 + num2];
            var num10 = cameraPixels[num7 + num];
            var num11 = cameraPixels[num7 + num2];
            var num12 = Mathf.Lerp(Mathf.Lerp(num8 >> 16, num10 >> 16, t2), Mathf.Lerp(num9 >> 16, num11 >> 16, t2), t);
            var num13 = Mathf.Lerp(Mathf.Lerp((num8 >> 8) & 0xFFu, (num10 >> 8) & 0xFFu, t2), Mathf.Lerp((num9 >> 8) & 0xFFu, (num11 >> 8) & 0xFFu, t2), t);
            var num14 = Mathf.Lerp(Mathf.Lerp(num8 & 0xFFu, num10 & 0xFFu, t2), Mathf.Lerp(num9 & 0xFFu, num11 & 0xFFu, t2), t);
            return new UnityEngine.Color(num12 / 255f, num13 / 255f, num14 / 255f, 1f);
        }

        internal void ClearProcessQueue()
        {
            while (resizingProcessList.Count > 0)
            {
                resizingProcessList[0].WaitForExit();
                File.Delete(resizingFilenames[0]);
                resizingProcessList.RemoveAt(0);
                resizingFilenames.RemoveAt(0);
            }
        }

        private IEnumerator UploadImage(byte[] imageFileBytes, string filename, string mimeType, bool async)
        {
            var startTime = Time.realtimeSinceStartup;
            var wWWForm = new WWWForm();
            wWWForm.AddField("key", apiKey);
            wWWForm.AddField("action", "upload");
            wWWForm.AddBinaryData("source", imageFileBytes, filename, mimeType);
            var w = new WWW(apiUrl + "upload", wWWForm);
            yield return w;
            if (!string.IsNullOrEmpty(w.error))
            {
                UnityEngine.Debug.LogError("Panorama upload failed: " + w.error, this);
                if (failSound != null && Camera.main != null) audioSource.PlayOneShot(failSound);
            }
            else
            {
                Log("Time to upload panorama screenshot: " + (Time.realtimeSinceStartup - startTime) + " sec");
                if (!captureEveryFrame && doneSound != null && Camera.main != null) audioSource.PlayOneShot(doneSound);
            }

            Capturing = false;
        }

        private IEnumerator CubemapToEquirectangular(
            ComputeBuffer cameraPixelsBuffer,
            uint[] cameraPixels,
            ComputeBuffer convertPanoramaResultBuffer,
            int cameraWidth,
            int cameraHeight,
            byte[] pixelValues,
            int stride,
            int panoramaWidth,
            int panoramaHeight,
            int ssaaFactor,
            bool async)
        {
            if (captureStereoscopic && usingGpuTransform)
            {
                convertPanoramaResultBuffer.GetData(resultPixels);
                if (resultPixels[convertPanoramaResultBuffer.count - 1] != 1419455993) ReportOutOfGraphicsMemory();
                writeOutputPixels(pixelValues, stride, panoramaWidth, panoramaHeight * 2, panoramaHeight * 2, 0);
            }
            else if (captureStereoscopic && !usingGpuTransform)
            {
                var realtimeSinceStartup = Time.realtimeSinceStartup;
                var processingTimePerFrame = cpuMillisecondsPerFrame / 1000f;
                for (var y = 0; y < panoramaHeight; y++)
                {
                    for (var x = 0; x < panoramaWidth; x++)
                    {
                        var num = (float)x / (float)panoramaWidth;
                        var num2 = ((float)y / (float)panoramaHeight - 0.5f) * (float)Math.PI;
                        var num3 = Mathf.Sin(num2);
                        var num4 = Mathf.Cos(num2);
                        var num5 = (num * 2f - 1f) * (float)Math.PI;
                        var num6 = Mathf.Sin(num5);
                        var num7 = Mathf.Cos(num5);
                        var latitudeNormalized = num2 / ((float)Math.PI / 2f);
                        var num8 = IpdScaleFunction(latitudeNormalized);
                        var num9 = num8 * interpupillaryDistance / 2f;
                        var num10 = 1f - num8 * 5f;
                        var b = new UnityEngine.Color(0f, 0f, 0f, 0f);
                        if (num10 > 0f)
                        {
                            var vector = new Vector3(num4 * num6, num3, num4 * num7);
                            var num11 = 1f / vector.y;
                            var num12 = vector.x * num11;
                            var num13 = vector.z * num11;
                            if (num12 * num12 <= 1f && num13 * num13 <= 1f)
                            {
                                int cameraNum;
                                if (vector.y > 0f)
                                    cameraNum = 0;
                                else
                                {
                                    num12 = 0f - num12;
                                    cameraNum = 1;
                                }

                                num12 = (num12 + 1f) * 0.5f;
                                num13 = (num13 + 1f) * 0.5f;
                                b = GetCameraPixelBilinear(cameraPixels, cameraNum, num12, num13);
                            }
                        }

                        for (var i = 0; i < 2; i++)
                        {
                            var vector2 = new Vector3(num6, 0f, num7);
                            var num14 = (float)Math.PI / 2f - Mathf.Acos(num9 / circleRadius);
                            if (i == 0) num14 = 0f - num14;
                            var num15 = num5 + num14;
                            if (num15 < 0f) num15 += (float)Math.PI * 2f;
                            if (num15 >= (float)Math.PI * 2f) num15 -= (float)Math.PI * 2f;
                            var num16 = num15 / ((float)Math.PI * 2f) * (float)numCirclePoints;
                            var num17 = (int)Mathf.Floor(num16) % numCirclePoints;
                            var a = default(UnityEngine.Color);
                            var b2 = default(UnityEngine.Color);
                            for (var j = 0; j < 2; j++)
                            {
                                var num18 = j == 0 ? num17 : (num17 + 1) % numCirclePoints;
                                var f = (float)Math.PI * 2f * (float)num18 / (float)numCirclePoints;
                                var num19 = Mathf.Sin(f);
                                var num20 = Mathf.Cos(f);
                                var num21 = Mathf.Sign(vector2.x * num20 - vector2.z * num19) * Mathf.Acos(vector2.z * num20 + vector2.x * num19);
                                var num22 = Mathf.Cos(num21);
                                var num23 = Mathf.Sin(num21);
                                var cameraNum = 2 + num18 * 2 + (num21 >= 0f ? 1 : 0);
                                var num24 = num21 >= 0f ? 0f - hFovAdjust : hFovAdjust;
                                var f2 = num21 + num24;
                                var vector3 = new Vector3(num4 * Mathf.Sin(f2), num3, num4 * Mathf.Cos(f2));
                                var num12 = vector3.x / vector3.z / tanHalfHFov;
                                var num13 = (0f - vector3.y) / vector3.z / tanHalfVFov;
                                if (!(vector3.z > 0f) || !(num12 * num12 <= 1f) || !(num13 * num13 <= 0.9f))
                                {
                                    cameraNum = 2 + numCirclePoints * 2 + num18 * 2 + (num2 >= 0f ? 1 : 0);
                                    var f3 = num2 >= 0f ? vFovAdjust : 0f - vFovAdjust;
                                    var num25 = Mathf.Cos(f3);
                                    var num26 = Mathf.Sin(f3);
                                    vector3 = new Vector3(num4 * num23, num25 * num3 - num4 * num22 * num26, num26 * num3 + num4 * num22 * num25);
                                    num12 = vector3.x / vector3.z / tanHalfHFov;
                                    num13 = (0f - vector3.y) / vector3.z / tanHalfVFov;
                                }

                                num12 = (num12 + 1f) * 0.5f;
                                num13 = (num13 + 1f) * 0.5f;
                                UnityEngine.Color color = GetCameraPixelBilinear(cameraPixels, cameraNum, num12, num13);
                                if (j == 0)
                                    a = color;
                                else
                                    b2 = color;
                            }

                            Color32 color2 = UnityEngine.Color.Lerp(a, b2, num16 - Mathf.Floor(num16));
                            if (b.a > 0f && num10 > 0f) color2 = UnityEngine.Color.Lerp(color2, b, num10);
                            var num27 = stride * (y + panoramaHeight * i) + x * 4;
                            pixelValues[num27] = color2.b;
                            pixelValues[num27 + 1] = color2.g;
                            pixelValues[num27 + 2] = color2.r;
                            pixelValues[num27 + 3] = byte.MaxValue;
                        }

                        if ((x & 0xFF) == 0 && Time.realtimeSinceStartup - realtimeSinceStartup > processingTimePerFrame)
                        {
                            yield return null;
                            realtimeSinceStartup = Time.realtimeSinceStartup;
                        }
                    }
                }
            }
            else if (!captureStereoscopic && usingGpuTransform)
            {
                var num28 = (panoramaHeight + 8 - 1) / 8;
                Log("Invoking GPU shader for equirectangular reprojection");
                var num29 = (int)Mathf.Floor((float)panoramaHeight * 0.25f);
                var num30 = (int)Mathf.Ceil((float)panoramaHeight * 0.75f);
                for (var k = 0; k < 8; k++)
                {
                    var num31 = k * num28;
                    var num32 = Math.Min(num31 + num28, panoramaHeight);
                    convertPanoramaShader.SetInt("startY", k * num28);
                    convertPanoramaShader.SetInt("sliceHeight", num32 - num31);
                    if (num32 <= num29)
                        convertPanoramaShader.Dispatch(convertPanoramaYNegativeKernelIdx, (panoramaWidth + threadsX - 1) / threadsX, (num28 + threadsY - 1) / threadsY, 1);
                    else if (num31 >= num30)
                        convertPanoramaShader.Dispatch(convertPanoramaYPositiveKernelIdx, (panoramaWidth + threadsX - 1) / threadsX, (num28 + threadsY - 1) / threadsY, 1);
                    else
                        convertPanoramaShader.Dispatch(convertPanoramaKernelIdx, (panoramaWidth + threadsX - 1) / threadsX, (panoramaHeight + threadsY - 1) / threadsY, 1);
                    convertPanoramaResultBuffer.GetData(resultPixels);
                    if (resultPixels[convertPanoramaResultBuffer.count - 1] != 1419455993) ReportOutOfGraphicsMemory();
                    writeOutputPixels(pixelValues, stride, panoramaWidth, num28, panoramaHeight, num31);
                }
            }
            else if (async)
                yield return StartCoroutine(CubemapToEquirectangularCpu(cameraPixels, cameraWidth, cameraHeight, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, async));
            else
            {
                var enumerator = CubemapToEquirectangularCpu(cameraPixels, cameraWidth, cameraHeight, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, async);
                while (enumerator.MoveNext()) { }
            }
        }

        private void writeOutputPixels(byte[] pixelValues, int stride, int bitmapWidth, int inHeight, int outHeight, int yStart)
        {
            var num = 0;
            for (var i = yStart; i < yStart + inHeight && i < outHeight; i++)
            {
                var num2 = stride * i;
                for (var j = 0; j < bitmapWidth; j++)
                {
                    var num3 = resultPixels[num];
                    pixelValues[num2] = (byte)(num3 & 0xFFu);
                    pixelValues[num2 + 1] = (byte)((num3 >> 8) & 0xFFu);
                    pixelValues[num2 + 2] = (byte)((num3 >> 16) & 0xFFu);
                    pixelValues[num2 + 3] = byte.MaxValue;
                    num2 += 4;
                    num++;
                }
            }
        }

        private IEnumerator CubemapToEquirectangularCpu(uint[] cameraPixels, int cameraWidth, int cameraHeight, byte[] pixelValues, int stride, int panoramaWidth, int panoramaHeight, int ssaaFactor, bool async)
        {
            Log("Converting to equirectangular");
            yield return null;
            var startTime = Time.realtimeSinceStartup;
            var processingTimePerFrame = cpuMillisecondsPerFrame / 1000f;
            var maxWidth = 1f - 1f / (float)cameraWidth;
            var maxHeight = 1f - 1f / (float)cameraHeight;
            var numPixelsAveraged = ssaaFactor * ssaaFactor;
            var endYPositive = (int)Mathf.Floor((float)panoramaHeight * 0.25f);
            var startYNegative = (int)Mathf.Ceil((float)panoramaHeight * 0.75f);
            var endTopMixedRegion = (int)Mathf.Ceil((float)panoramaHeight * 0.30408698f);
            var startBottomMixedRegion = (int)Mathf.Floor((float)panoramaHeight * 0.695913f);
            var startXNegative = (int)Mathf.Ceil((float)panoramaWidth * 1f / 8f);
            var endXNegative = (int)Mathf.Floor((float)panoramaWidth * 3f / 8f);
            var startZPositive = (int)Mathf.Ceil((float)panoramaWidth * 3f / 8f);
            var endZPositive = (int)Mathf.Floor((float)panoramaWidth * 5f / 8f);
            var startXPositive = (int)Mathf.Ceil((float)panoramaWidth * 5f / 8f);
            var endXPositive = (int)Mathf.Floor((float)panoramaWidth * 7f / 8f);
            var startZNegative = (int)Mathf.Ceil((float)panoramaWidth * 7f / 8f);
            var endZNegative = (int)Mathf.Floor((float)panoramaWidth * 1f / 8f);
            if (async)
            {
                yield return StartCoroutine(CubemapToEquirectangularCpuPositiveY(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame,
                                                                                 numPixelsAveraged, 0, 0, panoramaWidth, endYPositive));
                yield return StartCoroutine(CubemapToEquirectangularCpuNegativeY(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame,
                                                                                 numPixelsAveraged, 0, startYNegative, panoramaWidth, panoramaHeight));
                yield return StartCoroutine(CubemapToEquirectangularCpuPositiveX(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame,
                                                                                 numPixelsAveraged, startXPositive, endTopMixedRegion, endXPositive, startBottomMixedRegion));
                yield return StartCoroutine(CubemapToEquirectangularCpuNegativeX(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame,
                                                                                 numPixelsAveraged, startXNegative, endTopMixedRegion, endXNegative, startBottomMixedRegion));
                yield return StartCoroutine(CubemapToEquirectangularCpuPositiveZ(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame,
                                                                                 numPixelsAveraged, startZPositive, endTopMixedRegion, endZPositive, startBottomMixedRegion));
                yield return StartCoroutine(CubemapToEquirectangularCpuNegativeZ(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame,
                                                                                 numPixelsAveraged, startZNegative, endTopMixedRegion, panoramaWidth, startBottomMixedRegion));
                yield return StartCoroutine(CubemapToEquirectangularCpuNegativeZ(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame,
                                                                                 numPixelsAveraged, 0, endTopMixedRegion, endZNegative, startBottomMixedRegion));
                yield return StartCoroutine(CubemapToEquirectangularCpuGeneralCase(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, maxWidth,
                                                                                   maxHeight, numPixelsAveraged, 0, endYPositive, panoramaWidth, endTopMixedRegion));
                yield return StartCoroutine(CubemapToEquirectangularCpuGeneralCase(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, maxWidth,
                                                                                   maxHeight, numPixelsAveraged, 0, startBottomMixedRegion, panoramaWidth, startYNegative));
                if (endZNegative < startXNegative)
                    yield return StartCoroutine(CubemapToEquirectangularCpuGeneralCase(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, maxWidth,
                                                                                       maxHeight, numPixelsAveraged, endZNegative, endTopMixedRegion, startXNegative, startBottomMixedRegion));
                if (endXNegative < startZPositive)
                    yield return StartCoroutine(CubemapToEquirectangularCpuGeneralCase(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, maxWidth,
                                                                                       maxHeight, numPixelsAveraged, endXNegative, endTopMixedRegion, startZPositive, startBottomMixedRegion));
                if (endZPositive < startXPositive)
                    yield return StartCoroutine(CubemapToEquirectangularCpuGeneralCase(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, maxWidth,
                                                                                       maxHeight, numPixelsAveraged, endZPositive, endTopMixedRegion, startXPositive, startBottomMixedRegion));
                if (endXPositive < startZNegative)
                    yield return StartCoroutine(CubemapToEquirectangularCpuGeneralCase(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, maxWidth,
                                                                                       maxHeight, numPixelsAveraged, endXPositive, endTopMixedRegion, startZNegative, startBottomMixedRegion));
            }
            else
            {
                var enumerator = CubemapToEquirectangularCpuPositiveY(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, numPixelsAveraged, 0, 0,
                                                                      panoramaWidth, endYPositive);
                while (enumerator.MoveNext()) { }

                enumerator = CubemapToEquirectangularCpuNegativeY(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, numPixelsAveraged, 0,
                                                                  startYNegative, panoramaWidth, panoramaHeight);
                while (enumerator.MoveNext()) { }

                enumerator = CubemapToEquirectangularCpuPositiveX(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, numPixelsAveraged,
                                                                  startXPositive, endTopMixedRegion, endXPositive, startBottomMixedRegion);
                while (enumerator.MoveNext()) { }

                enumerator = CubemapToEquirectangularCpuNegativeX(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, numPixelsAveraged,
                                                                  startXNegative, endTopMixedRegion, endXNegative, startBottomMixedRegion);
                while (enumerator.MoveNext()) { }

                enumerator = CubemapToEquirectangularCpuPositiveZ(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, numPixelsAveraged,
                                                                  startZPositive, endTopMixedRegion, endZPositive, startBottomMixedRegion);
                while (enumerator.MoveNext()) { }

                enumerator = CubemapToEquirectangularCpuNegativeZ(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, numPixelsAveraged,
                                                                  startZNegative, endTopMixedRegion, panoramaWidth, startBottomMixedRegion);
                while (enumerator.MoveNext()) { }

                enumerator = CubemapToEquirectangularCpuNegativeZ(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, numPixelsAveraged, 0,
                                                                  endTopMixedRegion, endZNegative, startBottomMixedRegion);
                while (enumerator.MoveNext()) { }

                enumerator = CubemapToEquirectangularCpuGeneralCase(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, maxWidth, maxHeight,
                                                                    numPixelsAveraged, 0, endYPositive, panoramaWidth, endTopMixedRegion);
                while (enumerator.MoveNext()) { }

                enumerator = CubemapToEquirectangularCpuGeneralCase(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, maxWidth, maxHeight,
                                                                    numPixelsAveraged, 0, startBottomMixedRegion, panoramaWidth, startYNegative);
                while (enumerator.MoveNext()) { }

                if (endZNegative < startXNegative)
                {
                    enumerator = CubemapToEquirectangularCpuGeneralCase(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, maxWidth, maxHeight,
                                                                        numPixelsAveraged, endZNegative, endTopMixedRegion, startXNegative, startBottomMixedRegion);
                    while (enumerator.MoveNext()) { }
                }

                if (endXNegative < startZPositive)
                {
                    enumerator = CubemapToEquirectangularCpuGeneralCase(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, maxWidth, maxHeight,
                                                                        numPixelsAveraged, endXNegative, endTopMixedRegion, startZPositive, startBottomMixedRegion);
                    while (enumerator.MoveNext()) { }
                }

                if (endZPositive < startXPositive)
                {
                    enumerator = CubemapToEquirectangularCpuGeneralCase(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, maxWidth, maxHeight,
                                                                        numPixelsAveraged, endZPositive, endTopMixedRegion, startXPositive, startBottomMixedRegion);
                    while (enumerator.MoveNext()) { }
                }

                if (endXPositive < startZNegative)
                {
                    enumerator = CubemapToEquirectangularCpuGeneralCase(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, maxWidth, maxHeight,
                                                                        numPixelsAveraged, endXPositive, endTopMixedRegion, startZNegative, startBottomMixedRegion);
                    while (enumerator.MoveNext()) { }
                }
            }

            yield return null;
        }

        private IEnumerator CubemapToEquirectangularCpuPositiveY(
            uint[] cameraPixels,
            byte[] pixelValues,
            int stride,
            int panoramaWidth,
            int panoramaHeight,
            int ssaaFactor,
            float startTime,
            float processingTimePerFrame,
            int numPixelsAveraged,
            int startX,
            int startY,
            int endX,
            int endY)
        {
            for (var y = startY; y < endY; y++)
            {
                for (var x = startX; x < endX; x++)
                {
                    var num = 0;
                    var num2 = 0;
                    var num3 = 0;
                    var num4 = 0;
                    for (var i = y * ssaaFactor; i < (y + 1) * ssaaFactor; i++)
                    {
                        for (var j = x * ssaaFactor; j < (x + 1) * ssaaFactor; j++)
                        {
                            var num5 = (float)j / (float)(panoramaWidth * ssaaFactor);
                            var f = ((float)i / (float)(panoramaHeight * ssaaFactor) - 0.5f) * (float)Math.PI;
                            var f2 = (num5 * 2f - 1f) * (float)Math.PI;
                            var num6 = Mathf.Cos(f);
                            var vector = new Vector3(num6 * Mathf.Sin(f2), 0f - Mathf.Sin(f), num6 * Mathf.Cos(f2));
                            var num7 = 1f / vector.y;
                            var num8 = vector.x * num7;
                            var num9 = vector.z * num7;
                            num8 = (num8 + 1f) / 2f;
                            num9 = (num9 + 1f) / 2f;
                            var cameraPixelBilinear = GetCameraPixelBilinear(cameraPixels, 2, num8, num9);
                            num += cameraPixelBilinear.r;
                            num2 += cameraPixelBilinear.g;
                            num3 += cameraPixelBilinear.b;
                            num4 += cameraPixelBilinear.a;
                        }
                    }

                    var num10 = stride * (panoramaHeight - 1 - y) + x * 4;
                    pixelValues[num10] = (byte)(num3 / numPixelsAveraged);
                    pixelValues[num10 + 1] = (byte)(num2 / numPixelsAveraged);
                    pixelValues[num10 + 2] = (byte)(num / numPixelsAveraged);
                    pixelValues[num10 + 3] = (byte)(num4 / numPixelsAveraged);
                    if ((x & 0xFF) == 0 && Time.realtimeSinceStartup - startTime > processingTimePerFrame)
                    {
                        yield return null;
                        startTime = Time.realtimeSinceStartup;
                    }
                }
            }
        }

        private IEnumerator CubemapToEquirectangularCpuNegativeY(
            uint[] cameraPixels,
            byte[] pixelValues,
            int stride,
            int panoramaWidth,
            int panoramaHeight,
            int ssaaFactor,
            float startTime,
            float processingTimePerFrame,
            int numPixelsAveraged,
            int startX,
            int startY,
            int endX,
            int endY)
        {
            for (var y = startY; y < endY; y++)
            {
                for (var x = startX; x < endX; x++)
                {
                    var num = 0;
                    var num2 = 0;
                    var num3 = 0;
                    var num4 = 0;
                    for (var i = y * ssaaFactor; i < (y + 1) * ssaaFactor; i++)
                    {
                        for (var j = x * ssaaFactor; j < (x + 1) * ssaaFactor; j++)
                        {
                            var num5 = (float)j / (float)(panoramaWidth * ssaaFactor);
                            var f = ((float)i / (float)(panoramaHeight * ssaaFactor) - 0.5f) * (float)Math.PI;
                            var f2 = (num5 * 2f - 1f) * (float)Math.PI;
                            var num6 = Mathf.Cos(f);
                            var vector = new Vector3(num6 * Mathf.Sin(f2), 0f - Mathf.Sin(f), num6 * Mathf.Cos(f2));
                            var num7 = 1f / vector.y;
                            var num8 = vector.x * num7;
                            var num9 = vector.z * num7;
                            num8 = 0f - num8;
                            num8 = (num8 + 1f) / 2f;
                            num9 = (num9 + 1f) / 2f;
                            var cameraPixelBilinear = GetCameraPixelBilinear(cameraPixels, 3, num8, num9);
                            num += cameraPixelBilinear.r;
                            num2 += cameraPixelBilinear.g;
                            num3 += cameraPixelBilinear.b;
                            num4 += cameraPixelBilinear.a;
                        }
                    }

                    var num10 = stride * (panoramaHeight - 1 - y) + x * 4;
                    pixelValues[num10] = (byte)(num3 / numPixelsAveraged);
                    pixelValues[num10 + 1] = (byte)(num2 / numPixelsAveraged);
                    pixelValues[num10 + 2] = (byte)(num / numPixelsAveraged);
                    pixelValues[num10 + 3] = (byte)(num4 / numPixelsAveraged);
                    if ((x & 0xFF) == 0 && Time.realtimeSinceStartup - startTime > processingTimePerFrame)
                    {
                        yield return null;
                        startTime = Time.realtimeSinceStartup;
                    }
                }
            }
        }

        private IEnumerator CubemapToEquirectangularCpuPositiveX(
            uint[] cameraPixels,
            byte[] pixelValues,
            int stride,
            int panoramaWidth,
            int panoramaHeight,
            int ssaaFactor,
            float startTime,
            float processingTimePerFrame,
            int numPixelsAveraged,
            int startX,
            int startY,
            int endX,
            int endY)
        {
            for (var y = startY; y < endY; y++)
            {
                for (var x = startX; x < endX; x++)
                {
                    var num = 0;
                    var num2 = 0;
                    var num3 = 0;
                    var num4 = 0;
                    for (var i = y * ssaaFactor; i < (y + 1) * ssaaFactor; i++)
                    {
                        for (var j = x * ssaaFactor; j < (x + 1) * ssaaFactor; j++)
                        {
                            var num5 = (float)j / (float)(panoramaWidth * ssaaFactor);
                            var f = ((float)i / (float)(panoramaHeight * ssaaFactor) - 0.5f) * (float)Math.PI;
                            var f2 = (num5 * 2f - 1f) * (float)Math.PI;
                            var num6 = Mathf.Cos(f);
                            var vector = new Vector3(num6 * Mathf.Sin(f2), 0f - Mathf.Sin(f), num6 * Mathf.Cos(f2));
                            var num7 = 1f / vector.x;
                            var num8 = (0f - vector.z) * num7;
                            var num9 = vector.y * num7;
                            num9 = 0f - num9;
                            num8 = (num8 + 1f) / 2f;
                            num9 = (num9 + 1f) / 2f;
                            var cameraPixelBilinear = GetCameraPixelBilinear(cameraPixels, 0, num8, num9);
                            num += cameraPixelBilinear.r;
                            num2 += cameraPixelBilinear.g;
                            num3 += cameraPixelBilinear.b;
                            num4 += cameraPixelBilinear.a;
                        }
                    }

                    var num10 = stride * (panoramaHeight - 1 - y) + x * 4;
                    pixelValues[num10] = (byte)(num3 / numPixelsAveraged);
                    pixelValues[num10 + 1] = (byte)(num2 / numPixelsAveraged);
                    pixelValues[num10 + 2] = (byte)(num / numPixelsAveraged);
                    pixelValues[num10 + 3] = (byte)(num4 / numPixelsAveraged);
                    if ((x & 0xFF) == 0 && Time.realtimeSinceStartup - startTime > processingTimePerFrame)
                    {
                        yield return null;
                        startTime = Time.realtimeSinceStartup;
                    }
                }
            }
        }

        private IEnumerator CubemapToEquirectangularCpuNegativeX(
            uint[] cameraPixels,
            byte[] pixelValues,
            int stride,
            int panoramaWidth,
            int panoramaHeight,
            int ssaaFactor,
            float startTime,
            float processingTimePerFrame,
            int numPixelsAveraged,
            int startX,
            int startY,
            int endX,
            int endY)
        {
            for (var y = startY; y < endY; y++)
            {
                for (var x = startX; x < endX; x++)
                {
                    var num = 0;
                    var num2 = 0;
                    var num3 = 0;
                    var num4 = 0;
                    for (var i = y * ssaaFactor; i < (y + 1) * ssaaFactor; i++)
                    {
                        for (var j = x * ssaaFactor; j < (x + 1) * ssaaFactor; j++)
                        {
                            var num5 = (float)j / (float)(panoramaWidth * ssaaFactor);
                            var f = ((float)i / (float)(panoramaHeight * ssaaFactor) - 0.5f) * (float)Math.PI;
                            var f2 = (num5 * 2f - 1f) * (float)Math.PI;
                            var num6 = Mathf.Cos(f);
                            var vector = new Vector3(num6 * Mathf.Sin(f2), 0f - Mathf.Sin(f), num6 * Mathf.Cos(f2));
                            var num7 = 1f / vector.x;
                            var num8 = (0f - vector.z) * num7;
                            var num9 = vector.y * num7;
                            num8 = (num8 + 1f) / 2f;
                            num9 = (num9 + 1f) / 2f;
                            var cameraPixelBilinear = GetCameraPixelBilinear(cameraPixels, 1, num8, num9);
                            num += cameraPixelBilinear.r;
                            num2 += cameraPixelBilinear.g;
                            num3 += cameraPixelBilinear.b;
                            num4 += cameraPixelBilinear.a;
                        }
                    }

                    var num10 = stride * (panoramaHeight - 1 - y) + x * 4;
                    pixelValues[num10] = (byte)(num3 / numPixelsAveraged);
                    pixelValues[num10 + 1] = (byte)(num2 / numPixelsAveraged);
                    pixelValues[num10 + 2] = (byte)(num / numPixelsAveraged);
                    pixelValues[num10 + 3] = (byte)(num4 / numPixelsAveraged);
                    if ((x & 0xFF) == 0 && Time.realtimeSinceStartup - startTime > processingTimePerFrame)
                    {
                        yield return null;
                        startTime = Time.realtimeSinceStartup;
                    }
                }
            }
        }

        private IEnumerator CubemapToEquirectangularCpuPositiveZ(
            uint[] cameraPixels,
            byte[] pixelValues,
            int stride,
            int panoramaWidth,
            int panoramaHeight,
            int ssaaFactor,
            float startTime,
            float processingTimePerFrame,
            int numPixelsAveraged,
            int startX,
            int startY,
            int endX,
            int endY)
        {
            for (var y = startY; y < endY; y++)
            {
                for (var x = startX; x < endX; x++)
                {
                    var num = 0;
                    var num2 = 0;
                    var num3 = 0;
                    var num4 = 0;
                    for (var i = y * ssaaFactor; i < (y + 1) * ssaaFactor; i++)
                    {
                        for (var j = x * ssaaFactor; j < (x + 1) * ssaaFactor; j++)
                        {
                            var num5 = (float)j / (float)(panoramaWidth * ssaaFactor);
                            var f = ((float)i / (float)(panoramaHeight * ssaaFactor) - 0.5f) * (float)Math.PI;
                            var f2 = (num5 * 2f - 1f) * (float)Math.PI;
                            var num6 = Mathf.Cos(f);
                            var vector = new Vector3(num6 * Mathf.Sin(f2), 0f - Mathf.Sin(f), num6 * Mathf.Cos(f2));
                            var num7 = 1f / vector.z;
                            var num8 = vector.x * num7;
                            var num9 = vector.y * num7;
                            num9 = 0f - num9;
                            num8 = (num8 + 1f) / 2f;
                            num9 = (num9 + 1f) / 2f;
                            var cameraPixelBilinear = GetCameraPixelBilinear(cameraPixels, 4, num8, num9);
                            num += cameraPixelBilinear.r;
                            num2 += cameraPixelBilinear.g;
                            num3 += cameraPixelBilinear.b;
                            num4 += cameraPixelBilinear.a;
                        }
                    }

                    var num10 = stride * (panoramaHeight - 1 - y) + x * 4;
                    pixelValues[num10] = (byte)(num3 / numPixelsAveraged);
                    pixelValues[num10 + 1] = (byte)(num2 / numPixelsAveraged);
                    pixelValues[num10 + 2] = (byte)(num / numPixelsAveraged);
                    pixelValues[num10 + 3] = (byte)(num4 / numPixelsAveraged);
                    if ((x & 0xFF) == 0 && Time.realtimeSinceStartup - startTime > processingTimePerFrame)
                    {
                        yield return null;
                        startTime = Time.realtimeSinceStartup;
                    }
                }
            }
        }

        private IEnumerator CubemapToEquirectangularCpuNegativeZ(
            uint[] cameraPixels,
            byte[] pixelValues,
            int stride,
            int panoramaWidth,
            int panoramaHeight,
            int ssaaFactor,
            float startTime,
            float processingTimePerFrame,
            int numPixelsAveraged,
            int startX,
            int startY,
            int endX,
            int endY)
        {
            for (var y = startY; y < endY; y++)
            {
                for (var x = startX; x < endX; x++)
                {
                    var num = 0;
                    var num2 = 0;
                    var num3 = 0;
                    var num4 = 0;
                    for (var i = y * ssaaFactor; i < (y + 1) * ssaaFactor; i++)
                    {
                        for (var j = x * ssaaFactor; j < (x + 1) * ssaaFactor; j++)
                        {
                            var num5 = (float)j / (float)(panoramaWidth * ssaaFactor);
                            var f = ((float)i / (float)(panoramaHeight * ssaaFactor) - 0.5f) * (float)Math.PI;
                            var f2 = (num5 * 2f - 1f) * (float)Math.PI;
                            var num6 = Mathf.Cos(f);
                            var vector = new Vector3(num6 * Mathf.Sin(f2), 0f - Mathf.Sin(f), num6 * Mathf.Cos(f2));
                            var num7 = 1f / vector.z;
                            var num8 = vector.x * num7;
                            var num9 = vector.y * num7;
                            num8 = (num8 + 1f) / 2f;
                            num9 = (num9 + 1f) / 2f;
                            var cameraPixelBilinear = GetCameraPixelBilinear(cameraPixels, 5, num8, num9);
                            num += cameraPixelBilinear.r;
                            num2 += cameraPixelBilinear.g;
                            num3 += cameraPixelBilinear.b;
                            num4 += cameraPixelBilinear.a;
                        }
                    }

                    var num10 = stride * (panoramaHeight - 1 - y) + x * 4;
                    pixelValues[num10] = (byte)(num3 / numPixelsAveraged);
                    pixelValues[num10 + 1] = (byte)(num2 / numPixelsAveraged);
                    pixelValues[num10 + 2] = (byte)(num / numPixelsAveraged);
                    pixelValues[num10 + 3] = (byte)(num4 / numPixelsAveraged);
                    if ((x & 0xFF) == 0 && Time.realtimeSinceStartup - startTime > processingTimePerFrame)
                    {
                        yield return null;
                        startTime = Time.realtimeSinceStartup;
                    }
                }
            }
        }

        private IEnumerator CubemapToEquirectangularCpuGeneralCase(
            uint[] cameraPixels,
            byte[] pixelValues,
            int stride,
            int panoramaWidth,
            int panoramaHeight,
            int ssaaFactor,
            float startTime,
            float processingTimePerFrame,
            float maxWidth,
            float maxHeight,
            int numPixelsAveraged,
            int startX,
            int startY,
            int endX,
            int endY)
        {
            for (var y = startY; y < endY; y++)
            {
                for (var x = startX; x < endX; x++)
                {
                    var num = 0;
                    var num2 = 0;
                    var num3 = 0;
                    var num4 = 0;
                    for (var i = y * ssaaFactor; i < (y + 1) * ssaaFactor; i++)
                    {
                        for (var j = x * ssaaFactor; j < (x + 1) * ssaaFactor; j++)
                        {
                            var num5 = (float)j / (float)(panoramaWidth * ssaaFactor);
                            var f = ((float)i / (float)(panoramaHeight * ssaaFactor) - 0.5f) * (float)Math.PI;
                            var f2 = (num5 * 2f - 1f) * (float)Math.PI;
                            var num6 = Mathf.Cos(f);
                            var vector = new Vector3(num6 * Mathf.Sin(f2), 0f - Mathf.Sin(f), num6 * Mathf.Cos(f2));
                            var num7 = 1f / vector.y;
                            var num8 = vector.x * num7;
                            var num9 = vector.z * num7;
                            CubemapFace cameraNum;
                            if (vector.y > 0f)
                                cameraNum = CubemapFace.PositiveY;
                            else
                            {
                                cameraNum = CubemapFace.NegativeY;
                                num8 = 0f - num8;
                            }

                            if (Mathf.Abs(num8) > 1f || Mathf.Abs(num9) > 1f)
                            {
                                num7 = 1f / vector.x;
                                num8 = (0f - vector.z) * num7;
                                num9 = vector.y * num7;
                                if (vector.x > 0f)
                                {
                                    cameraNum = CubemapFace.PositiveX;
                                    num9 = 0f - num9;
                                }
                                else
                                    cameraNum = CubemapFace.NegativeX;
                            }

                            if (Mathf.Abs(num8) > 1f || Mathf.Abs(num9) > 1f)
                            {
                                num7 = 1f / vector.z;
                                num8 = vector.x * num7;
                                num9 = vector.y * num7;
                                if (vector.z > 0f)
                                {
                                    cameraNum = CubemapFace.PositiveZ;
                                    num9 = 0f - num9;
                                }
                                else
                                    cameraNum = CubemapFace.NegativeZ;
                            }

                            num8 = (num8 + 1f) / 2f;
                            num9 = (num9 + 1f) / 2f;
                            num8 = Mathf.Min(num8, maxWidth);
                            num9 = Mathf.Min(num9, maxHeight);
                            var cameraPixelBilinear = GetCameraPixelBilinear(cameraPixels, (int)cameraNum, num8, num9);
                            num += cameraPixelBilinear.r;
                            num2 += cameraPixelBilinear.g;
                            num3 += cameraPixelBilinear.b;
                            num4 += cameraPixelBilinear.a;
                        }
                    }

                    var num10 = stride * (panoramaHeight - 1 - y) + x * 4;
                    pixelValues[num10] = (byte)(num3 / numPixelsAveraged);
                    pixelValues[num10 + 1] = (byte)(num2 / numPixelsAveraged);
                    pixelValues[num10 + 2] = (byte)(num / numPixelsAveraged);
                    pixelValues[num10 + 3] = (byte)(num4 / numPixelsAveraged);
                    if ((x & 0xFF) == 0 && Time.realtimeSinceStartup - startTime > processingTimePerFrame)
                    {
                        yield return null;
                        startTime = Time.realtimeSinceStartup;
                    }
                }
            }
        }
    }
}
