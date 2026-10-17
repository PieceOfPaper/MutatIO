namespace MutatIO.Shared.Protocol
{
    /// <summary>
    /// 연결 확인과 왕복 지연 측정에 쓰는 Ping/Pong 공용 내용입니다.
    /// 서버는 받은 값을 그대로 Pong으로 돌려줍니다.
    /// </summary>
    public readonly struct PingPayload
    {
        public PingPayload(uint sequence, long clientTimestamp)
        {
            Sequence = sequence;
            ClientTimestamp = clientTimestamp;
        }

        /// <summary>클라이언트가 보낸 순번입니다.</summary>
        public uint Sequence { get; }

        /// <summary>클라이언트가 보낸 시각입니다. 단위는 클라이언트가 정하며 서버는 해석하지 않습니다.</summary>
        public long ClientTimestamp { get; }
    }
}
