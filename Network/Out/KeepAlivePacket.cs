using System.Text.Json.Serialization;

namespace HypeRate.Network.Out;

public class KeepAlivePacket
{
	[JsonInclude] [JsonPropertyName("event")]
	public string Event = "heartbeat";

	[JsonInclude] [JsonPropertyName("payload")]
	public Dictionary<string, string> Payload = new();

	[JsonInclude] [JsonPropertyName("ref")]
	public int Ref;

	[JsonInclude] [JsonPropertyName("topic")]
	public string Topic = "phoenix";
}
