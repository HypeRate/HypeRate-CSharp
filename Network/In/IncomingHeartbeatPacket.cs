using System.Text.Json.Serialization;

namespace HypeRate.Network.In;

public class IncomingHeartbeatPacket
{
	[JsonInclude] [JsonPropertyName("payload")]
	public IncomingHeartbeatPacketPayload? Payload;

	public class IncomingHeartbeatPacketPayload
	{
		[JsonInclude] [JsonPropertyName("hr")] public int? Hr;
	}
}
