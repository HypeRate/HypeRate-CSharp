using System.Linq;
using System.Text.RegularExpressions;

namespace HypeRate;

/// <summary>
///     Contains utility functions for working with HypeRate device IDs.
///     More information here: https://github.com/HypeRate/DevDocs/blob/main/Device%20ID.md
/// </summary>
public static class Device
{
    /// <summary>
    ///     Contains all valid characters which can be found in a generated device ID.
    /// </summary>
    private static readonly char[] ValidIdCharacters =
    {
        'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v',
        'w', 'x', 'y', 'z',
        'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V',
        'W', 'X', 'Y', 'Z',
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9'
    };

    /// <summary>
    ///     Checks if the given input is a valid HypeRate device ID. Otherwise false is returned.
    /// </summary>
    /// <param name="input">The input to check</param>
    public static bool IsValidDeviceId(string input)
    {
        if (input.ToLower() == "internal-testing") return true;

        if (!HasValidLength(input)) return false;

        if (!HasValidDeviceCharacters(input)) return false;

        return true;
    }

    /// <summary>
    ///     Tries to extract the user device ID from the given input.
    /// </summary>
    /// <param name="input">The user provided input</param>
    /// <returns>Either the extracted HypeRate device ID as string or null</returns>
    public static string? ExtractDeviceId(string input)
    {
        var regex = new Regex(@"((https?:\/\/)?app\.hyperate\.io\/)?(?<device_id>[a-zA-Z0-9\-]+)(\?.*)?");
        var regexMatch = regex.Match(input);

        return !regexMatch.Success ? null : regexMatch.Groups["device_id"].Value;
    }

    /// <summary>
    ///     Returns true when the given input is between 3 and 8 characters long. Otherwise false is returned
    /// </summary>
    /// <param name="input">The input to check</param>
    private static bool HasValidLength(string input)
    {
        var inputLength = input.Length;

        return inputLength is >= 3 and <= 8;
    }

    /// <summary>
    ///     Returns true when the given input only contains valid device ID characters. Otherwise false is returned.
    /// </summary>
    /// <param name="input">The input to check</param>
    private static bool HasValidDeviceCharacters(string input)
    {
        return input.All(character => ValidIdCharacters.Contains(character));
    }
}
