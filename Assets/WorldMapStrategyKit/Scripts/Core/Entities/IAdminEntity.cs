using System.Collections.Generic;
using UnityEngine;

namespace WorldMapStrategyKit
{
	public interface IAdminEntity
	{
		/// <summary>
		/// Entity name.
		/// </summary>
		string name { get; set; }

		/// <summary>
		/// List of all regions for the admin entity.
		/// </summary>
		List<Region> regions { get; set; }

		/// <summary>
		/// Center of the admin entity in the plane
		/// </summary>
		Vector2 center { get; set; }

		int mainRegionIndex { get; set; }

		/// <summary>
		/// Returns the region object which is the main region of the country
		/// </summary>
		Region mainRegion { get; }

		/// <summary>
		/// Computed Rect area that includes all regions. Used to fast hovering.
		/// </summary>
		Rect regionsRect2D { get; set; }

		/// <summary>
		/// Computed Rect area that includes all regions. Used to fast hovering.
		/// </summary>
		float regionsRect2DArea { get; }

		/// <summary>
		/// An unique identifier useful to persist data between sessions. Used by serialization.
		/// </summary>
		int uniqueId { get; set; }

		/// <summary>
		/// Use this property to add/retrieve custom attributes for this country
		/// </summary>
		JSONObject attrib { get; set; }

		/// <summary>
		/// Used by pathfinding in Country or Province modes to determine if route can cross a country/province. Defaults to true.
		/// </summary>
		/// <value><c>true</c> if can cross; otherwise, <c>false</c>.</value>
		bool canCross { get; set; }

		/// <summary>
		/// Used by pathfinding in Country or Province modes. Cost for crossing a country/province. Defaults to 1.
		/// </summary>
		float crossCost { get; set; }

		/// <summary>
		/// Custom array of countries/provinces that could be reached from this country. Useful for Country/Province path-finding.
		/// It defaults to natural neighbours of the country/province but you can modify its contents and add your own potential destinations per country/province.
		/// </summary>
		int[] neighbours { get; set; }

		/// <summary>
		/// Sets the entity hidden (borders won't appear and it won't be interactable)
		/// </summary>
		bool hidden { get; set; }

		/// <summary>
		/// Used internally by Editor.
		/// </summary>
		bool foldOut { get; set; }

		/// <summary>
		/// If this entity can be highlighted.
		/// </summary>
		bool allowHighlight { get; set; }
	}
}