using System.Net.WebSockets;
using System.Collections.Concurrent;

namespace CloudBackup.Management.WebSockets
{
    public class WebSocketConnectionManager
    {
        private readonly ConcurrentDictionary<string, WebSocket> _sockets = new();
        private readonly List<string> _socketsWithExceptions = new();

        public WebSocketConnectionManager()
        {
        }

        public WebSocket GetSocketById(string id)
        {
            return _sockets.FirstOrDefault(p => p.Key == id).Value;
        }

        public ConcurrentDictionary<string, WebSocket> GetAll()
        {
            return _sockets;
        }

        public string GetId(WebSocket socket)
        {
            return _sockets.FirstOrDefault(p => p.Value == socket).Key;
        }

        public void AddSocket(WebSocket socket)
        {
            _sockets.TryAdd(CreateConnectionId(), socket);
        }

        public async Task RemoveSocket(string id)
        {
            if (!_sockets.ContainsKey(id))
            {
                return;
            }

            var socket = _sockets[id];

            try
            {
                await socket.CloseAsync(closeStatus: WebSocketCloseStatus.NormalClosure,
                                        statusDescription: "Closed by the WebSocketManager",
                                        cancellationToken: CancellationToken.None);
            }
            catch
            {
                _socketsWithExceptions.Add(id);
            }
            finally
            {
                _sockets.TryRemove(id, out socket);
            }
        }

        private static string CreateConnectionId()
        {
            return Guid.NewGuid().ToString();
        }
    }
}