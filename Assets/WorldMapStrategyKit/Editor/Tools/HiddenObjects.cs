using UnityEngine;
using UnityEditor;
using System.Collections;

namespace WorldMapStrategyKit
{
	public class HiddenObjects : EditorWindow
	{
		[MenuItem("GameObject/Hidden GameObjects Tool (WMSK)")]
		public static void Create()
		{
			GetWindow<HiddenObjects>("Hidden Tool");
		}

		private string gameObjectName;

		private void OnGUI()
		{
			GUILayout.Label(
				"This tools deal with hidden GameObjects under the WMSK hierarchy (those with the HideFlags.HideInHierarchy flag set).",
				EditorStyles.wordWrappedLabel);

			if (GUILayout.Button("Find/Count All Hidden Orphans"))
			{
				var tt = (Transform[])Resources.FindObjectsOfTypeAll(typeof(Transform));
				var count = 0;
				foreach (var t in tt)
					if ((t.gameObject.hideFlags & HideFlags.HideInHierarchy) != 0 && t.parent == null)
					{
						Debug.Log(t.gameObject.name + " is invisible in the hierarchy.");
						count++;
					}
				Debug.Log(count + " hidden GameObject(s) found.");
			}

			if (GUILayout.Button("Show All Hidden GameObjects under WMSK"))
			{
				var g = FindObjectOfType<WMSK>().gameObject;
				var count = 0;
				foreach (Transform t in g.transform)
					if ((t.gameObject.hideFlags & HideFlags.HideInHierarchy) != 0)
					{
						t.gameObject.hideFlags ^= HideFlags.HideInHierarchy;
						count++;
						Debug.Log(g.name + " is now visible in the hierarchy.");
					}
				Debug.Log(count + " GameObject(s) found.");
			}
			if (GUILayout.Button("Destroy Hidden GameObjects"))
			{
				var g = FindObjectOfType<WMSK>().gameObject;
				var count = 0;
				foreach (Transform t in g.transform)
					if ((t.gameObject.hideFlags & HideFlags.HideInHierarchy) != 0)
					{
						count++;
						Debug.Log(t.gameObject.name + " destroyed.");
						DestroyImmediate(t.gameObject);
					}
				Debug.Log(count + " GameObject(s) destroyed.");
			}

			GUILayout.Label("Name of game object to delete:");
			gameObjectName = GUILayout.TextField(gameObjectName);
			if (GUILayout.Button("Delete THIS game object"))
				if (gameObjectName.Length > 0)
				{
					var tt = (Transform[])Resources.FindObjectsOfTypeAll(typeof(Transform));
					foreach (var t in tt)
						if (t.name.Equals(gameObjectName))
						{
							DestroyImmediate(t.gameObject);
							Debug.Log(gameObjectName + " destroyed.");
							break;
						}
				}
		}
	}
}