namespace MutatIO.Networking.Transport
{
    /// <summary>
    /// 현재 플랫폼에 맞는 전송 계층을 만듭니다.
    /// </summary>
    public static class ClientTransportFactory
    {
        public static IClientTransport CreateDefault()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return new WebGLWebSocketTransport();
#else
            return new ClientWebSocketTransport();
#endif
        }
    }
}
