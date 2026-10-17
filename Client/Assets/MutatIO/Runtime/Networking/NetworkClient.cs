using System;
using System.Diagnostics;
using MutatIO.Networking.Transport;
using MutatIO.Shared.Protocol;

namespace MutatIO.Networking
{
    public readonly struct PongResult
    {
        public PongResult(uint sequence, long roundTripMilliseconds)
        {
            Sequence = sequence;
            RoundTripMilliseconds = roundTripMilliseconds;
        }

        public uint Sequence { get; }

        public long RoundTripMilliseconds { get; }
    }

    /// <summary>
    /// 전송 계층 위에서 프로토콜 메시지를 주고받는 클라이언트입니다.
    /// 메인 스레드에서 매 프레임 <see cref="Update"/>를 호출해야 이벤트가 전달됩니다.
    /// </summary>
    public sealed class NetworkClient : IDisposable
    {
        private readonly IClientTransport transport;
        private readonly Func<long> clockMilliseconds;
        private uint lastPingSequence;

        public NetworkClient(IClientTransport transport)
            : this(transport, StopwatchMilliseconds)
        {
        }

        public NetworkClient(IClientTransport transport, Func<long> clockMilliseconds)
        {
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
            this.clockMilliseconds = clockMilliseconds ?? throw new ArgumentNullException(nameof(clockMilliseconds));
        }

        public event Action Connected;
        public event Action<string> Disconnected;
        public event Action<PongResult> PongReceived;

        public TransportState State => transport.State;

        /// <summary>마지막으로 측정한 왕복 지연입니다. 아직 측정하지 않았으면 null입니다.</summary>
        public long? LastRoundTripMilliseconds { get; private set; }

        public void Connect(string url)
        {
            LastRoundTripMilliseconds = null;
            transport.Connect(url);
        }

        public void Disconnect()
        {
            transport.Close();
        }

        public bool SendPing()
        {
            var payload = new PingPayload(lastPingSequence + 1, clockMilliseconds());
            if (!transport.Send(MessageCodec.EncodePing(payload)))
            {
                return false;
            }

            lastPingSequence = payload.Sequence;
            return true;
        }

        public void Update()
        {
            while (transport.TryDequeue(out var transportEvent))
            {
                switch (transportEvent.Kind)
                {
                    case TransportEventKind.Connected:
                        Connected?.Invoke();
                        break;
                    case TransportEventKind.Message:
                        HandleMessage(transportEvent.Data);
                        break;
                    case TransportEventKind.Disconnected:
                        Disconnected?.Invoke(transportEvent.Reason);
                        break;
                }
            }
        }

        public void Dispose()
        {
            transport.Dispose();
        }

        private void HandleMessage(byte[] message)
        {
            if (!MessageCodec.TryReadType(message, out var type))
            {
                return;
            }

            switch (type)
            {
                case MessageType.Pong when MessageCodec.TryDecodePong(message, out var pong):
                    var roundTrip = clockMilliseconds() - pong.ClientTimestamp;
                    LastRoundTripMilliseconds = roundTrip;
                    PongReceived?.Invoke(new PongResult(pong.Sequence, roundTrip));
                    break;
            }
        }

        private static long StopwatchMilliseconds()
        {
            return (long)(Stopwatch.GetTimestamp() * (1000.0 / Stopwatch.Frequency));
        }
    }
}
