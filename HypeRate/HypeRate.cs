using HypeRate.EventArgs;
using HypeRate.Network.In;
using HypeRate.Network.Out;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HypeRate;

/// <summary>
///     Exposes the public API for interacting with the HypeRate WebSocket API
/// </summary>
public class HypeRate
{
    /// <summary>
    ///     Holds the global unique instance of the current class
    /// </summary>
    private static readonly HypeRate Instance = new();

    private readonly Task _heartbeatTask;

    private readonly Task _receiveTask;

    /// <summary>
    ///     The internal channel manager which is used for managing channels
    /// </summary>
    private volatile ChannelManager _channelManager = new();

    /// <summary>
    ///     Holds an instance of the built-in Websocket client
    /// </summary>
    private volatile ClientWebSocket _websocketClient = new();

    /// <summary>
    ///     The base URL to which the Websocket client should connect to.
    ///     Normally you wouldn't change this value - otherwise the WebSocket client could not establish a connection.
    ///     Only change this if you are asked to by the HypeRate team.
    /// </summary>
    public string BaseUrl { get; set; } = "wss://app.hyperate.io/socket/websocket";

    /// <summary>
    ///     Constructs a new instance of the HypeRate class
    /// </summary>
    private HypeRate()
    {
        _heartbeatTask = new Task(async () =>
        {
            while (true)
            {
                if (_websocketClient.State == WebSocketState.Open) await SendPacket(GetKeepAlivePacket(), default);

                await Task.Delay(10 * 1000);
            }
        });

        _receiveTask = new Task(async () =>
        {
            while (true)
            {
                if (_websocketClient.State == WebSocketState.Open)
                {
                    var buffer = new ArraySegment<byte>(new byte[1024]);
                    WebSocketReceiveResult result;
                    var allBytes = new List<byte>();

                    do
                    {
                        try
                        {
                            result = await _websocketClient.ReceiveAsync(buffer, CancellationToken.None);

                            if (_websocketClient.State == WebSocketState.CloseReceived)
                            {
                                Disconnected?.Invoke(this, System.EventArgs.Empty);

                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            if (e is ObjectDisposedException) Disconnected?.Invoke(this, System.EventArgs.Empty);

                            break;
                        }

                        for (var i = 0; i < result.Count; i++) allBytes.Add(buffer[i]);
                    } while (!result.EndOfMessage);

                    if (allBytes.Count == 0) continue;

                    var incomingPacket = JsonSerializer.Deserialize<BasicIncomingPacket>(allBytes.ToArray());

                    if (incomingPacket == null) continue;

                    if (incomingPacket.IsSystemPacket)
                    {
                        if (incomingPacket.Ref != null)
                        {
                            var @ref = (int)incomingPacket.Ref;
                            var refType = _channelManager.DetermineRefTypeByRef(@ref);

                            switch (refType)
                            {
                                case RefType.Join:
                                    var joinedChannelName = _channelManager.HandleJoin(@ref);

                                    if (joinedChannelName != null) ChannelJoined?.Invoke(this, joinedChannelName);

                                    break;
                                case RefType.Leave:
                                    var leftChannelName = _channelManager.HandleLeave(@ref);

                                    if (leftChannelName != null) ChannelLeft?.Invoke(this, leftChannelName);

                                    break;
                            }
                        }
                    }
                    else if (incomingPacket.IsHeartbeatPacket)
                    {
                        var heartbeatUpdatePacket =
                            JsonSerializer.Deserialize<IncomingHeartbeatPacket>(allBytes.ToArray());

                        if (heartbeatUpdatePacket?.Payload?.Hr == null) continue;

                        var deviceId = incomingPacket.Topic?[3..];
                        var heartbeat = (int)heartbeatUpdatePacket.Payload.Hr;

                        HeartbeatReceived?.Invoke(this, new HeartbeatReceivedEventArgs(deviceId!, heartbeat));
                    }
                    else if (incomingPacket.IsClipsPacket)
                    {
                        var clipCreatedPacket =
                            JsonSerializer.Deserialize<IncomingClipCreatedPacket>(allBytes.ToArray());

                        if (clipCreatedPacket?.Payload?.TwitchSlug == null) continue;

                        var deviceId = incomingPacket.Topic?[6..];

                        ClipCreated?.Invoke(this,
                            new ClipCreatedEventArgs(deviceId!, clipCreatedPacket.Payload.TwitchSlug));
                    }
                }

                await Task.Delay(1);
            }
        });
    }

    /// <summary>
    ///     Contains the application specific API token
    /// </summary>
    public string ApiToken { get; private set; } = "";

    /// <summary>
    ///     Returns true when the connection to the HypeRate servers is established
    /// </summary>
    public bool IsConnected => _websocketClient.State == WebSocketState.Open;

    /// <summary>
    ///     Starts the Tasks which are required for receiving packets and sending the keep-alive packet in the required
    ///     interval.
    /// </summary>
    public void Start()
    {
        _heartbeatTask.Start();
        _receiveTask.Start();
    }

    /// <summary>
    ///     Sets the new WebSocket API token.
    ///     It automatically trims the input so the leading and trailing spaces get removed.
    /// </summary>
    /// <param name="newApiToken"></param>
    public void SetApiToken(string newApiToken)
    {
        ApiToken = newApiToken.Trim();
    }

    /// <summary>
    ///     Returns the singleton instance of the HypeRate class
    /// </summary>
    /// <returns>The global unique instance</returns>
    public static HypeRate GetInstance()
    {
        return Instance;
    }

    /// <summary>
    ///     Tries to determine the ChannelType based on the given input.
    /// </summary>
    /// <param name="channelName">The name of the channel</param>
    /// <returns>
    ///     Returns ChannelType.Heartbeat when the channel name starts with "hr:".
    ///     Returns ChannelType.Clips when the channel name starts with "clips:".
    ///     Otherwise ChannelType.Unknown will be returned.
    /// </returns>
    public static ChannelType DetermineChannelType(string channelName)
    {
        if (channelName.StartsWith("hr:")) return ChannelType.Heartbeat;

        if (channelName.StartsWith("clips:")) return ChannelType.Clips;

        return ChannelType.Unknown;
    }

    #region Events

    /// <summary>
    ///     This event gets fired when the WebSocket client has successfully established a connection to the HypeRate servers.
    /// </summary>
    public event EventHandler? Connected;

    /// <summary>
    ///     This event gets fired when the WebSocket client has been disconnected.
    ///     This could be in the following scenarios:
    ///     - the user has lost their internet connection
    ///     - the HypeRate servers are getting restarted
    ///     - the client send data which couldn't be processed by the server
    /// </summary>
    public event EventHandler? Disconnected;

    /// <summary>
    ///     This event gets fired when the server acknowledged the join of a specific channel.
    /// </summary>
    public event EventHandler<string>? ChannelJoined;

    /// <summary>
    ///     This event gets fired when the server acknowledged the leave of a specific channel.
    /// </summary>
    public event EventHandler<string>? ChannelLeft;

    /// <summary>
    ///     This event gets fired when the WebSocket client has received a new "heartbeat" event (the user sent a new heartbeat
    ///     from their device to the servers).
    /// </summary>
    public event EventHandler<HeartbeatReceivedEventArgs>? HeartbeatReceived;

    /// <summary>
    ///     This event gets fired when the user has created a new clip (with the HypeClips integration in HypeRate).
    ///     More information here: https://github.com/HypeRate/DevDocs/blob/main/Clips.md#receiving-data
    /// </summary>
    public event EventHandler<ClipCreatedEventArgs>? ClipCreated;

    #endregion

    #region Network related

    /// <summary>
    ///     Tries to establish a connection to the HypeRate servers based on the BaseUrl and ApiToken
    /// </summary>
    /// <param name="cancellationToken">The cancellation token which is passed to the WebSocket client</param>
    public async Task Connect(CancellationToken cancellationToken = default)
    {
        if (_websocketClient == null || _websocketClient.State != WebSocketState.None)
        {
            _websocketClient?.Dispose();
            _websocketClient = new ClientWebSocket();
        }

        var urlToConnectTo = new Uri($"{BaseUrl}?token={ApiToken}");
        await _websocketClient.ConnectAsync(urlToConnectTo, cancellationToken);
        _channelManager = new ChannelManager();
        Connected?.Invoke(this, System.EventArgs.Empty);
    }

    /// <summary>
    ///     Tries to disconnect from the HypeRate servers
    /// </summary>
    /// <param name="cancellationToken">The cancellation token which is passed to the WebSocket client</param>
    public async Task Disconnect(CancellationToken cancellationToken = default)
    {
        if (_websocketClient != null && _websocketClient.State == WebSocketState.Open)
            await _websocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, null, cancellationToken);

        Disconnected?.Invoke(this, System.EventArgs.Empty);
        _websocketClient = new ClientWebSocket();
    }

    /// <summary>
    ///     Reconnects the WebSocket client and re-joins all previous joined channels.
    /// </summary>
    /// <param name="cancellationToken">The optional cancellation token</param>
    public async Task Reconnect(CancellationToken cancellationToken = default)
    {
        await Disconnect(cancellationToken);
        var channelsToRejoin = _channelManager.GetChannelsToRejoin();
        await Connect(cancellationToken);

        foreach (var channelName in channelsToRejoin) await JoinChannel(channelName, cancellationToken);
    }

    #endregion

    #region HypeRate related

    /// <summary>
    ///     Tries to join the "heartbeat" channel so that the "HeartbeatReceived" event handler will be invoked when new data
    ///     has been received.
    ///     This function is part of the high-level API.
    /// </summary>
    /// <param name="deviceId">The device ID of the user</param>
    /// <param name="cancellationToken">The optional cancellation token</param>
    public async Task JoinHeartbeatChannel(string deviceId, CancellationToken? cancellationToken)
    {
        await JoinChannel($"hr:{deviceId}", cancellationToken);
    }

    /// <summary>
    ///     Tries to leave the "heartbeat" channel so that the "HeartbeatReceived" event handler will not emit any new events
    ///     for the given device ID.
    ///     This function is part of the high-level API.
    /// </summary>
    /// <param name="deviceId">The device ID of the user</param>
    /// <param name="cancellationToken">The optional cancellation token</param>
    public async Task LeaveHeartbeatChannel(string deviceId, CancellationToken? cancellationToken)
    {
        await LeaveChannel($"hr:{deviceId}", cancellationToken);
    }

    /// <summary>
    ///     Tries to join the "clips" channel so that the "ClipCreated" event handler will be invoked when new data has been
    ///     received.
    ///     This function is part of the high-level API.
    /// </summary>
    /// <param name="deviceId">The device ID of the user</param>
    /// <param name="cancellationToken">The optional cancellation token</param>
    public async Task JoinClipsChannel(string deviceId, CancellationToken? cancellationToken)
    {
        await JoinChannel($"clips:{deviceId}", cancellationToken);
    }

    /// <summary>
    ///     Tries to leave the "clips" channel so that the "ClipCreated" event handler will not emit any new events for the
    ///     given device ID.
    ///     This function is part of the high-level API.
    /// </summary>
    /// <param name="deviceId">The device ID of the user</param>
    /// <param name="cancellationToken">The optional cancellation token</param>
    public async Task LeaveClipsChannel(string deviceId, CancellationToken? cancellationToken)
    {
        await LeaveChannel($"clips:{deviceId}", cancellationToken);
    }

    /// <summary>
    ///     Tries to join the given channel by its name.
    ///     This function is part of the low-level API (when no specific function is available for joining the required
    ///     channel.
    /// </summary>
    /// <param name="channelName">The name of the channel to join</param>
    /// <param name="cancellationToken">The optional cancellation token</param>
    public async Task JoinChannel(string channelName, CancellationToken? cancellationToken)
    {
        var joinRef = _channelManager.JoinChannel(channelName);

        if (joinRef == -1) return;

        await SendPacket(GetJoinPacket(channelName, joinRef), cancellationToken ?? default);
    }

    /// <summary>
    ///     Tries to leave the given channel by its name.
    ///     This function is part of the low-level API (when no specific function is available for leaving the required
    ///     channel.
    /// </summary>
    /// <param name="channelName">The name of the channel to leave</param>
    /// <param name="cancellationToken">The optional cancellation token</param>
    public async Task LeaveChannel(string channelName, CancellationToken? cancellationToken)
    {
        var leaveRef = _channelManager.LeaveChannel(channelName);

        if (leaveRef == -1) return;

        await SendPacket(GetLeavePacket(channelName, leaveRef), cancellationToken ?? default);
    }

    #endregion

    #region Packet stuff

    /// <summary>
    ///     Returns the string representation of the channel join packet
    /// </summary>
    /// <param name="topic">The name of the topic which should be joined</param>
    /// <param name="joinRef">The ref (request ID) of the packet</param>
    /// <returns>The JSON representation of the join packet</returns>
    private static string GetJoinPacket(string topic, int joinRef)
    {
        return JsonSerializer.Serialize(new JoinChannelPacket(topic, joinRef));
    }

    /// <summary>
    ///     Returns the string representation of the keep-alive packet
    /// </summary>
    /// <returns>The JSON representation of the keep-alive packet</returns>
    private static string GetKeepAlivePacket()
    {
        return JsonSerializer.Serialize(new KeepAlivePacket());
    }

    /// <summary>
    ///     Returns the string representation of the channel leave packet
    /// </summary>
    /// <param name="topic">The name of the topic which should be left</param>
    /// <param name="leaveRef">The ref (request ID) of the packet</param>
    /// <returns>The JSON representation of the leave packet</returns>
    private static string GetLeavePacket(string topic, int leaveRef)
    {
        return JsonSerializer.Serialize(new LeaveChannelPacket(topic, leaveRef));
    }

    /// <summary>
    ///     Sends the given JSON data to the HypeRate server
    /// </summary>
    /// <param name="data">The data to send</param>
    /// <param name="cancellationToken">The cancellation token of the user</param>
    private async Task SendPacket(string data, CancellationToken cancellationToken)
    {
        var messageBytes = Encoding.UTF8.GetBytes(data);
        try
        {
            await _websocketClient.SendAsync(
                messageBytes,
                WebSocketMessageType.Text,
                WebSocketMessageFlags.EndOfMessage,
                cancellationToken
            );
        }
        catch (Exception e)
        {
            if (e is ObjectDisposedException) Disconnected?.Invoke(this, System.EventArgs.Empty);

            throw;
        }
    }

    #endregion
}
