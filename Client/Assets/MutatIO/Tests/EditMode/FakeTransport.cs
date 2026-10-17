using System.Collections.Generic;
using MutatIO.Networking.Transport;

namespace MutatIO.Tests.EditMode
{
    /// <summary>
    /// 네트워크 없이 NetworkClient를 검증하기 위한 메모리 전송 계층입니다.
    /// </summary>
    internal sealed class FakeTransport : IClientTransport
    {
        private readonly Queue<TransportEvent> events = new Queue<TransportEvent>();

        public List<byte[]> Sent { get; } = new List<byte[]>();

        public TransportState State { get; private set; } = TransportState.Disconnected;

        public bool Disposed { get; private set; }

        public void Connect(string url)
        {
            State = TransportState.Connecting;
        }

        public bool Send(byte[] message)
        {
            if (State != TransportState.Connected)
            {
                return false;
            }

            Sent.Add(message);
            return true;
        }

        public void Close()
        {
            SimulateDisconnected("closed");
        }

        public bool TryDequeue(out TransportEvent transportEvent)
        {
            if (events.Count > 0)
            {
                transportEvent = events.Dequeue();
                return true;
            }

            transportEvent = default;
            return false;
        }

        public void Dispose()
        {
            Disposed = true;
        }

        public void SimulateConnected()
        {
            State = TransportState.Connected;
            events.Enqueue(TransportEvent.Connected());
        }

        public void SimulateMessage(byte[] message)
        {
            events.Enqueue(TransportEvent.Message(message));
        }

        public void SimulateDisconnected(string reason)
        {
            State = TransportState.Disconnected;
            events.Enqueue(TransportEvent.Disconnected(reason));
        }
    }
}
