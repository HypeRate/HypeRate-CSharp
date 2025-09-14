using System;
using System.Threading;
using System.Threading.Tasks;
using HypeRate;
using HypeRate.EventArgs;

namespace HypeRateSample
{
    internal static class Program
    {
        private static bool _running = true;
        private static int _lastHeartbeat = 0;
        private static readonly object _lock = new();

        public static async Task Main(string[] _)
        {
            HypeRate.HypeRate.GetInstance().Start();
            string? apiToken;

            while (true)
            {
                Console.WriteLine("Please enter your HypeRate API Token:");
                apiToken = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(apiToken))
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
                    else
                    {
                        Console.WriteLine("API Token invalid or connection failed. Please try again.\n");
                    }
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
                if (string.IsNullOrWhiteSpace(deviceId))
                {
                    Console.WriteLine("Device ID is required!");
                    continue;
                }
                break;
            }

            HypeRate.HypeRate.GetInstance().HeartbeatReceived += OnHeartbeatReceived;
            await HypeRate.HypeRate.GetInstance().JoinHeartbeatChannel(deviceId, null);

            Console.Clear();
            Console.WriteLine("Heart rate monitor running. Press ESC or Ctrl+C to exit.");
            Console.WriteLine("Current heart rate: --");

            // Thread for key input to exit
            var inputThread = new Thread(() =>
            {
                while (_running)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true);
                        if (key.Key == ConsoleKey.Escape)
                        {
                            _running = false;
                            break;
                        }
                    }
                    Thread.Sleep(100);
                }
            })
            {
                IsBackground = true
            };
            inputThread.Start();

            try
            {
                while (_running)
                {
                    int heartbeat;
                    lock (_lock)
                    {
                        heartbeat = _lastHeartbeat;
                    }
                    try
                    {
                        Console.SetCursorPosition(0, 1);
                        Console.Write($"Current heart rate: {(heartbeat > 0 ? heartbeat.ToString() : "--")}   ");
                    }
                    catch { /* Ignore cursor errors */ }
                    await Task.Delay(500);
                }
            }
            finally
            {
                await HypeRate.HypeRate.GetInstance().LeaveHeartbeatChannel(deviceId, null);
                await HypeRate.HypeRate.GetInstance().Disconnect();
                Console.WriteLine("\nExited.");
            }
        }

        private static void OnHeartbeatReceived(object? sender, HeartbeatReceivedEventArgs e)
        {
            lock (_lock)
            {
                _lastHeartbeat = e.Heartbeat;
            }
        }
    }
}
