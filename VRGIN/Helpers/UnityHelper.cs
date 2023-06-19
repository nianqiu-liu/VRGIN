using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using VRGIN.Core;

namespace VRGIN.Helpers
{
	public static class UnityHelper
	{
		private class RayDrawer : ProtectedBehaviour
		{
			private Ray _Ray;

			private Color _Color;

			private float _LastTouch;

			private LineRenderer Renderer;

			public static RayDrawer Create(Color color, Ray ray)
			{
				Color color2 = color;
				RayDrawer rayDrawer = new GameObject("Ray Drawer (" + color2.ToString() + ")").AddComponent<RayDrawer>();
				rayDrawer.gameObject.AddComponent<LineRenderer>();
				rayDrawer._Ray = ray;
				rayDrawer._Color = color;
				return rayDrawer;
			}

			public void Touch(Ray ray)
			{
				_LastTouch = Time.time;
				_Ray = ray;
				base.gameObject.SetActive(true);
			}

			protected override void OnStart()
			{
				base.OnStart();
				Renderer = GetComponent<LineRenderer>();
				Renderer.SetColors(_Color, _Color);
				Renderer.SetVertexCount(2);
				Renderer.useWorldSpace = true;
				Renderer.material = VR.Context.Materials.Unlit;
				Renderer.SetWidth(0.01f, 0.01f);
			}

			protected override void OnUpdate()
			{
				base.OnUpdate();
				Renderer.SetPosition(0, (Vector3.Distance(_Ray.origin, VR.Camera.transform.position) < 0.3f) ? (_Ray.origin + _Ray.direction * 0.3f) : _Ray.origin);
				Renderer.SetPosition(1, _Ray.origin + _Ray.direction * 100f);
				CheckAge();
			}

			private void CheckAge()
			{
				if (Time.time - _LastTouch > 1f)
				{
					base.gameObject.SetActive(false);
				}
			}
		}

		private static AssetBundle _SteamVR;

		private static IDictionary<string, AssetBundle> _AssetBundles = new Dictionary<string, AssetBundle>();

		private static readonly MethodInfo _LoadFromMemory = typeof(AssetBundle).GetMethod("LoadFromMemory", new Type[1] { typeof(byte[]) });

		private static readonly MethodInfo _CreateFromMemory = typeof(AssetBundle).GetMethod("CreateFromMemoryImmediate", new Type[1] { typeof(byte[]) });

		private static Dictionary<Color, RayDrawer> _Rays = new Dictionary<Color, RayDrawer>();

		private static Dictionary<string, Transform> _DebugBalls = new Dictionary<string, Transform>();

		public static Shader GetShader(string name)
		{
			return LoadFromAssetBundle<Shader>(ResourceManager.SteamVR, name);
		}

		public static Shader GetShaderByMaterial(string name)
		{
			return LoadFromAssetBundle<Material>(ResourceManager.SteamVR, name).shader;
		}

		public static T LoadFromAssetBundle<T>(byte[] assetBundleBytes, string name) where T : UnityEngine.Object
		{
			string key = GetKey(assetBundleBytes);
			if (!_AssetBundles.ContainsKey(key))
			{
				_AssetBundles[key] = LoadAssetBundle(assetBundleBytes);
				if (_AssetBundles[key] == null)
				{
					VRLog.Error("Looks like the asset bundle failed to load?");
				}
			}
			try
			{
				VRLog.Info("Loading: {0} ({1})", name, key);
				name = name.Replace("Custom/", "");
				T val = _AssetBundles[key].LoadAsset<T>(name);
				if (!(UnityEngine.Object)val)
				{
					VRLog.Error("Failed to load {0}", name);
					VRLog.Error("All items of the same type are:");
					T[] array = _AssetBundles[key].LoadAllAssets<T>();
					foreach (T val2 in array)
					{
						VRLog.Error("\t" + val2.name);
					}
				}
				return (!typeof(Shader).IsAssignableFrom(typeof(T)) && !typeof(ComputeShader).IsAssignableFrom(typeof(T)) && !typeof(Material).IsAssignableFrom(typeof(T))) ? UnityEngine.Object.Instantiate(val) : val;
			}
			catch (Exception obj)
			{
				VRLog.Error(obj);
				return null;
			}
		}

		private static AssetBundle LoadAssetBundle(byte[] bytes)
		{
			if (_LoadFromMemory != null)
			{
				return _LoadFromMemory.Invoke(null, new object[1] { bytes }) as AssetBundle;
			}
			if (_CreateFromMemory != null)
			{
				return _CreateFromMemory.Invoke(null, new object[1] { bytes }) as AssetBundle;
			}
			VRLog.Error("Could not find a way to load AssetBundles!");
			return null;
		}

		private static string CalculateChecksum(byte[] byteToCalculate)
		{
			int num = 0;
			foreach (byte b in byteToCalculate)
			{
				num += b;
			}
			return (num & 0xFF).ToString("X2");
		}

		private static string GetKey(byte[] assetBundleBytes)
		{
			return CalculateChecksum(assetBundleBytes);
		}

		public static Transform GetDebugBall(string name)
		{
			if (!_DebugBalls.TryGetValue(name, out var value) || !value)
			{
				value = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
				value.transform.localScale *= 0.03f;
				_DebugBalls[name] = value;
			}
			return value;
		}

		public static void DrawDebugBall(Transform transform)
		{
			GetDebugBall(transform.GetInstanceID().ToString()).position = transform.position;
		}

		public static void DrawRay(Color color, Vector3 origin, Vector3 direction)
		{
			DrawRay(color, new Ray(origin, direction.normalized));
		}

		public static void DrawRay(Color color, Ray ray)
		{
			if (!_Rays.TryGetValue(color, out var value) || !value)
			{
				value = RayDrawer.Create(color, ray);
				_Rays[color] = value;
			}
			value.Touch(ray);
		}

		public static Transform CreateGameObjectAsChild(string name, Transform parent, bool dontDestroy = false)
		{
			GameObject gameObject = new GameObject(name);
			gameObject.transform.SetParent(parent, false);
			if (dontDestroy)
			{
				UnityEngine.Object.DontDestroyOnLoad(gameObject);
			}
			return gameObject.transform;
		}

		public static Texture2D LoadImage(string filePath)
		{
			filePath = Path.Combine(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Images"), filePath);
			Texture2D texture2D = null;
			if (File.Exists(filePath))
			{
				byte[] data = File.ReadAllBytes(filePath);
				texture2D = new Texture2D(2, 2);
				texture2D.LoadImage(data);
			}
			else
			{
				VRLog.Warn("File " + filePath + " does not exist");
			}
			return texture2D;
		}

		public static string[] GetLayerNames(int mask)
		{
			List<string> list = new List<string>();
			for (int i = 0; i <= 31; i++)
			{
				if ((mask & (1 << i)) != 0)
				{
					list.Add(LayerMask.LayerToName(i));
				}
			}
			return (from m in list
				select m.Trim() into m
				where m.Length > 0
				select m).ToArray();
		}

		public static T CopyComponent<T>(T original, GameObject destination) where T : Component
		{
			Type type = original.GetType();
			Component component = destination.AddComponent(type);
			FieldInfo[] fields = type.GetFields();
			foreach (FieldInfo fieldInfo in fields)
			{
				fieldInfo.SetValue(component, fieldInfo.GetValue(original));
			}
			return component as T;
		}

		public static void DumpScene(string path, bool onlyActive = false)
		{
			VRLog.Info("Dumping scene...");
			JSONArray jSONArray = new JSONArray();
			foreach (GameObject item in from go in UnityEngine.Object.FindObjectsOfType<GameObject>()
				where go.transform.parent == null
				select go)
			{
				jSONArray.Add(AnalyzeNode(item, onlyActive));
			}
			File.WriteAllText(path, jSONArray.ToJSON(0));
			VRLog.Info("Done!");
		}

		public static void DumpObject(GameObject obj, string path)
		{
			VRLog.Info("Dumping object...");
			File.WriteAllText(path, AnalyzeNode(obj).ToJSON(0));
			VRLog.Info("Done!");
		}

		public static IEnumerable<GameObject> GetRootNodes()
		{
			return from go in UnityEngine.Object.FindObjectsOfType<GameObject>()
				where go.transform.parent == null
				select go;
		}

		public static JSONClass AnalyzeComponent(Component c)
		{
			JSONClass jSONClass = new JSONClass();
			FieldInfo[] fields = c.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
			foreach (FieldInfo fieldInfo in fields)
			{
				try
				{
					string text = FieldToString(fieldInfo.Name, fieldInfo.GetValue(c));
					if (text != null)
					{
						jSONClass[fieldInfo.Name] = text;
					}
				}
				catch (Exception)
				{
					VRLog.Warn("Failed to get field {0}", fieldInfo.Name);
				}
			}
			PropertyInfo[] properties = c.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
			foreach (PropertyInfo propertyInfo in properties)
			{
				try
				{
					if (propertyInfo.GetIndexParameters().Length == 0)
					{
						string text2 = FieldToString(propertyInfo.Name, propertyInfo.GetValue(c, null));
						if (text2 != null)
						{
							jSONClass[propertyInfo.Name] = text2;
						}
					}
				}
				catch (Exception)
				{
					VRLog.Warn("Failed to get prop {0}", propertyInfo.Name);
				}
			}
			return jSONClass;
		}

		public static JSONClass AnalyzeNode(GameObject go, bool onlyActive = false)
		{
			JSONClass jSONClass = new JSONClass();
			jSONClass["name"] = go.name;
			jSONClass["active"] = go.activeSelf.ToString();
			jSONClass["tag"] = go.tag;
			jSONClass["layer"] = LayerMask.LayerToName(go.gameObject.layer);
			jSONClass["pos"] = go.transform.localPosition.ToString();
			jSONClass["rot"] = go.transform.localEulerAngles.ToString();
			jSONClass["scale"] = go.transform.localScale.ToString();
			JSONClass jSONClass2 = new JSONClass();
			Component[] components = go.GetComponents<Component>();
			foreach (Component component in components)
			{
				if (component == null)
				{
					VRLog.Warn("NULL component: " + component);
				}
				else
				{
					jSONClass2[component.GetType().Name] = AnalyzeComponent(component);
				}
			}
			JSONArray jSONArray = new JSONArray();
			foreach (GameObject item in go.Children())
			{
				if (!onlyActive || item.activeInHierarchy)
				{
					jSONArray.Add(AnalyzeNode(item, onlyActive));
				}
			}
			jSONClass["Components"] = jSONClass2;
			jSONClass["Children"] = jSONArray;
			return jSONClass;
		}

		private static string FieldToString(string memberName, object value)
		{
			if (value == null)
			{
				return null;
			}
			if (!(memberName == "cullingMask"))
			{
				if (memberName == "renderer")
				{
					return ((Renderer)value).material.shader.name;
				}
				if (value is Vector3 vector)
				{
					return $"({vector.x:0.000}, {vector.y:0.000}, {vector.z:0.000})";
				}
				if (value is Vector2 vector2)
				{
					return $"({vector2.x:0.000}, {vector2.y:0.000})";
				}
				return value.ToString();
			}
			return string.Join(", ", GetLayerNames((int)value));
		}

		public static void SetPropertyOrField<T>(T obj, string name, object value)
		{
			PropertyInfo property = typeof(T).GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			FieldInfo field = typeof(T).GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (property != null)
			{
				property.SetValue(obj, value, null);
			}
			else if (field != null)
			{
				field.SetValue(obj, value);
			}
			else
			{
				VRLog.Warn("Prop/Field not found!");
			}
		}

		public static object GetPropertyOrField<T>(T obj, string name)
		{
			PropertyInfo property = typeof(T).GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			FieldInfo field = typeof(T).GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (property != null)
			{
				return property.GetValue(obj, null);
			}
			if (field != null)
			{
				return field.GetValue(obj);
			}
			VRLog.Warn("Prop/Field not found!");
			return null;
		}

		public static void SaveTexture(RenderTexture rt, string pngOutPath)
		{
			RenderTexture active = RenderTexture.active;
			try
			{
				Texture2D texture2D = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);
				RenderTexture.active = rt;
				texture2D.ReadPixels(new Rect(0f, 0f, rt.width, rt.height), 0, 0);
				texture2D.Apply();
				File.WriteAllBytes(pngOutPath, texture2D.EncodeToPNG());
				UnityEngine.Object.Destroy(texture2D);
			}
			finally
			{
				RenderTexture.active = active;
			}
		}
	}
}
