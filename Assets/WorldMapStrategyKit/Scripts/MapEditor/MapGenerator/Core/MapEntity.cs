namespace WorldMapStrategyKit
{
	public interface MapEntity
	{
		string name { get; set; }
		MapRegion region { get; set; }
		bool visible { get; set; }
		bool hasCapital { get; set; }
	}
}