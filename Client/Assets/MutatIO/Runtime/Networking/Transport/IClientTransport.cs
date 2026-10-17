using System;

namespace MutatIO.Networking.Transport
{
    /// <summary>
    /// 플랫폼별 연결 구현을 감추는 클라이언트 전송 계층입니다.
    /// 모든 메서드는 메인 스레드에서 호출하며, 수신 이벤트는 <see cref="TryDequeue"/>로 메인 스레드에서 꺼냅니다.
    /// </summary>
    public interface IClientTransport : IDisposable
    {
        TransportState State { get; }

        /// <summary>연결을 시작합니다. 결과는 Connected 또는 Disconnected 이벤트로 전달됩니다.</summary>
        void Connect(string url);

        /// <summary>바이너리 메시지 하나를 보냅니다. 연결되어 있지 않으면 false를 반환합니다.</summary>
        bool Send(byte[] message);

        /// <summary>정상 종료를 요청합니다. 종료가 끝나면 Disconnected 이벤트가 전달됩니다.</summary>
        void Close();

        bool TryDequeue(out TransportEvent transportEvent);
    }
}
