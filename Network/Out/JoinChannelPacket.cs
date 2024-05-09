using System.Text.Json.Serialization;

namespace HypeRate.Network.Out;

public class JoinChannelPacket
{
	[JsonInclude] [JsonPropertyName("event")]
	public string Event = "phx_join";

	[JsonInclude] [JsonPropertyName("payload")]
	public Dictionary<string, string> Payload = new();

	[JsonInclude] [JsonPropertyName("ref")]
	public int Ref;

	[JsonInclude] [JsonPropertyName("topic")]
	public string Topic;

	public JoinChannelPacket(string topic, int @ref)
	{
		Topic = topic;
		Ref = @ref;
	}
}
