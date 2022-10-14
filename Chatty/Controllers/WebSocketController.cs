using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace Chatty.Controllers;

// <snippet>
public class WebSocketController : ControllerBase
{
    private readonly ILogger _logger;
    private ICollection<string> _chatMessages = new List<string>();

    public WebSocketController(ILogger<WebSocketController> logger)
    {
        _logger = logger;
    }

    [HttpGet("/ws")]
    public async Task Get()
    {
        var sourceIp = Request.HttpContext.Connection.RemoteIpAddress;
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            _logger.LogInformation("WebSocket connected from " + sourceIp);
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await Echo(webSocket);
        }
        else
        {
            _logger.LogDebug("Not a WebSocket request from " + sourceIp);
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
    // </snippet>

    private async Task Echo(WebSocket webSocket)
    {
        var buffer = new byte[1024 * 4];
        var receiveResult = await webSocket.ReceiveAsync(
            new ArraySegment<byte>(buffer), CancellationToken.None);

        while (!receiveResult.CloseStatus.HasValue)
        {
            //await webSocket.SendAsync(
            //    new ArraySegment<byte>(buffer, 0, receiveResult.Count),
            //    receiveResult.MessageType,
            //    receiveResult.EndOfMessage,
            //    CancellationToken.None);

            await StoreMessage(buffer, receiveResult);

            var staticResponse = new ArraySegment<byte>(Encoding.UTF8.GetBytes("stfu dood"));
            await webSocket.SendAsync(
                staticResponse,
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);

            receiveResult = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer), CancellationToken.None);
        }

        await webSocket.CloseAsync(
            receiveResult.CloseStatus.Value,
            receiveResult.CloseStatusDescription,
            CancellationToken.None);
    }

    private async Task StoreMessage(byte[] buffer, WebSocketReceiveResult webSocketReceiveResult)
    {
        var message = Encoding.ASCII.GetString(buffer, 0, webSocketReceiveResult.Count);
        
        _logger.LogInformation(message);
        _chatMessages.Add(message);
    }
}