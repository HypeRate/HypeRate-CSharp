using System.Text.Json.Serialization;

namespace HypeRate.Network.In;

public class IncomingClipCreatedPacket
{
	[JsonInclude] [JsonPropertyName("payload")]
	public IncomingClipCreatedPacketPayload? Payload;

	public class IncomingClipCreatedPacketPayload
	{
		[JsonInclude] [JsonPropertyName("twitch_slug")]
		public string? TwitchSlug;
	}
}
