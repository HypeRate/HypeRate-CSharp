using HypeRate;
using HypeRate.EventArgs;

namespace HypeRateSample
{
    internal static class Program
    {
        private static bool _running = true;

        public static async Task Main(string[] _)
        {
            HypeRate.HypeRate.GetInstance().Start();
            string? apiToken;

            while (true)
            {
                Console.WriteLine("Please enter your HypeRate API Token:");
                apiToken = Console.ReadLine();
                apiToken = apiToken?.Trim();

                if (string.IsNullOrEmpty(apiToken))
                {
                    Console.WriteLine("API Token is required!");
                    continue;
                }

                HypeRate.HypeRate.GetInstance().SetApiToken(apiToken);

                try
                {
                    await HypeRate.HypeRate.GetInstance().Connect();

                    if (HypeRate.HypeRate.GetInstance().IsConnected)
                    {
                        break;
                    }

                    Console.WriteLine("API Token invalid or connection failed. Please try again.\n");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Connection failed: {ex.Message}\nPlease try again.\n");
                }
            }

            string? deviceId;

            while (true)
            {
                Console.WriteLine("Please enter your HypeRate Device ID:");

                deviceId = Console.ReadLine();
                deviceId = deviceId?.Trim();


                if (string.IsNullOrEmpty(deviceId))
                {
                    Console.WriteLine("Device ID is required!");
                    continue;
                }

                if (Device.IsValidDeviceId(deviceId) == false)
                {
                    Console.WriteLine("Device ID is not valid. Please read the documentation: https://github.com/HypeRate/DevDocs/blob/main/Device%20ID.md");

                    continue;
                }

                break;
            }

            // Register the callback which getse called when a new heartbeat was received
            HypeRate.HypeRate.GetInstance().HeartbeatReceived += OnHeartbeatReceived;

            // Join the channel so we get updates for the provided ID
            await HypeRate.HypeRate.GetInstance().JoinHeartbeatChannel(deviceId, null);

            Console.Clear();
            Console.WriteLine("Heart rate monitor running. Press ESC or Ctrl+C to exit.");

            while (_running)
            {
                var key = Console.ReadKey(true);

                if (key.Key != ConsoleKey.Escape)
                {
                    continue;
                }

                break;
            }

            // Leave the heartbeat channel so we don't get any further updates for the specified device ID
            await HypeRate.HypeRate.GetInstance().LeaveHeartbeatChannel(deviceId, null);

            // Disconnect from the HypeRate server
            await HypeRate.HypeRate.GetInstance().Disconnect();
            Console.WriteLine("Exited");
        }

        private static void OnHeartbeatReceived(object? sender, HeartbeatReceivedEventArgs e)
        {
            // Print the current heart rate
            // This can also be 0 if the device is not (properly) positioned against the user
            Console.WriteLine($"Current heart rate: {e.Heartbeat}");
        }
    }
}
