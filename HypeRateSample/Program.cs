using System;
using System.Threading;
using System.Threading.Tasks;
using HypeRate;
using HypeRate.EventArgs;

namespace HypeRateSample
{
    internal static class Program
    {
        private static int _lastHeartbeat = 0;
        private static readonly object _lock = new();
        private static bool _running = true;

        public static async Task Main(string[] _)
        {
            Console.WriteLine("Please enter your HypeRate API Token:");
            string? apiToken = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(apiToken))
            {
                Console.WriteLine("API Token is required!");
                return;
            }

            Console.WriteLine("Please enter your HypeRate Device ID:");
            string? deviceId = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                Console.WriteLine("Device ID is required!");
                return;
            }

            var hypeRate = HypeRate.HypeRate.GetInstance();
            hypeRate.SetApiToken(apiToken);
            hypeRate.HeartbeatReceived += OnHeartbeatReceived;
            hypeRate.Start();
            await hypeRate.Connect();
            await hypeRate.JoinHeartbeatChannel(deviceId, null);

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
                await hypeRate.LeaveHeartbeatChannel(deviceId, null);
                await hypeRate.Disconnect();
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
