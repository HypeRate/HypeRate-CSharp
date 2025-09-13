using System.Text.Json.Serialization;

namespace HypeRate.Network.Out;

public class LeaveChannelPacket
{
	[JsonInclude] [JsonPropertyName("event")]
	public string Event = "phx_leave";

	[JsonInclude] [JsonPropertyName("payload")]
	public Dictionary<string, string> Payload = new();

	[JsonInclude] [JsonPropertyName("ref")]
	public int Ref;

	[JsonInclude] [JsonPropertyName("topic")]
	public string Topic;

	public LeaveChannelPacket(string topic, int @ref)
	{
		Topic = topic;
		Ref = @ref;
	}
}
