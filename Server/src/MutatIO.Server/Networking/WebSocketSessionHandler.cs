using System.Net.WebSockets;
using MutatIO.Shared.Protocol;

namespace MutatIO.Server.Networking;

/// <summary>
/// WebSocket 연결 하나의 수명을 관리하고 수신 메시지를 처리합니다.
/// </summary>
public sealed class WebSocketSessionHandler(ILogger<WebSocketSessionHandler> logger)
{
    private const int ReceiveBufferSize = 4 * 1024;
    private const int MaxMessageSize = 64 * 1024;

    public async Task RunAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        var sessionId = Guid.NewGuid();
        logger.LogInformation("세션 연결: {SessionId}", sessionId);

        var buffer = new byte[ReceiveBufferSize];
        using var message = new MemoryStream();
        try
        {
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(buffer, cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, cancellationToken);
                    break;
                }

                if (message.Length + result.Count > MaxMessageSize)
                {
                    logger.LogWarning("메시지 크기 초과로 세션 종료: {SessionId}", sessionId);
                    await socket.CloseAsync(WebSocketCloseStatus.MessageTooBig, null, cancellationToken);
                    break;
                }

                message.Write(buffer, 0, result.Count);
                if (!result.EndOfMessage)
                {
                    continue;
                }

                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    await HandleMessageAsync(socket, message.GetBuffer().AsMemory(0, (int)message.Length), cancellationToken);
                }

                message.SetLength(0);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (WebSocketException ex)
        {
            logger.LogWarning(ex, "세션 비정상 종료: {SessionId}", sessionId);
        }

        logger.LogInformation("세션 종료: {SessionId}", sessionId);
    }

    private static async Task HandleMessageAsync(WebSocket socket, ReadOnlyMemory<byte> message, CancellationToken cancellationToken)
    {
        if (!MessageCodec.TryReadType(message.Span, out var type))
        {
            return;
        }

        switch (type)
        {
            case MessageType.Ping when MessageCodec.TryDecodePing(message.Span, out var ping):
                var pong = MessageCodec.EncodePong(ping);
                await socket.SendAsync(pong, WebSocketMessageType.Binary, true, cancellationToken);
                break;
        }
    }
}
