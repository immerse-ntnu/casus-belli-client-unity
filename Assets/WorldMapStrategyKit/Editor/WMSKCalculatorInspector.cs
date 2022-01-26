using System.Text;
using UnityEditor;
using UnityEngine;

namespace WorldMapStrategyKit
{
	[CustomEditor(typeof(WMSK_Calculator))]
	public class WMSK_CalculatorInspector : Editor
	{
		// units
		private string[] unitNames;
		private UNIT_TYPE fromUnit = UNIT_TYPE.Degrees;
		private UNIT_TYPE toUnit = UNIT_TYPE.DecimalDegrees;

		// UI variables (need to be string)
		private string fromLatDegree = "";
		private string fromLatMinute = "";

		private string fromLatSeconds = "";

		// From: longitude (degree)
		private string fromLonDegree = "";
		private string fromLonMinute = "";

		private string fromLonSeconds = "";

		// From: decimal degrees
		private string fromLatDec = "", fromLonDec = "";

		// From: spherical coordinates
		private string fromX = "", fromY = "";

		// To: latitude (degree)
		private string toLatDegree = "";
		private string toLatMinute = "";

		private string toLatSeconds = "";

		// To: longitude (degree)
		private string toLonDegree = "";
		private string toLonMinute = "";

		private string toLonSeconds = "";

		// To: decimal degrees
		private string toLatDec = "", toLonDec = "";

		// To: spherical coordinates
		private string toX = "", toY = "";

		// Other utility variables
		private string errorMsg;
		private WMSK_Calculator _calc;
		private int lastCityCount = -1;
		private string[] _cityNames;

		private string[] cityNames
		{
			get
			{
				if (_calc.map != null && lastCityCount != _calc.map.cities.Length)
				{
					_cityNames = _calc.map.GetCityNames();
					lastCityCount = _cityNames.Length;
				}
				return _cityNames;
			}
		}

		private void OnEnable()
		{
			unitNames = new[]
			{
				"Degrees",
				"Decimal Degrees",
				"Plane Coordinates"
			};
			errorMsg = "";
			_calc = (WMSK_Calculator)target;
		}

		public override void OnInspectorGUI()
		{
			if (_calc == null)
				return;

			var runConversion = false;
			var runCalcDistance = false;

			EditorGUILayout.Separator();
			EditorGUILayout.BeginVertical();

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Convert From", GUILayout.Width(120));
			var oldUnit = fromUnit;
			fromUnit = (UNIT_TYPE)EditorGUILayout.Popup((int)fromUnit, unitNames, GUILayout.MaxWidth(200));
			if (fromUnit != oldUnit)
				runConversion = true;
			EditorGUILayout.EndHorizontal();

			switch (fromUnit)
			{
				case UNIT_TYPE.Degrees:
					EditorGUILayout.BeginHorizontal();
					GUILayout.Label("   Latitude", GUILayout.Width(120));
					fromLatDegree = GUILayout.TextField(fromLatDegree, GUILayout.Width(40));
					GUILayout.Label("°", GUILayout.Width(10));
					fromLatMinute = GUILayout.TextField(fromLatMinute, GUILayout.Width(40));
					GUILayout.Label("'", GUILayout.Width(10));
					fromLatSeconds = GUILayout.TextField(fromLatSeconds, GUILayout.Width(80));
					GUILayout.Label("''", GUILayout.Width(10));
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.BeginHorizontal();
					GUILayout.Label("   Longitude", GUILayout.Width(120));
					fromLonDegree = GUILayout.TextField(fromLonDegree, GUILayout.Width(40));
					GUILayout.Label("°", GUILayout.Width(10));
					fromLonMinute = GUILayout.TextField(fromLonMinute, GUILayout.Width(40));
					GUILayout.Label("'", GUILayout.Width(10));
					fromLonSeconds = GUILayout.TextField(fromLonSeconds, GUILayout.Width(80));
					GUILayout.Label("''", GUILayout.Width(10));
					EditorGUILayout.EndHorizontal();
					break;
				case UNIT_TYPE.DecimalDegrees:
					EditorGUILayout.BeginHorizontal();
					GUILayout.Label("   Latitude", GUILayout.Width(120));
					fromLatDec = GUILayout.TextField(fromLatDec, GUILayout.Width(80));
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.BeginHorizontal();
					GUILayout.Label("   Longitude", GUILayout.Width(120));
					fromLonDec = GUILayout.TextField(fromLonDec, GUILayout.Width(80));
					EditorGUILayout.EndHorizontal();
					break;
				case UNIT_TYPE.PlaneCoordinates:
					EditorGUILayout.BeginHorizontal();
					GUILayout.Label("   X", GUILayout.Width(120));
					fromX = GUILayout.TextField(fromX, GUILayout.Width(100));
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.BeginHorizontal();
					GUILayout.Label("   Y", GUILayout.Width(120));
					fromY = GUILayout.TextField(fromY, GUILayout.Width(100));
					EditorGUILayout.EndHorizontal();

					EditorGUILayout.BeginHorizontal();
					GUILayout.Label("   Capture Cursor", GUILayout.Width(120));
					_calc.captureCursor = EditorGUILayout.Toggle(_calc.captureCursor, GUILayout.Width(20));
					var warningLabelStyle = new GUIStyle(GUI.skin.label);
					warningLabelStyle.normal.textColor = new Color(0.31f, 0.38f, 0.56f);
					if (!Application.isPlaying)
						GUILayout.Label("(not available in Edit mode)", warningLabelStyle);
					else if (_calc.captureCursor)
						GUILayout.Label("(press C to capture)", warningLabelStyle);
					EditorGUILayout.EndHorizontal();
					break;
			}

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Convert To", GUILayout.Width(120));
			oldUnit = toUnit;
			toUnit = (UNIT_TYPE)EditorGUILayout.Popup((int)toUnit, unitNames, GUILayout.MaxWidth(200));
			if (oldUnit != toUnit)
				runConversion = true;
			EditorGUILayout.EndHorizontal();

			switch (toUnit)
			{
				case UNIT_TYPE.Degrees:
					EditorGUILayout.BeginHorizontal();
					GUILayout.Label("   Latitude", GUILayout.Width(120));
					toLatDegree = GUILayout.TextField(toLatDegree, GUILayout.Width(40));
					GUILayout.Label("°", GUILayout.Width(10));
					toLatMinute = GUILayout.TextField(toLatMinute, GUILayout.Width(40));
					GUILayout.Label("'", GUILayout.Width(10));
					toLatSeconds = GUILayout.TextField(toLatSeconds, GUILayout.Width(80));
					GUILayout.Label("''", GUILayout.Width(10));
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.BeginHorizontal();
					GUILayout.Label("   Longitude", GUILayout.Width(120));
					toLonDegree = GUILayout.TextField(toLonDegree, GUILayout.Width(40));
					GUILayout.Label("°", GUILayout.Width(10));
					toLonMinute = GUILayout.TextField(toLonMinute, GUILayout.Width(40));
					GUILayout.Label("'", GUILayout.Width(10));
					toLonSeconds = GUILayout.TextField(toLonSeconds, GUILayout.Width(80));
					GUILayout.Label("''", GUILayout.Width(10));
					EditorGUILayout.EndHorizontal();
					break;
				case UNIT_TYPE.DecimalDegrees:
					EditorGUILayout.BeginHorizontal();
					GUILayout.Label("   Latitude", GUILayout.Width(120));
					toLatDec = GUILayout.TextField(toLatDec, GUILayout.Width(80));
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.BeginHorizontal();
					GUILayout.Label("   Longitude", GUILayout.Width(120));
					toLonDec = GUILayout.TextField(toLonDec, GUILayout.Width(80));
					EditorGUILayout.EndHorizontal();
					break;
				case UNIT_TYPE.PlaneCoordinates:
					EditorGUILayout.BeginHorizontal();
					GUILayout.Label("   X", GUILayout.Width(120));
					toX = GUILayout.TextField(toX, GUILayout.Width(100));
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.BeginHorizontal();
					GUILayout.Label("   Y", GUILayout.Width(120));
					toY = GUILayout.TextField(toY, GUILayout.Width(100));
					EditorGUILayout.EndHorizontal();
					break;
			}

			if (errorMsg.Length > 0)
			{
				var warningLabelStyle = new GUIStyle(GUI.skin.label);
				warningLabelStyle.normal.textColor = new Color(0.31f, 0.38f, 0.56f);
				GUILayout.Label("Conversion error: ", errorMsg);
			}

			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Convert", GUILayout.Width(120)))
				runConversion = true;

			if (GUILayout.Button("Copy to ClipBoard", GUILayout.Width(120)))
			{
				var sb = new StringBuilder();
				switch (toUnit)
				{
					case UNIT_TYPE.DecimalDegrees:
						sb.Append("Latitude (decimal degrees): ");
						sb.AppendLine(toLatDec);
						sb.Append("Longitude (decimal degrees): ");
						sb.AppendLine(toLonDec);
						break;
					case UNIT_TYPE.Degrees:
						sb.Append("Latitude (degrees): ");
						sb.Append(toLatDegree);
						sb.Append("°");
						sb.Append(toLatMinute);
						sb.Append("'");
						sb.Append(toLatSeconds);
						sb.AppendLine("''");
						sb.Append("Longitude (degrees): ");
						sb.Append(toLonDegree);
						sb.Append("°");
						sb.Append(toLonMinute);
						sb.Append("'");
						sb.Append(toLonSeconds);
						sb.AppendLine("''");
						break;
					case UNIT_TYPE.PlaneCoordinates:
						sb.Append("X: ");
						sb.AppendLine(toX);
						sb.Append("Y: ");
						sb.AppendLine(toY);
						break;
				}
				EditorGUIUtility.systemCopyBuffer = sb.ToString();
			}
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			EditorGUILayout.EndVertical();
			EditorGUILayout.Separator();

			EditorGUILayout.BeginVertical();
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Distance From", GUILayout.Width(120));
			var prev = _calc.cityDistanceFrom;
			_calc.cityDistanceFrom =
				EditorGUILayout.Popup(_calc.cityDistanceFrom, cityNames, GUILayout.MaxWidth(200));
			if (_calc.cityDistanceFrom != prev)
				runCalcDistance = true;
			GUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Distance To", GUILayout.Width(120));
			prev = _calc.cityDistanceTo;
			_calc.cityDistanceTo =
				EditorGUILayout.Popup(_calc.cityDistanceTo, cityNames, GUILayout.MaxWidth(200));
			if (_calc.cityDistanceTo != prev)
				runCalcDistance = true;
			GUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("", GUILayout.Width(120));
			GUILayout.TextField(_calc.cityDistanceResult, GUILayout.Width(100));
			GUILayout.Label("km", GUILayout.Width(20));
			GUILayout.EndHorizontal();

			EditorGUILayout.EndVertical();

			if (runConversion)
				DoConvert();

			if (runCalcDistance)
				DoCalcDistance();

			if (_calc.isDirty)
			{
				GetResults();
				_calc.isDirty = false;
				EditorUtility.SetDirty(target);
			}
		}

		private void DoConvert()
		{
			// Setup "from" parameters
			_calc.fromUnit = fromUnit;
			_calc.fromLatDec = GetFloat(fromLatDec);
			_calc.fromLatDegrees = GetFloat(fromLatDegree);
			_calc.fromLatMinutes = GetInt(fromLatMinute);
			_calc.fromLatSeconds = GetFloat(fromLatSeconds);
			_calc.fromLonDec = GetFloat(fromLonDec);
			_calc.fromLonDegrees = GetFloat(fromLonDegree);
			_calc.fromLonMinutes = GetInt(fromLonMinute);
			_calc.fromLonSeconds = GetFloat(fromLonSeconds);
			_calc.fromX = GetFloat(fromX);
			_calc.fromY = GetFloat(fromY);
			// Do conversion
			_calc.Convert();
			GetResults();
		}

		private void GetResults()
		{
			// Recover results
			errorMsg = _calc.errorMsg;
			fromX = _calc.fromX.ToString();
			fromY = _calc.fromY.ToString();
			toLatDec = _calc.toLatDec.ToString();
			toLatDegree = _calc.toLatDegree.ToString();
			toLatMinute = _calc.toLatMinute.ToString();
			toLatSeconds = _calc.toLatSeconds.ToString();
			toLonDec = _calc.toLonDec.ToString();
			toLonDegree = _calc.toLonDegree.ToString();
			toLonMinute = _calc.toLonMinute.ToString();
			toLonSeconds = _calc.toLonSecond.ToString();
			toX = _calc.toX.ToString();
			toY = _calc.toY.ToString();
		}

		public int GetInt(string value)
		{
			var intValue = 0;
			int.TryParse(value, out intValue);
			return intValue;
		}

		public float GetFloat(string value)
		{
			float floatValue = 0;
			float.TryParse(value, out floatValue);
			return floatValue;
		}

		private int GetCityindex(string s)
		{
			var k = s.IndexOf("(");
			var j = s.LastIndexOf(")");
			var i = -1;
			if (k > 0 && j > k)
				int.TryParse(s.Substring(k + 1, j - k - 1), out i);
			return i;
		}

		private void DoCalcDistance()
		{
			_calc.cityDistanceResult = "";
			if (_calc.cityDistanceFrom >= 0 && _calc.cityDistanceTo >= 0)
			{
				var c1 = GetCityindex(cityNames[_calc.cityDistanceFrom]);
				var c2 = GetCityindex(cityNames[_calc.cityDistanceTo]);
				if (c1 >= 0 && c2 >= 0)
				{
					var city1 = _calc.map.cities[c1];
					var city2 = _calc.map.cities[c2];
					var distance = _calc.Distance(city1, city2) / 1000;
					_calc.cityDistanceResult = distance.ToString("F3");
				}
			}
		}
	}
}