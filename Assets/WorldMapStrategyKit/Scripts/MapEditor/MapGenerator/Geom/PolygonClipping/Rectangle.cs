namespace WorldMapStrategyKit.MapGenerator.Geom
{
	public class Rectangle
	{
		public double minX, minY, width, height;

		public Rectangle(double minX, double minY, double width, double height)
		{
			this.minX = minX;
			this.minY = minY;
			this.width = width;
			this.height = height;
		}

		public double right => minX + width;

		public double top => minY + height;

		public Rectangle Union(Rectangle o)
		{
			var minX = this.minX < o.minX + Point.PRECISION ? this.minX : o.minX;
			var maxX = right > o.right - Point.PRECISION ? right : o.right;
			var minY = this.minY < o.minY + Point.PRECISION ? this.minY : o.minY;
			var maxY = top > o.top - Point.PRECISION ? top : o.top;
			return new Rectangle(minX, minY, maxX - minX, maxY - minY);
		}

		public bool Intersects(Rectangle o)
		{
			if (o.minX > right + Point.PRECISION)
				return false;
			if (o.right < minX - Point.PRECISION)
				return false;
			if (o.minY > top + Point.PRECISION)
				return false;
			if (o.top < minY - Point.PRECISION)
				return false;
			return true;
		}

		public override string ToString() => string.Format("minX:" +
		                                                   minX.ToString("F5") +
		                                                   " minY:" +
		                                                   minY.ToString("F5") +
		                                                   " width:" +
		                                                   width.ToString("F5") +
		                                                   " height:" +
		                                                   height.ToString("F5"));
	}
}