using System.Net.WebSockets;

namespace CloudBackup.Management.WebSockets
{
    public class WebSocketManagerMiddleware
    {
        private WebSocketHandler WebSocketHandler { get; set; }

        public WebSocketManagerMiddleware(RequestDelegate next, WebSocketHandler webSocketHandler)
        {
            WebSocketHandler = webSocketHandler;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                return;
            }

            var socket = await context.WebSockets.AcceptWebSocketAsync();
            WebSocketHandler.OnConnected(socket);

            await Receive(socket, async (result, buffer) =>
            {
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    await WebSocketHandler.ReceiveAsync(socket, result, buffer);

                    return;
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await WebSocketHandler.OnDisconnected(socket);

                    return;
                }
            });
        }

        private async Task Receive(WebSocket socket, Action<WebSocketReceiveResult, byte[]> handleMessage)
        {
            var buffer = new byte[1024 * 4];

            while (socket.State == WebSocketState.Open)
            {
                try
                {
                    var result = await socket.ReceiveAsync(buffer: new ArraySegment<byte>(buffer),
                                                           cancellationToken: CancellationToken.None);

                    handleMessage(result, buffer);
                }
                catch
                {
                    await WebSocketHandler.OnDisconnected(socket);
                }
            }
        }
    }
}
