using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;

namespace WorldMapStrategyKit
{
	[ExecuteInEditMode]
	public class ViewportHelper : MonoBehaviour
	{
#if UNITY_EDITOR
#if UNITY_2018_3_OR_NEWER
		private void Update()
		{
			if (WMSK.instance == null)
				Debug.LogError(
					"<b>WMSK not found in scene!</b>: viewport requires a instance of World Map Strategy Kit present in the scene. Add a WorldMapStrategyKit prefab to the scene and drag&drop the Viewport gameObject to the Render View port section.");
			else if (!WMSK.instance.renderViewportIsEnabled)
			{
				if (PrefabUtility.GetPrefabInstanceStatus(WMSK.instance.gameObject) !=
				    PrefabInstanceStatus.NotAPrefab)
					PrefabUtility.UnpackPrefabInstance(WMSK.instance.gameObject,
						PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
				if (PrefabUtility.GetPrefabInstanceStatus(gameObject) != PrefabInstanceStatus.NotAPrefab)
					PrefabUtility.UnpackPrefabInstance(gameObject, PrefabUnpackMode.Completely,
						InteractionMode.AutomatedAction);
				WMSK.instance.transform.position +=
					new Vector3(500, 500, -500); // keep normal map out of camera
				WMSK.instance.renderViewport = gameObject;
				if (Camera.main != null)
				{
					Camera.main.transform.position = Misc.Vector3zero;
					transform.position = Misc.Vector3forward * 200f;
				}
			}
			DestroyImmediate(this);
		}

#else
        void Update() {
            if (WMSK.instance == null) {
                EditorUtility.DisplayDialog("WMSK not found in scene", "Viewport requires a instance of World Map Strategy Kit present in the scene.\n\nAdd a WorldMapStrategyKit prefab to the scene and drag&drop the Viewport gameObject to the Render View port section.", "Ok");
            } else if (!WMSK.instance.renderViewportIsEnabled) {
				if (PrefabUtility.GetPrefabType (WMSK.instance.gameObject) == PrefabType.PrefabInstance) {
					PrefabUtility.DisconnectPrefabInstance (WMSK.instance.gameObject);
				}
				if (PrefabUtility.GetPrefabType (gameObject) == PrefabType.PrefabInstance) {
					PrefabUtility.DisconnectPrefabInstance (gameObject);
				}
				WMSK.instance.transform.position +=
 new Vector3(500, 500, -500); // keep normal map out of camera
                WMSK.instance.renderViewport = gameObject;
                if (Camera.main != null) {
                    Camera.main.transform.position = Misc.Vector3zero;
                    transform.position = Misc.Vector3forward * 200f;
                }
            }
            DestroyImmediate(this);
        }
#endif
#endif
	}
}