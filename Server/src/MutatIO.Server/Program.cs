using MutatIO.Server.Networking;
using MutatIO.Shared.Protocol;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<WebSocketSessionHandler>();

var app = builder.Build();

app.UseWebSockets();

app.MapGet("/health", () => Results.Ok(new { status = "ok", protocolVersion = ProtocolInfo.Version }));

app.Map(ProtocolInfo.WebSocketPath, async (HttpContext context, WebSocketSessionHandler handler) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    await handler.RunAsync(socket, context.RequestAborted);
});

app.Run();
