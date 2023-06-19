using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace CapturePanorama.Internals
{
    internal class ImageEffectCopyCamera : MonoBehaviour
    {
        public struct InstanceMethodPair
        {
            public object Instance;

            public MethodInfo Method;
        }

        public List<InstanceMethodPair> onRenderImageMethods = new List<InstanceMethodPair>();

        private RenderTexture[] temp = new RenderTexture[2];

        public static List<InstanceMethodPair> GenerateMethodList(Camera camToCopy)
        {
            var list = new List<InstanceMethodPair>();
            var components = camToCopy.gameObject.GetComponents<MonoBehaviour>();
            foreach (var monoBehaviour in components)
            {
                if (monoBehaviour.enabled)
                {
                    var method = monoBehaviour.GetType().GetMethod("OnRenderImage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[2]
                    {
                        typeof(RenderTexture),
                        typeof(RenderTexture)
                    }, null);
                    if (method != null)
                    {
                        var item = default(InstanceMethodPair);
                        item.Instance = monoBehaviour;
                        item.Method = method;
                        list.Add(item);
                    }
                }
            }

            return list;
        }

        private void OnDestroy()
        {
            for (var i = 0; i < temp.Length; i++)
            {
                if (temp[i] != null) Destroy(temp[i]);
                temp[i] = null;
            }
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            var num = Math.Max(src.depth, dest.depth);
            for (var i = 0; i < temp.Length; i++)
            {
                if (onRenderImageMethods.Count > i + 1)
                {
                    if (temp[i] != null && (temp[i].width != dest.width || temp[i].height != dest.height || temp[i].depth != num || temp[i].format != dest.format))
                    {
                        Destroy(temp[i]);
                        temp[i] = null;
                    }

                    if (temp[i] == null) temp[i] = new RenderTexture(dest.width, dest.height, num, dest.format);
                }
            }

            var list = new List<RenderTexture>();
            list.Add(src);
            for (var j = 0; j < onRenderImageMethods.Count - 1; j++) list.Add(j % 2 == 0 ? temp[0] : temp[1]);
            list.Add(dest);
            for (var k = 0; k < onRenderImageMethods.Count; k++)
            {
                onRenderImageMethods[k].Method.Invoke(onRenderImageMethods[k].Instance, new object[2]
                {
                    list[k],
                    list[k + 1]
                });
            }
        }
    }
}
