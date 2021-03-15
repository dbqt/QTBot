using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace QTBot.Modules
{
    public class BeatSaberModule
    {
        private string address = "127.0.0.1:2946";

        private ClientWebSocket clientWebSocket = new ClientWebSocket();
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        public BeatSaberModule()
        {
        }

        public void StopListening()
        {
            cancellationTokenSource.Cancel();
        }


        public Task StartListening()
        {
            return Task.Run(async () =>
            {
                await clientWebSocket.ConnectAsync(new Uri(address), cancellationTokenSource.Token);

                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    WebSocketReceiveResult result;
                    var buffer = new byte[1024];
                    result = await clientWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationTokenSource.Token);

                    if (result == null)
                    {
                        continue;
                    }


                }

                await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationTokenSource.Token);
            });
        }
    }
}
