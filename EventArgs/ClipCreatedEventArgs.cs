namespace HypeRate.EventArgs;

public class ClipCreatedEventArgs : System.EventArgs
{
	public readonly string Device;
	public readonly string TwitchSlug;

	public ClipCreatedEventArgs(string device, string twitchSlug)
	{
		Device = device;
		TwitchSlug = twitchSlug;
	}
}
