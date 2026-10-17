using MutatIO.Networking;
using MutatIO.Shared.Protocol;
using NUnit.Framework;

namespace MutatIO.Tests.EditMode
{
    public sealed class NetworkClientTests
    {
        private FakeTransport transport;
        private long now;
        private NetworkClient client;

        [SetUp]
        public void SetUp()
        {
            transport = new FakeTransport();
            now = 1000;
            client = new NetworkClient(transport, () => now);
        }

        [Test]
        public void SendPing_FailsWhenNotConnected()
        {
            Assert.That(client.SendPing(), Is.False);
            Assert.That(transport.Sent, Is.Empty);
        }

        [Test]
        public void Update_RaisesConnectedAndDisconnected()
        {
            var connected = false;
            string reason = null;
            client.Connected += () => connected = true;
            client.Disconnected += r => reason = r;

            client.Connect("ws://test");
            transport.SimulateConnected();
            Assert.That(connected, Is.False, "Update 전에는 이벤트가 전달되지 않아야 합니다.");

            client.Update();
            Assert.That(connected, Is.True);

            transport.SimulateDisconnected("bye");
            client.Update();
            Assert.That(reason, Is.EqualTo("bye"));
        }

        [Test]
        public void Pong_ReportsRoundTripTime()
        {
            client.Connect("ws://test");
            transport.SimulateConnected();
            client.Update();

            Assert.That(client.SendPing(), Is.True);
            Assert.That(MessageCodec.TryDecodePing(transport.Sent[0], out var ping), Is.True);
            Assert.That(ping.Sequence, Is.EqualTo(1u));

            PongResult? received = null;
            client.PongReceived += r => received = r;
            now += 35;
            transport.SimulateMessage(MessageCodec.EncodePong(ping));
            client.Update();

            Assert.That(received.HasValue, Is.True);
            Assert.That(received.Value.Sequence, Is.EqualTo(1u));
            Assert.That(received.Value.RoundTripMilliseconds, Is.EqualTo(35));
            Assert.That(client.LastRoundTripMilliseconds, Is.EqualTo(35));
        }

        [Test]
        public void Dispose_DisposesTransport()
        {
            client.Dispose();

            Assert.That(transport.Disposed, Is.True);
        }
    }
}
