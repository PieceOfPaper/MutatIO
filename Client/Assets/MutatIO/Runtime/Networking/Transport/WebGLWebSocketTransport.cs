// WebGL 플레이어와, 컴파일 확인을 위한 Editor에서만 빌드합니다.
// 다른 플레이어 빌드(iOS 등)에서 "__Internal" 심볼 링크 오류가 나지 않도록 제외합니다.
#if UNITY_WEBGL || UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;

namespace MutatIO.Networking.Transport
{
    /// <summary>
    /// 브라우저 WebSocket API를 jslib(MutatIOWebSocket.jslib)로 호출하는 WebGL 전용 전송 계층입니다.
    /// WebGL은 단일 스레드이므로 브라우저 콜백은 메인 스레드에서 실행됩니다.
    /// </summary>
    public sealed class WebGLWebSocketTransport : IClientTransport
    {
        private const int NormalClosureCode = 1000;

        private static readonly Dictionary<int, WebGLWebSocketTransport> Instances = new Dictionary<int, WebGLWebSocketTransport>();
        private static bool callbacksRegistered;

        private readonly Queue<TransportEvent> events = new Queue<TransportEvent>();
        private int socketId;

        private delegate void OpenCallback(int socketId);
        private delegate void MessageCallback(int socketId, IntPtr data, int length);
        private delegate void CloseCallback(int socketId, int code);

        public TransportState State { get; private set; } = TransportState.Disconnected;

        public void Connect(string url)
        {
            if (State != TransportState.Disconnected)
            {
                throw new InvalidOperationException($"연결 중이거나 연결된 상태에서는 다시 연결할 수 없습니다: {State}");
            }

            EnsureCallbacksRegistered();

            socketId = MutatIOWebSocket_Connect(url);
            if (socketId <= 0)
            {
                events.Enqueue(TransportEvent.Disconnected("WebSocket 생성 실패"));
                return;
            }

            Instances[socketId] = this;
            State = TransportState.Connecting;
        }

        public bool Send(byte[] message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return State == TransportState.Connected && MutatIOWebSocket_Send(socketId, message, message.Length) != 0;
        }

        public void Close()
        {
            if (State == TransportState.Connecting || State == TransportState.Connected)
            {
                State = TransportState.Closing;
                MutatIOWebSocket_Close(socketId, NormalClosureCode);
            }
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
            if (socketId > 0)
            {
                Instances.Remove(socketId);
                MutatIOWebSocket_Close(socketId, NormalClosureCode);
                socketId = 0;
            }

            State = TransportState.Disconnected;
        }

        private static void EnsureCallbacksRegistered()
        {
            if (callbacksRegistered)
            {
                return;
            }

            MutatIOWebSocket_SetCallbacks(OnOpen, OnMessage, OnClose);
            callbacksRegistered = true;
        }

        [MonoPInvokeCallback(typeof(OpenCallback))]
        private static void OnOpen(int id)
        {
            if (Instances.TryGetValue(id, out var transport))
            {
                transport.State = TransportState.Connected;
                transport.events.Enqueue(TransportEvent.Connected());
            }
        }

        [MonoPInvokeCallback(typeof(MessageCallback))]
        private static void OnMessage(int id, IntPtr data, int length)
        {
            if (Instances.TryGetValue(id, out var transport))
            {
                var message = new byte[length];
                Marshal.Copy(data, message, 0, length);
                transport.events.Enqueue(TransportEvent.Message(message));
            }
        }

        [MonoPInvokeCallback(typeof(CloseCallback))]
        private static void OnClose(int id, int code)
        {
            if (Instances.TryGetValue(id, out var transport))
            {
                Instances.Remove(id);
                transport.socketId = 0;
                transport.State = TransportState.Disconnected;
                transport.events.Enqueue(TransportEvent.Disconnected($"close code {code}"));
            }
        }

        [DllImport("__Internal")]
        private static extern void MutatIOWebSocket_SetCallbacks(OpenCallback onOpen, MessageCallback onMessage, CloseCallback onClose);

        [DllImport("__Internal")]
        private static extern int MutatIOWebSocket_Connect(string url);

        [DllImport("__Internal")]
        private static extern int MutatIOWebSocket_Send(int socketId, byte[] data, int length);

        [DllImport("__Internal")]
        private static extern void MutatIOWebSocket_Close(int socketId, int code);
    }
}
#endif
