using System.Net;
using System.Net.Http.Json;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc.Testing;
using MutatIO.Shared.Protocol;

namespace MutatIO.Server.Tests;

public sealed class ServerEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Health_ReturnsProtocolVersion()
    {
        using var client = factory.CreateClient();

        var health = await client.GetFromJsonAsync<HealthResponse>("/health");

        Assert.NotNull(health);
        Assert.Equal("ok", health.Status);
        Assert.Equal(ProtocolInfo.Version, health.ProtocolVersion);
    }

    [Fact]
    public async Task WebSocketPath_RejectsPlainHttpRequest()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(ProtocolInfo.WebSocketPath);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task WebSocket_ConnectsAndClosesNormally()
    {
        var wsClient = factory.Server.CreateWebSocketClient();
        var uri = new Uri(factory.Server.BaseAddress, ProtocolInfo.WebSocketPath);

        using var socket = await wsClient.ConnectAsync(uri, CancellationToken.None);
        Assert.Equal(WebSocketState.Open, socket.State);

        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);

        Assert.Equal(WebSocketState.Closed, socket.State);
        Assert.Equal(WebSocketCloseStatus.NormalClosure, socket.CloseStatus);
    }

    [Fact]
    public async Task WebSocket_RepliesPongToPing()
    {
        var wsClient = factory.Server.CreateWebSocketClient();
        var uri = new Uri(factory.Server.BaseAddress, ProtocolInfo.WebSocketPath);
        using var socket = await wsClient.ConnectAsync(uri, CancellationToken.None);

        var ping = MessageCodec.EncodePing(new PingPayload(7, 1234L));
        await socket.SendAsync(ping, WebSocketMessageType.Binary, true, CancellationToken.None);

        var buffer = new byte[64];
        var result = await socket.ReceiveAsync(buffer, CancellationToken.None);

        Assert.Equal(WebSocketMessageType.Binary, result.MessageType);
        Assert.True(MessageCodec.TryDecodePong(buffer.AsSpan(0, result.Count), out var pong));
        Assert.Equal(7u, pong.Sequence);
        Assert.Equal(1234L, pong.ClientTimestamp);

        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
    }

    private sealed record HealthResponse(string Status, int ProtocolVersion);
}
