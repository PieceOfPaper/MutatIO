namespace MutatIO.Shared.Protocol
{
    /// <summary>
    /// 클라이언트와 서버가 연결 시 맞춰야 하는 프로토콜 기본 정보입니다.
    /// </summary>
    public static class ProtocolInfo
    {
        /// <summary>
        /// 메시지 형식이 호환되지 않게 바뀔 때마다 올립니다.
        /// </summary>
        public const int Version = 1;

        /// <summary>
        /// 게임 서버의 WebSocket 엔드포인트 경로입니다.
        /// </summary>
        public const string WebSocketPath = "/ws";
    }
}
