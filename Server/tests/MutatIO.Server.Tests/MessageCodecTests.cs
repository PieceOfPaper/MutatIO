using MutatIO.Shared.Protocol;

namespace MutatIO.Server.Tests;

public sealed class MessageCodecTests
{
    [Fact]
    public void Ping_RoundTrips()
    {
        var encoded = MessageCodec.EncodePing(new PingPayload(42, -123456789L));

        Assert.True(MessageCodec.TryReadType(encoded, out var type));
        Assert.Equal(MessageType.Ping, type);
        Assert.True(MessageCodec.TryDecodePing(encoded, out var decoded));
        Assert.Equal(42u, decoded.Sequence);
        Assert.Equal(-123456789L, decoded.ClientTimestamp);
    }

    [Fact]
    public void Decode_RejectsWrongTypeAndLength()
    {
        var ping = MessageCodec.EncodePing(new PingPayload(1, 2));

        Assert.False(MessageCodec.TryDecodePong(ping, out _));
        Assert.False(MessageCodec.TryDecodePing(ping.AsSpan(0, ping.Length - 1), out _));
        Assert.False(MessageCodec.TryReadType(ReadOnlySpan<byte>.Empty, out _));
    }
}
