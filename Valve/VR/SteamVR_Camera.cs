using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.XR;

namespace Valve.VR
{
    [RequireComponent(typeof(Camera))]
    public class SteamVR_Camera : MonoBehaviour
    {
        [SerializeField] private Transform _head;

        [SerializeField] private Transform _ears;

        public bool wireframe;

        private static Hashtable values;

        private const string eyeSuffix = " (eye)";

        private const string earsSuffix = " (ears)";

        private const string headSuffix = " (head)";

        private const string originSuffix = " (origin)";

        public Transform head => _head;

        public Transform offset => _head;

        public Transform origin => _head.parent;

        public Camera camera { get; private set; }

        public Transform ears => _ears;

        public static float sceneResolutionScale
        {
            get => XRSettings.eyeTextureResolutionScale;
            set => XRSettings.eyeTextureResolutionScale = value;
        }

        public string baseName
        {
            get
            {
                if (!name.EndsWith(" (eye)")) return name;
                return name.Substring(0, name.Length - " (eye)".Length);
            }
        }

        public Ray GetRay()
        {
            return new Ray(_head.position, _head.forward);
        }

        private void OnDisable()
        {
            SteamVR_Render.Remove(this);
        }

        private void OnEnable()
        {
            if (SteamVR.instance == null)
            {
                if (head != null) head.GetComponent<SteamVR_TrackedObject>().enabled = false;
                enabled = false;
                return;
            }

            var transform = this.transform;
            if (head != transform)
            {
                Expand();
                transform.parent = origin;
                while (head.childCount > 0) head.GetChild(0).parent = transform;
                head.parent = transform;
                head.localPosition = Vector3.zero;
                head.localRotation = Quaternion.identity;
                head.localScale = Vector3.one;
                head.gameObject.SetActive(false);
                _head = transform;
            }

            if (ears == null)
            {
                var componentInChildren = this.transform.GetComponentInChildren<SteamVR_Ears>();
                if (componentInChildren != null) _ears = componentInChildren.transform;
            }

            if (ears != null) ears.GetComponent<SteamVR_Ears>().vrcam = this;
            SteamVR_Render.Add(this);
        }

        private void Awake()
        {
            camera = GetComponent<Camera>();
            ForceLast();
        }

        public void ForceLast()
        {
            if (values != null)
            {
                foreach (DictionaryEntry value in values) (value.Key as FieldInfo).SetValue(this, value.Value);
                values = null;
                return;
            }

            var components = GetComponents<Component>();
            for (var i = 0; i < components.Length; i++)
            {
                var steamVR_Camera = components[i] as SteamVR_Camera;
                if (steamVR_Camera != null && steamVR_Camera != this) DestroyImmediate(steamVR_Camera);
            }

            components = GetComponents<Component>();
            if (!(this != components[components.Length - 1])) return;
            values = new Hashtable();
            var fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var fieldInfo in fields)
            {
                if (fieldInfo.IsPublic || fieldInfo.IsDefined(typeof(SerializeField), true)) values[fieldInfo] = fieldInfo.GetValue(this);
            }

            var obj = gameObject;
            DestroyImmediate(this);
            obj.AddComponent<SteamVR_Camera>().ForceLast();
        }

        public void Expand()
        {
            var parent = transform.parent;
            if (parent == null)
            {
                parent = new GameObject(name + " (origin)").transform;
                parent.localPosition = transform.localPosition;
                parent.localRotation = transform.localRotation;
                parent.localScale = transform.localScale;
            }

            if (head == null)
            {
                _head = new GameObject(name + " (head)", typeof(SteamVR_TrackedObject)).transform;
                head.parent = parent;
                head.position = transform.position;
                head.rotation = transform.rotation;
                head.localScale = Vector3.one;
                head.tag = tag;
            }

            if (transform.parent != head)
            {
                transform.parent = head;
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
                while (transform.childCount > 0) transform.GetChild(0).parent = head;
                var component = GetComponent<AudioListener>();
                if (component != null)
                {
                    DestroyImmediate(component);
                    _ears = new GameObject(name + " (ears)", typeof(SteamVR_Ears)).transform;
                    ears.parent = _head;
                    ears.localPosition = Vector3.zero;
                    ears.localRotation = Quaternion.identity;
                    ears.localScale = Vector3.one;
                }
            }

            if (!name.EndsWith(" (eye)")) name += " (eye)";
        }

        public void Collapse()
        {
            this.transform.parent = null;
            while (head.childCount > 0) head.GetChild(0).parent = transform;
            if (ears != null)
            {
                while (ears.childCount > 0) ears.GetChild(0).parent = transform;
                DestroyImmediate(ears.gameObject);
                _ears = null;
                gameObject.AddComponent(typeof(AudioListener));
            }

            if (origin != null)
            {
                if (origin.name.EndsWith(" (origin)"))
                {
                    var transform = origin;
                    while (transform.childCount > 0) transform.GetChild(0).parent = transform.parent;
                    DestroyImmediate(transform.gameObject);
                }
                else
                    transform.parent = origin;
            }

            DestroyImmediate(head.gameObject);
            _head = null;
            if (name.EndsWith(" (eye)")) name = name.Substring(0, name.Length - " (eye)".Length);
        }
    }
}
