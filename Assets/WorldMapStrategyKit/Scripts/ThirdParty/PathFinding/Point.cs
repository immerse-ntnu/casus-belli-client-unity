namespace WorldMapStrategyKit.PathFinding
{
	public struct Point
	{
		public int X;
		public int Y;

		public Point(int x, int y)
		{
			X = x;
			Y = y;
		}

		// For debugging
		public override string ToString() => string.Format("{0}, {1}", X, Y);
	}
}