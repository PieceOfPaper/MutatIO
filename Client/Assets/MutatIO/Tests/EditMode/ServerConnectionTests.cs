using System;
using System.Collections;
using System.Diagnostics;
using MutatIO.Networking;
using MutatIO.Networking.Transport;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace MutatIO.Tests.EditMode
{
    /// <summary>
    /// 로컬에서 실행 중인 실제 서버와 통신하는 통합 테스트입니다.
    /// 서버가 없으면 건너뜁니다. 주소는 환경 변수 MUTATIO_SERVER_URL로 바꿀 수 있습니다.
    /// </summary>
    [Category("Integration")]
    public sealed class ServerConnectionTests
    {
        private const string DefaultServerUrl = "ws://localhost:5080/ws";
        private const double TimeoutSeconds = 5;

        [UnityTest]
        public IEnumerator ClientWebSocket_ConnectsPingsAndCloses()
        {
            var url = Environment.GetEnvironmentVariable("MUTATIO_SERVER_URL");
            if (string.IsNullOrEmpty(url))
            {
                url = DefaultServerUrl;
            }

            using (var client = new NetworkClient(new ClientWebSocketTransport()))
            {
                var connected = false;
                string disconnectReason = null;
                var disconnected = false;
                PongResult? pong = null;
                client.Connected += () => connected = true;
                client.Disconnected += reason =>
                {
                    disconnected = true;
                    disconnectReason = reason;
                };
                client.PongReceived += result => pong = result;

                client.Connect(url);
                yield return WaitUntil(client, () => connected || disconnected);
                if (!connected)
                {
                    Assert.Ignore($"서버에 연결할 수 없어 건너뜁니다 ({url}): {disconnectReason}");
                }

                Assert.That(client.SendPing(), Is.True);
                yield return WaitUntil(client, () => pong.HasValue);
                Assert.That(pong.HasValue, Is.True, "Pong을 받지 못했습니다.");
                Assert.That(pong.Value.Sequence, Is.EqualTo(1u));
                Assert.That(pong.Value.RoundTripMilliseconds, Is.GreaterThanOrEqualTo(0));

                client.Disconnect();
                yield return WaitUntil(client, () => disconnected);
                Assert.That(disconnected, Is.True, "연결이 종료되지 않았습니다.");
                Assert.That(client.State, Is.EqualTo(TransportState.Disconnected));
            }
        }

        private static IEnumerator WaitUntil(NetworkClient client, Func<bool> condition)
        {
            var stopwatch = Stopwatch.StartNew();
            while (!condition() && stopwatch.Elapsed.TotalSeconds < TimeoutSeconds)
            {
                client.Update();
                yield return null;
            }

            client.Update();
        }
    }
}
