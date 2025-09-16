using System;
using System.Collections.Generic;

namespace HypeRate;

public class ChannelManager
{
    /// <summary>
    ///     Contains all joined channels that were accepted by the server
    /// </summary>
    private readonly List<string> _joinedChannels = new();

    /// <summary>
    ///     Contains all channels which are about to be joined using their ref as key
    /// </summary>
    private readonly Dictionary<int, string> _joiningChannels = new();

    /// <summary>
    ///     Contains all channels that are about to be left using their ref as key
    /// </summary>
    private readonly Dictionary<int, string> _leavingChannels = new();

    /// <summary>
    ///     The random number generator which is used for generating "refs" (references aka request IDs)
    /// </summary>
    private readonly Random _randomNumberGenerator = new();

    /// <summary>
    ///     Contains all refs (request IDs) which are currently in use
    /// </summary>
    private readonly List<int> _refsInUse = new();

    /// <summary>
    ///     Returns the determined RefType by the given ref (request ID).
    ///     When the ref was not used for joining or leaving a channel then RefType.Unknown will be returned.
    /// </summary>
    /// <param name="ref">The ref (request ID) to check</param>
    /// <returns>
    ///     Returns RefType.Join when the given ref was used for joining a channel.
    ///     Returns RefType.Leave when the given ref was used for leaving a channel.
    ///     Otherwise returns RefType.Unknown when the ref couldn't be found.
    /// </returns>
    public RefType DetermineRefTypeByRef(int @ref)
    {
        if (_joiningChannels.ContainsKey(@ref)) return RefType.Join;

        if (_leavingChannels.ContainsKey(@ref)) return RefType.Leave;

        return RefType.Unknown;
    }

    /// <summary>
    ///     Tries to generate a ref for the given channel which should be joined.
    ///     Returns -1 when the channel is already about to be joined or when the channel has already been joined.
    /// </summary>
    /// <param name="channelName">The name of the channel which should be joined</param>
    /// <returns>
    ///     Either the generated ref (request ID) or -1 when the channel is about to be joined or has been joined.
    /// </returns>
    public int JoinChannel(string channelName)
    {
        if (_joiningChannels.ContainsValue(channelName)) return -1;

        if (_joinedChannels.Contains(channelName)) return -1;

        var generatedRef = GenerateRef();

        _joiningChannels[generatedRef] = channelName;
        _refsInUse.Add(generatedRef);

        return generatedRef;
    }

    /// <summary>
    ///     Tries to generate a ref for the given channel which should be left.
    ///     Returns -1 when the channel is about to be joined, the channel has not been joined or the channel is about to be
    ///     left already.
    /// </summary>
    /// <param name="channelName">The name of the channel to leave</param>
    /// <returns>
    ///     Either the generated ref (request ID) or -1 when the channel is about to be joined, has not been joined or is
    ///     about to be left.
    /// </returns>
    public int LeaveChannel(string channelName)
    {
        if (_joiningChannels.Count > 0 && _joiningChannels.ContainsValue(channelName))
        {
            var joiningRef = _joiningChannels.First(pair => pair.Value == channelName).Key;
            _joiningChannels.Remove(joiningRef);

            return -1;
        }

        if (!_joinedChannels.Contains(channelName)) return -1;

        if (_leavingChannels.ContainsValue(channelName)) return -1;

        var generatedRef = GenerateRef();

        _joinedChannels.Remove(channelName);
        _leavingChannels[generatedRef] = channelName;
        _refsInUse.Add(generatedRef);

        return generatedRef;
    }

    /// <summary>
    ///     Returns the list of channels which should be rejoined after a reconnect
    /// </summary>
    /// <returns>The list of channels to join</returns>
    public List<string> GetChannelsToRejoin()
    {
        var channelsToLeave = _leavingChannels.Values;
        var channelsToJoin = _joiningChannels.Values;

        return _joinedChannels
            .Where(joinedChannel => !channelsToLeave.Contains(joinedChannel))
            .Concat(
                channelsToJoin.Where(channelToJoin => !channelsToLeave.Contains(channelToJoin))
            ).ToList();
    }

    /// <summary>
    /// Handles the join event for the given ref (reference)
    /// </summary>
    /// <param name="ref">The generated reference of the request</param>
    /// <returns>Returns the name of the channel with the matching ref. Otherwise null is returned.</returns>
    public string? HandleJoin(int @ref)
    {
        if (!_joiningChannels.ContainsKey(@ref)) return null;

        var channelName = _joiningChannels[@ref];

        _joiningChannels.Remove(@ref);
        _joinedChannels.Add(channelName);
        _refsInUse.Remove(@ref);

        return channelName;
    }

    /// <summary>
    /// Handles the leave event for the given ref (reference)
    /// </summary>
    /// <param name="ref">The generated reference of the request</param>
    /// <returns></returns>
    public string? HandleLeave(int @ref)
    {
        if (!_leavingChannels.ContainsKey(@ref)) return null;

        var channelName = _leavingChannels[@ref];
        _leavingChannels.Remove(@ref);
        _refsInUse.Remove(@ref);

        return channelName;
    }

    /// <summary>
    ///     Generates a random ref (request ID).
    ///     Returns a number between 1 and 2147483646 since 0 is reserved for the KeepAlive packet.
    /// </summary>
    /// <returns>The unique and not used ref</returns>
    private int GenerateRef()
    {
        int generatedRef;

        do
        {
            generatedRef = _randomNumberGenerator.Next(1, int.MaxValue - 1);
        } while (_refsInUse.Contains(generatedRef));

        return generatedRef;
    }
}
