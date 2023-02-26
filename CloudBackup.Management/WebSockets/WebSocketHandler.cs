using System.Text;
using System.Net.WebSockets;

namespace CloudBackup.Management.WebSockets
{
    public abstract class WebSocketHandler
    {
        protected WebSocketConnectionManager WebSocketConnectionManager { get; set; }

        public WebSocketHandler(WebSocketConnectionManager webSocketConnectionManager)
        {
            WebSocketConnectionManager = webSocketConnectionManager;
        }

        public virtual void OnConnected(WebSocket socket)
        {
            WebSocketConnectionManager.AddSocket(socket);
        }

        public virtual async Task OnDisconnected(WebSocket socket)
        {
            await WebSocketConnectionManager.RemoveSocket(WebSocketConnectionManager.GetId(socket));
        }

        public async Task SendMessageAsync(WebSocket socket, string message)
        {
            if (socket.State != WebSocketState.Open)
            {
                return;
            }

            await socket.SendAsync(buffer: new ArraySegment<byte>(array: Encoding.ASCII.GetBytes(message), offset: 0, count: message.Length),
                messageType: WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken: CancellationToken.None);
        }

        public async Task SendMessageToAllAsync(string message)
        {
            var inactiveSockets = new List<WebSocket>();

            foreach (var pair in WebSocketConnectionManager.GetAll())
            {
                if (pair.Value.State == WebSocketState.Open)
                {
                    try
                    {
                        await SendMessageAsync(pair.Value, message);
                    }
                    catch
                    {
                        inactiveSockets.Add(pair.Value);
                    }
                }
                else
                {
                    inactiveSockets.Add(pair.Value);
                }
            }

            foreach (var socket in inactiveSockets)
            {
                await WebSocketConnectionManager.RemoveSocket(WebSocketConnectionManager.GetId(socket));
            }
        }

        public abstract Task ReceiveAsync(WebSocket socket, WebSocketReceiveResult result, byte[] buffer);
    }
}
