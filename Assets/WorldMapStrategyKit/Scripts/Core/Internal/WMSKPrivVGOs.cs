// World Map Strategy Kit for Unity - Main Script
// (C) 2016-2020 by Ramiro Oliva (Kronnect)
// Don't modify this script - changes could be lost if you upgrade to a more recent version of WMSK

using System;
using System.Collections.Generic;
using UnityEngine;

namespace WorldMapStrategyKit
{
	public partial class WMSK : MonoBehaviour
	{
		// viewport game objects
		private Dictionary<int, GameObjectAnimator> vgosDict;
		private GameObjectAnimator[] vgos;
		private int vgosCount;
		private bool vgosArrayIsDirty;

		// Water effects
		private float buoyancyCurrentAngle;

		private void SetupVGOs()
		{
			if (vgos == null)
				vgosDict = new Dictionary<int, GameObjectAnimator>();
			if (vgos == null || vgos.Length < vgosCount)
				vgos = new GameObjectAnimator[vgosCount > 100 ? vgosCount : 100];
		}

		private void CheckVGOsArrayDirty()
		{
			if (vgos == null || vgos.Length < vgosCount)
				vgos = new GameObjectAnimator[vgosCount];
			if (!vgosArrayIsDirty)
				return;
			for (var k = 0; k < vgosCount; k++)
				if (vgos[k] == null)
				{
					vgosCount--;
					Array.Copy(vgos, k + 1, vgos, k, vgosCount);
				}
			vgosArrayIsDirty = false;
		}

		private void UpdateViewportObjectsLoop()
		{
			// Update animators
			CheckVGOsArrayDirty();
			for (var k = 0; k < vgosCount; k++)
			{
				var vgo = vgos[k];
				if (vgo.isMoving ||
				    vgo.mouseIsOver ||
				    vgo.lastKnownPosIsOnWater && vgo.enableBuoyancyEffect)
					vgo.PerformUpdateLoop();
			}
		}

		private void UpdateViewportObjectsVisibility()
		{
			// Update animators
			CheckVGOsArrayDirty();
			for (var k = 0; k < vgosCount; k++)
			{
				var vgo = vgos[k];
				vgo.UpdateTransformAndVisibility();
			}
		}

		private void RepositionViewportObjects()
		{
			if (renderViewportIsEnabled)
				for (var k = 0; k < vgosCount; k++)
				{
					var go = vgos[k];
					go.transform.SetParent(null, true);
					go.UpdateTransformAndVisibility(true);
				}
			else
				for (var k = 0; k < vgosCount; k++)
				{
					var go = vgos[k];
					go.transform.localScale = go.originalScale;
					go.UpdateTransformAndVisibility(true);
				}
		}

		private void UpdateViewportObjectsBuoyancy()
		{
			buoyancyCurrentAngle = Mathf.Sin(time) * VGOBuoyancyAmplitude * Mathf.Rad2Deg;
		}
	}
}