using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace MutatIO.Networking.Transport
{
    /// <summary>
    /// .NET <see cref="ClientWebSocket"/> 기반 전송 계층입니다.
    /// Editor, 데스크톱, Android, iOS에서 사용합니다. WebGL에서는 동작하지 않습니다.
    /// 송수신은 백그라운드 스레드에서 수행하고 이벤트는 큐로 메인 스레드에 넘깁니다.
    /// </summary>
    public sealed class ClientWebSocketTransport : IClientTransport
    {
        private const int ReceiveBufferSize = 4 * 1024;
        private const int MaxMessageSize = 64 * 1024;
        private const int CloseTimeoutMilliseconds = 5000;

        private readonly ConcurrentQueue<TransportEvent> events = new ConcurrentQueue<TransportEvent>();

        private ConcurrentQueue<byte[]> outgoing;
        private SemaphoreSlim outgoingSignal;
        private CancellationTokenSource cancellation;
        private volatile bool closeRequested;
        private int state = (int)TransportState.Disconnected;

        public TransportState State => (TransportState)Volatile.Read(ref state);

        public void Connect(string url)
        {
            if (State != TransportState.Disconnected)
            {
                throw new InvalidOperationException($"연결 중이거나 연결된 상태에서는 다시 연결할 수 없습니다: {State}");
            }

            var uri = new Uri(url);
            outgoing = new ConcurrentQueue<byte[]>();
            outgoingSignal = new SemaphoreSlim(0);
            cancellation?.Dispose();
            cancellation = new CancellationTokenSource();
            closeRequested = false;
            SetState(TransportState.Connecting);

            _ = RunAsync(uri, outgoing, outgoingSignal, cancellation.Token);
        }

        public bool Send(byte[] message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (State != TransportState.Connected)
            {
                return false;
            }

            outgoing.Enqueue(message);
            outgoingSignal.Release();
            return true;
        }

        public void Close()
        {
            switch (State)
            {
                case TransportState.Connecting:
                    cancellation.Cancel();
                    break;
                case TransportState.Connected:
                    SetState(TransportState.Closing);
                    closeRequested = true;
                    outgoingSignal.Release();
                    cancellation.CancelAfter(CloseTimeoutMilliseconds);
                    break;
            }
        }

        public bool TryDequeue(out TransportEvent transportEvent)
        {
            return events.TryDequeue(out transportEvent);
        }

        public void Dispose()
        {
            cancellation?.Cancel();
        }

        private async Task RunAsync(Uri uri, ConcurrentQueue<byte[]> sendQueue, SemaphoreSlim sendSignal, CancellationToken token)
        {
            string reason = null;
            using (var socket = new ClientWebSocket())
            {
                try
                {
                    await socket.ConnectAsync(uri, token).ConfigureAwait(false);
                    SetState(TransportState.Connected);
                    events.Enqueue(TransportEvent.Connected());

                    using (var sendLoopCancellation = CancellationTokenSource.CreateLinkedTokenSource(token))
                    {
                        var sendLoop = SendLoopAsync(socket, sendQueue, sendSignal, sendLoopCancellation.Token);
                        reason = await ReceiveLoopAsync(socket, token).ConfigureAwait(false);
                        sendLoopCancellation.Cancel();
                        await IgnoreCancellation(sendLoop).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    reason = closeRequested ? "종료 응답 시간 초과" : "연결 취소";
                }
                catch (Exception ex)
                {
                    reason = ex.Message;
                }
            }

            SetState(TransportState.Disconnected);
            events.Enqueue(TransportEvent.Disconnected(reason));
        }

        private async Task<string> ReceiveLoopAsync(ClientWebSocket socket, CancellationToken token)
        {
            var buffer = new byte[ReceiveBufferSize];
            using (var message = new MemoryStream())
            {
                while (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseSent)
                {
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        if (socket.State == WebSocketState.CloseReceived)
                        {
                            await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, token).ConfigureAwait(false);
                        }

                        return socket.CloseStatusDescription ?? socket.CloseStatus?.ToString();
                    }

                    if (message.Length + result.Count > MaxMessageSize)
                    {
                        await socket.CloseOutputAsync(WebSocketCloseStatus.MessageTooBig, null, token).ConfigureAwait(false);
                        return "메시지 크기 초과";
                    }

                    message.Write(buffer, 0, result.Count);
                    if (!result.EndOfMessage)
                    {
                        continue;
                    }

                    if (result.MessageType == WebSocketMessageType.Binary)
                    {
                        events.Enqueue(TransportEvent.Message(message.ToArray()));
                    }

                    message.SetLength(0);
                }
            }

            return socket.CloseStatusDescription;
        }

        private async Task SendLoopAsync(ClientWebSocket socket, ConcurrentQueue<byte[]> sendQueue, SemaphoreSlim sendSignal, CancellationToken token)
        {
            while (true)
            {
                await sendSignal.WaitAsync(token).ConfigureAwait(false);

                while (sendQueue.TryDequeue(out var message))
                {
                    await socket.SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Binary, true, token).ConfigureAwait(false);
                }

                if (closeRequested)
                {
                    await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, token).ConfigureAwait(false);
                    return;
                }
            }
        }

        private static async Task IgnoreCancellation(Task task)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (WebSocketException)
            {
            }
        }

        private void SetState(TransportState value)
        {
            Volatile.Write(ref state, (int)value);
        }
    }
}
