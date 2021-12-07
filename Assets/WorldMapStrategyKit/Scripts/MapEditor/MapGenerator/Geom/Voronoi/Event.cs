using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace WorldMapStrategyKit.MapGenerator.Geom
{
	internal enum EVENT_TYPE
	{
		SiteEvent = 0,
		CircleEvent = 1
	}

	internal class Event
	{
		public double x;
		public Point p;
		public Arc a;
		public EVENT_TYPE type;
		public bool valid;
		public VoronoiCell cell;

		public Event(EVENT_TYPE type)
		{
			valid = true;
			this.type = type;
		}

		public Event(EVENT_TYPE type, double x, Point p, Arc a) : this(type)
		{
			this.x = x;
			this.p = p;
			this.a = a;
		}
	}
}