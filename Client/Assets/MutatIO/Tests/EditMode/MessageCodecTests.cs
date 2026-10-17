using MutatIO.Shared.Protocol;
using NUnit.Framework;

namespace MutatIO.Tests.EditMode
{
    public sealed class MessageCodecTests
    {
        [Test]
        public void Ping_RoundTrips()
        {
            var encoded = MessageCodec.EncodePing(new PingPayload(42, -123456789L));

            Assert.That(MessageCodec.TryReadType(encoded, out var type), Is.True);
            Assert.That(type, Is.EqualTo(MessageType.Ping));
            Assert.That(MessageCodec.TryDecodePing(encoded, out var decoded), Is.True);
            Assert.That(decoded.Sequence, Is.EqualTo(42u));
            Assert.That(decoded.ClientTimestamp, Is.EqualTo(-123456789L));
        }

        [Test]
        public void Decode_RejectsWrongType()
        {
            var ping = MessageCodec.EncodePing(new PingPayload(1, 2));

            Assert.That(MessageCodec.TryDecodePong(ping, out _), Is.False);
        }
    }
}
