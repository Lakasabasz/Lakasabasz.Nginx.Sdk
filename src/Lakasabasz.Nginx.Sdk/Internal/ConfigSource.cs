using System.Text;

namespace Lakasabasz.Nginx.Sdk.Internal;

/// <summary>
/// The text of a config file and how it is stored on disk (encoding, byte order mark), so that writing it back
/// produces the same bytes.
/// </summary>
internal sealed record ConfigSource(string Text, Encoding Encoding, bool ByteOrderMark)
{
    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];
    private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, throwOnInvalidBytes: true);

    /// <summary>A new, empty file: UTF-8 without a byte order mark.</summary>
    public static ConfigSource Empty { get; } = new(string.Empty, StrictUtf8, false);

    /// <summary>
    /// Reads a file: UTF-8 when its bytes are valid UTF-8 (with or without a byte order mark), otherwise Latin-1,
    /// which maps every byte to one character and back.
    /// </summary>
    public static ConfigSource Read(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var bom = bytes.Length >= 3 && bytes[0] == Utf8Bom[0] && bytes[1] == Utf8Bom[1] && bytes[2] == Utf8Bom[2];
        var offset = bom ? Utf8Bom.Length : 0;
        try
        {
            return new ConfigSource(StrictUtf8.GetString(bytes, offset, bytes.Length - offset), StrictUtf8, bom);
        }
        catch (DecoderFallbackException)
        {
            var latin1 = Encoding.GetEncoding("iso-8859-1");
            return new ConfigSource(latin1.GetString(bytes), latin1, false);
        }
    }
}
