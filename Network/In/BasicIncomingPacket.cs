using System.Text.Json.Serialization;

namespace HypeRate.Network.In;

public class BasicIncomingPacket
{
	[JsonInclude] [JsonPropertyName("event")]
	public string? Event;

	[JsonInclude] [JsonPropertyName("ref")]
	public int? Ref;

	[JsonInclude] [JsonPropertyName("topic")]
	public string? Topic;

	public bool IsSystemPacket => Event == "phx_reply";

	public bool IsHeartbeatPacket => Event == "hr_update";

	public bool IsClipsPacket => Event == "clip:created";

	public override string ToString()
	{
		return
			$"{nameof(Event)}: {Event}, {nameof(Ref)}: {Ref}, {nameof(Topic)}: {Topic}, {nameof(IsSystemPacket)}: {IsSystemPacket}, {nameof(IsHeartbeatPacket)}: {IsHeartbeatPacket}, {nameof(IsClipsPacket)}: {IsClipsPacket}";
	}
}
