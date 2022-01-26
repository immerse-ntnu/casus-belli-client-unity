using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace WorldMapStrategyKit
{
	public static class EditorCoroutines
	{
		public class Coroutine
		{
			public IEnumerator enumerator;
			public Action<bool> OnUpdate;
			public List<IEnumerator> history = new();
		}

		private static readonly List<Coroutine> coroutines = new();

		public static IEnumerator Start(IEnumerator enumerator, Action<bool> OnUpdate = null)
		{
			if (coroutines.Count == 0)
			{
				EditorApplication.update -= Update;
				EditorApplication.update += Update;
			}
			var coroutine = new Coroutine { enumerator = enumerator, OnUpdate = OnUpdate };
			coroutines.Add(coroutine);
			return enumerator;
		}

		private static void Update()
		{
			for (var i = 0; i < coroutines.Count; i++)
			{
				var coroutine = coroutines[i];
				var done = !coroutine.enumerator.MoveNext();
				if (done)
				{
					if (coroutine.history.Count == 0)
					{
						coroutines.RemoveAt(i);
						i--;
					}
					else
					{
						done = false;
						coroutine.enumerator = coroutine.history[coroutine.history.Count - 1];
						coroutine.history.RemoveAt(coroutine.history.Count - 1);
					}
				}
				else
				{
					if (coroutine.enumerator.Current is IEnumerator)
					{
						coroutine.history.Add(coroutine.enumerator);
						coroutine.enumerator = (IEnumerator)coroutine.enumerator.Current;
					}
				}
				if (coroutine.OnUpdate != null)
					coroutine.OnUpdate(done);
			}
			if (coroutines.Count == 0)
				EditorApplication.update -= Update;
		}

		internal static void StopAll()
		{
			coroutines.Clear();
			EditorApplication.update -= Update;
		}
	}
}