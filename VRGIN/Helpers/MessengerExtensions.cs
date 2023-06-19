using System.Reflection;
using UnityEngine;

namespace VRGIN.Helpers
{
    public static class MessengerExtensions
    {
        private static void InvokeIfExists(this object objectToCheck, string methodName, params object[] parameters)
        {
            var method = objectToCheck.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method != null) method.Invoke(objectToCheck, parameters);
        }

        public static void BroadcastToAll(this GameObject gameobject, string methodName, params object[] parameters)
        {
            var components = gameobject.GetComponents<MonoBehaviour>();
            for (var i = 0; i < components.Length; i++) components[i].InvokeIfExists(methodName, parameters);
        }

        public static void BroadcastToAll(this Component component, string methodName, params object[] parameters)
        {
            component.gameObject.BroadcastToAll(methodName, parameters);
        }

        public static void SendMessageToAll(this GameObject gameobject, string methodName, params object[] parameters)
        {
            var componentsInChildren = gameobject.GetComponentsInChildren<MonoBehaviour>(true);
            for (var i = 0; i < componentsInChildren.Length; i++) componentsInChildren[i].InvokeIfExists(methodName, parameters);
        }

        public static void SendMessageToAll(this Component component, string methodName, params object[] parameters)
        {
            component.gameObject.SendMessageToAll(methodName, parameters);
        }

        public static void SendMessageUpwardsToAll(this GameObject gameobject, string methodName, params object[] parameters)
        {
            var transform = gameobject.transform;
            while (transform != null)
            {
                transform.gameObject.BroadcastToAll(methodName, parameters);
                transform = transform.parent;
            }
        }

        public static void SendMessageUpwardsToAll(this Component component, string methodName, params object[] parameters)
        {
            component.gameObject.SendMessageUpwardsToAll(methodName, parameters);
        }
    }
}
