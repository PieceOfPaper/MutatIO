namespace MutatIO.Shared.Protocol
{
    /// <summary>
    /// 메시지 첫 바이트에 기록되는 메시지 종류입니다.
    /// </summary>
    public enum MessageType : byte
    {
        Ping = 1,
        Pong = 2,
    }
}
