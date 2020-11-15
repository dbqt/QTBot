using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.Communication.Clients;

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
            this.cancellationTokenSource.Cancel();
        }


        public Task StartListening()
        {
            return Task.Run(async () =>
            {
                await this.clientWebSocket.ConnectAsync(new Uri(this.address), cancellationTokenSource.Token);

                while (!this.cancellationTokenSource.IsCancellationRequested)
                {
                    WebSocketReceiveResult result;
                    var buffer = new byte[1024];
                    result = await this.clientWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationTokenSource.Token);

                    if (result == null)
                    {
                        continue;
                    }


                }

                await this.clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", this.cancellationTokenSource.Token);
            });
        }
    }
}
