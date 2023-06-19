using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using VRGIN.Core;

namespace VRGIN.Helpers
{
	public class Profiler : ProtectedBehaviour
	{
		public delegate void Callback();

		private const int DEFAULT_SAMPLE_COUNT = 30;

		private const float INTERVAL_TIME = 0.01f;

		private Callback _Callback;

		private double _CurrentInterval;

		public static void FindHotPaths(Callback callback)
		{
			if (!GameObject.Find("Profiler"))
			{
				new GameObject("Profiler").AddComponent<Profiler>()._Callback = callback;
			}
		}

		protected override void OnStart()
		{
			base.OnStart();
			StartCoroutine(Measure());
		}

		private IEnumerator Measure()
		{
			List<GameObject> queue = (from n in UnityHelper.GetRootNodes().Except(new GameObject[1] { base.gameObject })
				where !n.name.StartsWith("VRGIN") && !n.name.StartsWith("[")
				select n).ToList();
			yield return StartCoroutine(MeasureFramerate(30));
			double startInterval = _CurrentInterval;
			VRLog.Info("Starting to profile! This might take a while...");
			while (queue.Count > 0)
			{
				GameObject obj = queue.First();
				queue.RemoveAt(0);
				if (!obj.activeInHierarchy)
				{
					continue;
				}
				obj.SetActive(false);
				yield return StartCoroutine(MeasureFramerate(30));
				obj.SetActive(true);
				double num = startInterval / _CurrentInterval;
				VRLog.Info("{0}{1}: {2:0.00}", string.Join("", Enumerable.Repeat(" ", obj.transform.Depth()).ToArray()), obj.name, num);
				if (num > 1.149999976158142)
				{
					queue.InsertRange(0, obj.Children());
					foreach (Behaviour component in from c in obj.GetComponents<Behaviour>()
						where c.enabled
						select c)
					{
						component.enabled = false;
						yield return StartCoroutine(MeasureFramerate(30));
						component.enabled = true;
						num = startInterval / _CurrentInterval;
						VRLog.Info("{0}{1} [{2}]: {3:0.000}", string.Join("", Enumerable.Repeat(" ", obj.transform.Depth()).ToArray()), obj.name, component.GetType().Name, num);
					}
				}
				yield return null;
			}
			VRLog.Info("Done!");
			_Callback();
			Object.Destroy(base.gameObject);
		}

		private IEnumerator MeasureFramerate(int sampleCount)
		{
			yield return new WaitForSeconds(0.01f);
			long[] samples = new long[sampleCount];
			yield return null;
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			for (int i = 0; i < sampleCount; i++)
			{
				stopwatch.Reset();
				stopwatch.Start();
				yield return null;
				samples[i] = stopwatch.ElapsedMilliseconds;
			}
			_CurrentInterval = samples.Average();
			yield return new WaitForSeconds(0.01f);
		}
	}
}
