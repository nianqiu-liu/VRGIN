using System;
using System.IO;
using UnityEngine;

namespace CapturePanorama
{
    public class ReadPanoConfig : MonoBehaviour
    {
        public string iniPath;

        private void Start()
        {
            if (Application.isEditor) return;
            var component = GetComponent<CapturePanorama>();
            var text = iniPath;
            if (text == "")
            {
                var text2 = "CapturePanorama.ini";
                text = Application.dataPath + "/" + text2;
            }

            if (!File.Exists(text))
            {
                WriteConfig(text, component);
                return;
            }

            var array = File.ReadAllLines(text);
            foreach (var text3 in array)
            {
                if (!(text3.Trim() == ""))
                {
                    var array2 = text3.Split(new char[1] { '=' }, 2);
                    var text4 = array2[0].Trim();
                    var text5 = array2[1].Trim();
                    switch (text4)
                    {
                        case "Panorama Name":
                            component.panoramaName = text5;
                            break;
                        case "Capture Key":
                            component.captureKey = (KeyCode)Enum.Parse(typeof(KeyCode), text5);
                            break;
                        case "Image Format":
                            component.imageFormat = (CapturePanorama.ImageFormat)Enum.Parse(typeof(CapturePanorama.ImageFormat), text5);
                            break;
                        case "Capture Stereoscopic":
                            component.captureStereoscopic = bool.Parse(text5);
                            break;
                        case "Interpupillary Distance":
                            component.interpupillaryDistance = float.Parse(text5);
                            break;
                        case "Num Circle Points":
                            component.numCirclePoints = int.Parse(text5);
                            break;
                        case "Panorama Width":
                            component.panoramaWidth = int.Parse(text5);
                            break;
                        case "Anti Aliasing":
                            component.antiAliasing = (CapturePanorama.AntiAliasing)int.Parse(text5);
                            break;
                        case "Ssaa Factor":
                            component.ssaaFactor = int.Parse(text5);
                            break;
                        case "Save Image Path":
                            component.saveImagePath = text5;
                            break;
                        case "Save Cubemap":
                            component.saveCubemap = bool.Parse(text5);
                            break;
                        case "Upload Images":
                            component.uploadImages = bool.Parse(text5);
                            break;
                        case "Use Default Orientation":
                            component.useDefaultOrientation = bool.Parse(text5);
                            break;
                        case "Use Gpu Transform":
                            component.useGpuTransform = bool.Parse(text5);
                            break;
                        case "Cpu Milliseconds Per Frame":
                            component.cpuMillisecondsPerFrame = (float)double.Parse(text5);
                            break;
                        case "Capture Every Frame":
                            component.captureEveryFrame = bool.Parse(text5);
                            break;
                        case "Frame Rate":
                            component.frameRate = int.Parse(text5);
                            break;
                        case "Max Frames To Record":
                            component.maxFramesToRecord = !(text5 == "") ? int.Parse(text5) : 0;
                            break;
                        case "Frame Number Digits":
                            component.frameNumberDigits = int.Parse(text5);
                            break;
                        case "Fade During Capture":
                            component.fadeDuringCapture = bool.Parse(text5);
                            break;
                        case "Fade Time":
                            component.fadeTime = float.Parse(text5);
                            break;
                        case "Enable Debugging":
                            component.enableDebugging = bool.Parse(text5);
                            break;
                        default:
                            Debug.LogError("Unrecognized key in line in CapturePanorama.ini: " + text3);
                            break;
                    }
                }
            }
        }

        private void WriteConfig(string path, CapturePanorama pano)
        {
            using var streamWriter = new StreamWriter(path);
            streamWriter.WriteLine("Panorama Name=" + pano.panoramaName);
            streamWriter.WriteLine("Capture Key=" + pano.captureKey);
            streamWriter.WriteLine("Image Format=" + pano.imageFormat);
            streamWriter.WriteLine("Capture Stereoscopic=" + pano.captureStereoscopic);
            streamWriter.WriteLine("Interpupillary Distance=" + pano.interpupillaryDistance);
            streamWriter.WriteLine("Num Circle Points=" + pano.numCirclePoints);
            streamWriter.WriteLine("Panorama Width=" + pano.panoramaWidth);
            var antiAliasing = (int)pano.antiAliasing;
            streamWriter.WriteLine("Anti Aliasing=" + antiAliasing);
            streamWriter.WriteLine("Ssaa Factor=" + pano.ssaaFactor);
            streamWriter.WriteLine("Save Image Path=" + pano.saveImagePath);
            streamWriter.WriteLine("Save Cubemap=" + pano.saveCubemap);
            streamWriter.WriteLine("Upload Images=" + pano.uploadImages);
            streamWriter.WriteLine("Use Default Orientation=" + pano.useDefaultOrientation);
            streamWriter.WriteLine("Use Gpu Transform=" + pano.useGpuTransform);
            streamWriter.WriteLine("Cpu Milliseconds Per Frame=" + pano.cpuMillisecondsPerFrame);
            streamWriter.WriteLine("Capture Every Frame=" + pano.captureEveryFrame);
            streamWriter.WriteLine("Frame Rate=" + pano.frameRate);
            streamWriter.WriteLine("Max Frames To Record=" + pano.maxFramesToRecord);
            streamWriter.WriteLine("Frame Number Digits=" + pano.frameNumberDigits);
            streamWriter.WriteLine("Fade During Capture=" + pano.fadeDuringCapture);
            streamWriter.WriteLine("Fade Time=" + pano.fadeTime);
            streamWriter.WriteLine("Enable Debugging=" + pano.enableDebugging);
        }
    }
}
