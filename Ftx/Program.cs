using System;
using FtxApi;

namespace Ftx
{
    class Program
    {
        static void Main(string[] args)
        {
            var ins = "KNC/USDT";
            FtxWebSocket webSocket = new FtxWebSocket();
            webSocket.Connect();
            // string auth = webSocket.GetAuthRequest("", "");
            // webSocket.Send(auth);
            string orderbook = webSocket.GetSubscribeRequest("orderbook", ins);
            webSocket.Send(orderbook);
            // while (true)
            {
                System.Threading.Thread.Sleep(1000 * 60 * 2);
            }

            string orderbook_un = webSocket.GetUnsubscribeRequest("orderbook", ins);
            webSocket.Send(orderbook_un);

            while (true)
            {
                System.Threading.Thread.Sleep(1000 * 30);
            }
            Console.WriteLine("Hello World!");
        }
    }
}
