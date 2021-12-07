using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace WorldMapStrategyKit
{
	public static class WMSKGameObjectExtensions
	{
		/// <summary>
		/// Smoothly moves this game object to given map position with duration in seconds.
		/// </summary>
		/// <returns>The GameObjectAnimator component.</returns>
		public static GameObjectAnimator WMSK_MoveTo(this GameObject o, float x, float y) =>
			WMSK_MoveTo(o, new Vector2(x, y));

		/// <summary>
		/// Smoothly moves this game object to given map position with duration in seconds.
		/// <param name="durationType">Step: each step will take the same duration, Route: the given duration is for the entire route, MapLap: the duration is the time to cross entire map. Default duration type is 'Step'. Use 'MapLap' if you pass a custom set of non-continuous points to ensure a consistent speed of movement.</param> 
		/// </summary>
		/// <returns>The GameObjectAnimator component.</returns>
		public static GameObjectAnimator WMSK_MoveTo(this GameObject o, float x, float y, float duration,
			DURATION_TYPE durationType = DURATION_TYPE.Step) =>
			WMSK_MoveTo(o, new Vector2(x, y), duration, durationType);

		/// <summary>
		/// Makes a placeholder object children of map. Useful if you want to organize other units under a common parent. This gameobject is just repositioned on the center of the map.
		/// This is a convenient function. Same results can be obtained calling WMSK_MoveTo(Vector2.zero, 0, false);
		/// </summary>
		/// <returns>The GameObjectAnimator component.</returns>
		public static GameObjectAnimator WMSK_MakeChild(this GameObject o) =>
			WMSK_MoveTo(o, Vector2.zero, 0, DURATION_TYPE.Step, false);

		/// <summary>
		/// Smoothly moves this game object to given map position with options.
		/// </summary>
		/// <returns>The GameObjectAnimator component.</returns>
		/// <param name="destination">destination in -0.5 ... 0.5 range for X/Y coordinates</param>
		/// <param name="scaleOnZoom">If set to <c>true</c> the gameobject will increase/decrease its scale when zoomin in/out.</param>
		public static GameObjectAnimator WMSK_MoveTo(this GameObject o, Vector2 destination,
			bool scaleOnZoom, float altitude = 0) =>
			WMSK_MoveTo(o, destination, 0, DURATION_TYPE.Step, scaleOnZoom, altitude);

		/// <summary>
		/// Smoothly moves this game object to given map position with options.
		/// </summary>
		/// <returns>The GameObjectAnimator component.</returns>
		/// <param name="destination">destination in -0.5 ... 0.5 range for X/Y coordinates</param>
		/// <param name="duration">duration in seconds</param>
		/// <param name="durationType">Step: each step will take the same duration, Route: the given duration is for the entire route, MapLap: the duration is the time to cross entire map. Default duration type is 'Step'. Use 'MapLap' if you pass a custom set of non-continuous points to ensure a consistent speed of movement.</param> 
		/// <param name="scaleOnZoom">If set to <c>true</c> the gameobject will increase/decrease its scale when zoomin in/out.</param>
		public static GameObjectAnimator WMSK_MoveTo(this GameObject o, Vector2 destination,
			float duration = 0, DURATION_TYPE durationType = DURATION_TYPE.Step, bool scaleOnZoom = true,
			float altitude = 0)
		{
			var anim = o.GetComponent<GameObjectAnimator>() ?? o.AddComponent<GameObjectAnimator>();
			if (altitude == 0)
				anim.heightMode = HEIGHT_OFFSET_MODE.RELATIVE_TO_GROUND;
			else
			{
				anim.heightMode = HEIGHT_OFFSET_MODE.ABSOLUTE_CLAMPED;
				anim.altitude = altitude;
			}
			anim.autoScale = scaleOnZoom;
			anim.MoveTo(destination, duration, durationType);
			return anim;
		}

		/// <summary>
		/// Smoothly moves this game object to given map destination along route of points.
		/// </summary>
		/// <param name="durationType">Step: each step will take the same duration, Route: the given duration is for the entire route, MapLap: the duration is the time to cross entire map. Default duration type is 'Step'. Use 'MapLap' if you pass a custom set of non-continuous points to ensure a consistent speed of movement.</param> 
		public static GameObjectAnimator WMSK_MoveTo(this GameObject o, List<Vector2> route,
			float duration, DURATION_TYPE durationType = DURATION_TYPE.Step)
		{
			var anim = o.GetComponent<GameObjectAnimator>() ?? o.AddComponent<GameObjectAnimator>();
			anim.MoveTo(route, duration, durationType);
			return anim;
		}

		public static Vector2 WMSK_GetMap2DPosition(this GameObject o)
		{
			var anim = o.GetComponent<GameObjectAnimator>() ?? o.AddComponent<GameObjectAnimator>();
			return anim.currentMap2DLocation;
		}

		public static List<Vector2> WMSK_FindRoute(this GameObject o, Vector2 destination)
		{
			var anim = o.GetComponent<GameObjectAnimator>() ?? o.AddComponent<GameObjectAnimator>();
			return anim.FindRoute(destination);
		}

		public static void WMSK_LookAt(this GameObject o, Vector2 destination)
		{
			var anim = o.GetComponent<GameObjectAnimator>();
			if (anim == null)
				return;
			anim.LookAt(destination);
		}

		/// <summary>
		/// Fires a bullet, cannon-ball, missile, etc.
		/// </summary>
		/// <param name="bullet">Bullet. You must supply your own bullet gameobject.</param>
		/// <param name="startAnchor">Start anchor. Where does the bullet appear? This vector3 is expressed in local coordinates of the firing unit and ignores its scale.</param>
		/// <param name="destination">Destination. Target 2d map coordinates.</param>
		/// <param name="duration">Duration for the bullet to reach its destination..</param>
		/// <param name="arcMultiplier">Pass a value greater than 1 to produce a parabole.</param>
		/// <param name="delay">Firing delay. Gives time to the unit so it can orient to target.</param>
		/// <param name="orientToTarget">Orient to target before firing.</param>
		/// <param name="testAnchor">Drops the bullet at the start of the trajectory. Useful to check the anchor is correct.</param>
		public static GameObjectAnimator WMSK_Fire(this GameObject o, GameObject bullet,
			Vector3 startAnchor, Vector2 destination, float bulletSpeed, float arcHeight = 1f,
			float delay = 1f, bool orientToTarget = true, bool testAnchor = false)
		{
			var goAnim = o.GetComponent<GameObjectAnimator>();
			if (goAnim == null)
				return null;
			if (orientToTarget)
				goAnim.LookAt(destination);
			return goAnim.Fire(delay, bullet, startAnchor, destination, bulletSpeed, arcHeight,
				testAnchor);
		}
	}
}