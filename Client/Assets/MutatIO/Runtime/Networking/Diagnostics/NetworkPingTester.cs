using MutatIO.Networking.Transport;
using UnityEngine;

namespace MutatIO.Networking.Diagnostics
{
    /// <summary>
    /// 서버 연결과 왕복 지연을 확인하는 개발용 컴포넌트입니다.
    /// 화면 왼쪽 위에 연결 상태와 RTT를 표시합니다.
    /// WebGL 기본 폰트에 한글 글리프가 없으므로 화면 문구는 영어로 둡니다.
    /// </summary>
    [AddComponentMenu("MutatIO/Diagnostics/Network Ping Tester")]
    public sealed class NetworkPingTester : MonoBehaviour
    {
        [SerializeField] private string serverUrl = "ws://localhost:5080/ws";
        [SerializeField] private bool connectOnStart = true;
        [SerializeField, Min(0.1f)] private float pingIntervalSeconds = 1f;

        private NetworkClient client;
        private float nextPingTime;
        private int pongCount;
        private string lastDisconnectReason;

        private void Awake()
        {
            client = new NetworkClient(ClientTransportFactory.CreateDefault());
            client.Connected += OnConnected;
            client.Disconnected += OnDisconnected;
            client.PongReceived += OnPongReceived;
        }

        private void Start()
        {
            if (connectOnStart)
            {
                Connect();
            }
        }

        private void Update()
        {
            client.Update();

            if (client.State == TransportState.Connected && Time.unscaledTime >= nextPingTime)
            {
                nextPingTime = Time.unscaledTime + pingIntervalSeconds;
                client.SendPing();
            }
        }

        private void OnDestroy()
        {
            client?.Dispose();
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 320, 160), GUI.skin.box);
            GUILayout.Label($"Server: {serverUrl}");
            GUILayout.Label($"State: {client.State}");
            GUILayout.Label($"RTT: {(client.LastRoundTripMilliseconds.HasValue ? client.LastRoundTripMilliseconds + " ms" : "-")}  (Pong {pongCount})");
            if (!string.IsNullOrEmpty(lastDisconnectReason))
            {
                GUILayout.Label($"Closed: {lastDisconnectReason}");
            }

            GUILayout.BeginHorizontal();
            GUI.enabled = client.State == TransportState.Disconnected;
            if (GUILayout.Button("Connect"))
            {
                Connect();
            }

            GUI.enabled = client.State == TransportState.Connected;
            if (GUILayout.Button("Disconnect"))
            {
                client.Disconnect();
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void Connect()
        {
            lastDisconnectReason = null;
            pongCount = 0;
            client.Connect(serverUrl);
        }

        private void OnConnected()
        {
            Debug.Log($"[MutatIO] 서버 연결: {serverUrl}");
            nextPingTime = 0f;
        }

        private void OnDisconnected(string reason)
        {
            lastDisconnectReason = reason;
            Debug.Log($"[MutatIO] 서버 연결 종료: {reason}");
        }

        private void OnPongReceived(PongResult result)
        {
            pongCount++;
        }
    }
}
