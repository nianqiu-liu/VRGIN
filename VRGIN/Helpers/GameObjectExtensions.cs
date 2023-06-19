using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace VRGIN.Helpers
{
    public static class GameObjectExtensions
    {
        public static IEnumerable<MonoBehaviour> GetCameraEffects(this GameObject go)
        {
            return go.GetComponents<MonoBehaviour>().Where(IsCameraEffect);
        }

        private static bool IsCameraEffect(MonoBehaviour component)
        {
            return IsImageEffect(component.GetType());
        }

        public static int Level(this GameObject go)
        {
            if (!go.transform.parent) return 0;
            return go.transform.parent.gameObject.Level() + 1;
        }

        private static bool IsImageEffect(Type type)
        {
            if (type != null)
            {
                if (!type.Name.EndsWith("Effect") && !type.Name.Contains("AmbientOcclusion")) return IsImageEffect(type.BaseType);
                return true;
            }

            return false;
        }

        public static U CopyComponentFrom<T, U>(this GameObject destination, T original) where T : Component where U : T
        {
            var typeFromHandle = typeof(T);
            var val = destination.AddComponent<U>();
            var fields = typeFromHandle.GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (var fieldInfo in fields) fieldInfo.SetValue(val, fieldInfo.GetValue(original));
            return val;
        }

        public static T CopyComponentFrom<T>(this GameObject destination, T original) where T : Component
        {
            var type = original.GetType();
            var val = destination.AddComponent(type) as T;
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (var fieldInfo in fields) fieldInfo.SetValue(val, fieldInfo.GetValue(original));
            return val;
        }

        public static string GetPath(this Component component)
        {
            if (!component.transform.parent) return component.name;
            return component.transform.parent.GetPath() + "/" + component.name;
        }

        public static IEnumerable<GameObject> Children(this GameObject gameObject)
        {
            for (var i = 0; i < gameObject.transform.childCount; i++) yield return gameObject.transform.GetChild(i).gameObject;
        }

        public static IEnumerable<Transform> Children(this Transform transform)
        {
            for (var i = 0; i < transform.childCount; i++) yield return transform.GetChild(i);
        }

        public static IEnumerable<Transform> Ancestors(this Transform transform)
        {
            var t = transform;
            while ((bool)t.parent)
            {
                t = t.parent;
                yield return t;
            }
        }

        public static int Depth(this Transform transform)
        {
            return transform.Ancestors().Count();
        }

        public static IEnumerable<GameObject> Descendants(this GameObject gameObject)
        {
            var queue = new Queue<GameObject>();
            queue.Enqueue(gameObject);
            while (queue.Count > 0)
            {
                var gameObject2 = queue.Dequeue();
                foreach (var child in gameObject2.Children())
                {
                    yield return child;
                    queue.Enqueue(child);
                }
            }
        }

        public static IEnumerable<Transform> Descendants(this Transform transform)
        {
            return from d in transform.gameObject.Descendants()
                   select d.transform;
        }

        public static Transform FindDescendant(this Transform transform, string name)
        {
            return transform.Descendants().FirstOrDefault((Transform d) => d.name == name);
        }

        public static Transform FindDescendant(this Transform transform, Regex name)
        {
            return transform.Descendants().FirstOrDefault((Transform d) => name.IsMatch(d.name));
        }

        public static IEnumerable<GameObject> FindGameObjectsByTag(this GameObject gameObject, string tag)
        {
            return from child in gameObject.Descendants()
                   where child.CompareTag(tag)
                   select child;
        }

        public static GameObject FindGameObjectByTag(this GameObject gameObject, string tag)
        {
            return gameObject.FindGameObjectsByTag(tag).FirstOrDefault();
        }
    }
}
