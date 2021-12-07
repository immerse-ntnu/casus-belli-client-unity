using UnityEngine;
using UnityEditor;
using System.Collections;

namespace WorldMapStrategyKit
{
	public static class WMSKEditorStyles
	{
		public static void SetFoldoutColor(this GUIStyle style, Color foldoutColor)
		{
			style.normal.textColor = foldoutColor;
			style.onNormal.textColor = foldoutColor;
			style.hover.textColor = foldoutColor;
			style.onHover.textColor = foldoutColor;
			style.focused.textColor = foldoutColor;
			style.onFocused.textColor = foldoutColor;
			style.active.textColor = foldoutColor;
			style.onActive.textColor = foldoutColor;
			style.fontStyle = FontStyle.Bold;
		}
	}
}