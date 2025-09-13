namespace HypeRate.EventArgs;

public class HeartbeatReceivedEventArgs : System.EventArgs
{
	public readonly string Device;
	public readonly int Heartbeat;

	public HeartbeatReceivedEventArgs(string device, int heartbeat)
	{
		Device = device;
		Heartbeat = heartbeat;
	}
}
