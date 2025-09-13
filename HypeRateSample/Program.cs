using System;
using System.Threading;
using HypeRate;
using HypeRate.EventArgs;

class Program
{
    private static int _lastHeartbeat = 0;
    private static readonly object _lock = new();

    static void Main(string[] args)
    {
        Console.WriteLine("Bitte gib deinen HypeRate Device ID ein:");
        string deviceId = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            Console.WriteLine("Device ID ist erforderlich!");
            return;
        }

        Console.WriteLine("Bitte gib deinen HypeRate API Token ein:");
        string apiToken = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(apiToken))
        {
            Console.WriteLine("API Token ist erforderlich!");
            return;
        }

        HypeRate.HypeRate;
        hypeRate.SetApiToken(apiToken);
        hypeRate.HeartbeatReceived += OnHeartbeatReceived;
        hypeRate.Start();
        hypeRate.Connect();
        hypeRate.JoinHeartbeatChannel(deviceId);

        Console.Clear();
        Console.WriteLine("Herzfrequenz Monitor läuft. Drücke Strg+C zum Beenden.");

        while (true)
        {
            int heartbeat;
            lock (_lock)
            {
                heartbeat = _lastHeartbeat;
            }
            Console.SetCursorPosition(0, 1);
            Console.Write($"Aktuelle Herzfrequenz: {heartbeat}   ");
            Thread.Sleep(500);
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
