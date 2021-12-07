namespace WorldMapStrategyKit
{
	public interface IExtendableAttribute
	{
		int uniqueId { get; set; }

		JSONObject attrib { get; set; }
	}
}