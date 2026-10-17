namespace MutatIO.Networking.Transport
{
    public enum TransportEventKind
    {
        Connected,
        Message,
        Disconnected,
    }

    /// <summary>
    /// 전송 계층에서 발생해 메인 스레드에서 처리될 이벤트입니다.
    /// </summary>
    public readonly struct TransportEvent
    {
        private TransportEvent(TransportEventKind kind, byte[] data, string reason)
        {
            Kind = kind;
            Data = data;
            Reason = reason;
        }

        public TransportEventKind Kind { get; }

        /// <summary><see cref="TransportEventKind.Message"/>일 때 받은 메시지 전체입니다.</summary>
        public byte[] Data { get; }

        /// <summary><see cref="TransportEventKind.Disconnected"/>일 때 종료 사유입니다. 없을 수 있습니다.</summary>
        public string Reason { get; }

        public static TransportEvent Connected() => new TransportEvent(TransportEventKind.Connected, null, null);

        public static TransportEvent Message(byte[] data) => new TransportEvent(TransportEventKind.Message, data, null);

        public static TransportEvent Disconnected(string reason) => new TransportEvent(TransportEventKind.Disconnected, null, reason);
    }
}
