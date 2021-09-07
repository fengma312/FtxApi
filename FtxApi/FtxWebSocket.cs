using System;
using System.Text;
using System.Timers;
using WebSocketSharp;
using Newtonsoft.Json;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using System.Security.Authentication;
using Microsoft.Extensions.Logging.Abstractions;

namespace FtxApi
{
    /// <summary>
    /// Ftx 的ＷebSocket接口
    /// </summary>
    public class FtxWebSocket
    {
        /// <summary>
        /// 日志接口
        /// </summary>
        private ILogger logger;
        /// <summary>
        /// 日志标签
        /// </summary>
        /// <returns></returns>
        private EventId event_id = new EventId(0, "FTX");
        /// <summary>
        /// websocket地址
        /// </summary>
        private string api_host;
        /// <summary>
        /// 判断超时定时器
        /// </summary>
        private Timer timer;
        /// <summary>
        /// websocket对象
        /// </summary>
        protected WebSocket websocket;
        /// <summary>
        /// 最后反馈时间
        /// </summary>
        private DateTime last_received_time;
        /// <summary>
        /// 是否自动重连
        /// </summary>
        public bool auto_connect;
        /// <summary>
        /// 重新连接等待时长
        /// </summary>
        private const int RECONNECT_WAIT_SECOND = 60;
        /// <summary>
        /// 第二次重新连接等待时长
        /// </summary>
        private const int RENEW_WAIT_SECOND = 120;
        /// <summary>
        /// 定时器间隔时长秒
        /// </summary>
        private const int TIMER_INTERVAL_SECOND = 5;
        /// <summary>
        /// 接收消息回调事件
        /// </summary>
        public event Action<string> event_message;
        /// <summary>AppLogger
        /// websocket连接成功回调事件
        /// </summary>
        public event Action<EventArgs> event_open;

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="host">地址</param>
        /// <param name="logger">日志接口</param>
        public FtxWebSocket(string host = "wss://ftx.com/ws/", ILogger logger = null)
        {
            this.api_host = host;
            this.logger = logger ?? NullLogger.Instance;
            timer = new Timer(TIMER_INTERVAL_SECOND * 1000);
            timer.Elapsed += TimerElapsed;
            InitializeWebSocket();
        }

        /// <summary>
        /// 超时定时器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                double elapsedSecond = (DateTime.UtcNow - last_received_time).TotalSeconds;
                if (elapsedSecond > RECONNECT_WAIT_SECOND && elapsedSecond <= RENEW_WAIT_SECOND)
                {
                    logger.LogInformation(event_id, "WebSocket reconnecting...");
                    websocket.Close();
                    System.Threading.Thread.Sleep(100);
                    websocket.Connect();
                }
                else if (elapsedSecond > RENEW_WAIT_SECOND)
                {
                    logger.LogInformation(event_id, "WebSocket re-initialize...");
                    Disconnect();
                    UninitializeWebSocket();
                    InitializeWebSocket();
                    Connect();
                }
            }
            catch (System.Exception ex)
            {
                logger.LogError(event_id, ex, "Ftx SDK websocket超时重连报错");
            }
        }

        /// <summary>
        /// 初始化websocket
        /// </summary>
        private void InitializeWebSocket()
        {
            websocket = new WebSocket(this.api_host);
            //_WebSocket.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.None;
            websocket.SslConfiguration.EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls | SslProtocols.Tls12;
            websocket.OnError += WebSocket_OnError;
            websocket.OnOpen += WebSocket_OnOpen;
            last_received_time = DateTime.UtcNow;
        }

        /// <summary>
        /// 取消websocket连接
        /// </summary>
        private void UninitializeWebSocket()
        {
            websocket.OnOpen -= WebSocket_OnOpen;
            websocket.OnError -= WebSocket_OnError;
            websocket = null;
        }

        /// <summary>
        /// 连接到Websocket服务器
        /// </summary>
        /// <param name="autoConnect">断开连接后是否自动连接到服务器</param>
        public void Connect(bool autoConnect = true)
        {
            websocket.OnMessage += WebSocket_OnMessage;

            websocket.Connect();

            auto_connect = autoConnect;
            if (auto_connect)
            {
                timer.Enabled = true;
            }
        }

        /// <summary>
        /// 断开与Websocket服务器的连接
        /// </summary>
        public void Disconnect()
        {
            timer.Enabled = false;
            websocket.OnMessage -= WebSocket_OnMessage;
            websocket.Close(CloseStatusCode.Normal);
        }

        /// <summary>
        /// websocket连接成功回调函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WebSocket_OnOpen(object sender, EventArgs e)
        {
            logger.LogDebug(event_id, "WebSocket opened");
            last_received_time = DateTime.UtcNow;
            if (this.event_open != null)
            {
                this.event_open(e);
            }
        }

        /// <summary>
        /// websocket 接收到消息回调函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WebSocket_OnMessage(object sender, MessageEventArgs e)
        {
            last_received_time = DateTime.UtcNow;
            string data = e.Data;
            if (e.IsBinary)
            {
                data = Util.Util.Decompress(e.RawData);
            }
            if (this.event_message != null)
            {
                this.event_message(data);
            }
        }

        /// <summary>
        /// websocket 出错回调函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WebSocket_OnError(object sender, ErrorEventArgs e)
        {
            logger.LogError(event_id, $"WebSocket error: {e.Message}");
        }

        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="obj"></param>
        public void Send(Object obj)
        {
            if (websocket.ReadyState == WebSocketState.Open)
            {
                if (obj is string)
                {
                    websocket.Send(obj.ToString());

                }
                else
                {
                    websocket.Send(JsonConvert.SerializeObject(obj));
                }
            }
        }

        /// <summary>
        /// 获取权限
        /// </summary>
        /// <param name="api_key">key</param>
        /// <param name="api_secret">密钥</param>
        /// <returns></returns>
        public string GetAuthRequest(string api_key, string api_secret)
        {
            var time = Util.Util.GetMillisecondsFromEpochStart();
            var sig = GenerateSignature(api_secret, time);
            var s = "{" +
                    "\"args\": {" +
                    $"\"key\": \"{api_key}\"," +
                    $"\"sign\": \"{sig}\"," +
                    $"\"time\": {time}" +
                    "}," +
                    "\"op\": \"login\"}";
            return s;
        }

        /// <summary>
        /// 生成令牌
        /// </summary>
        /// <param name="api_secret">密钥</param>
        /// <param name="time">时间</param>
        /// <returns></returns>
        private string GenerateSignature(string api_secret, long time)
        {
            var _hashMaker = new HMACSHA256(Encoding.UTF8.GetBytes(api_secret));
            var signature = $"{time}websocket_login";
            var hash = _hashMaker.ComputeHash(Encoding.UTF8.GetBytes(signature));
            var hashStringBase64 = BitConverter.ToString(hash).Replace("-", string.Empty);
            return hashStringBase64.ToLower();
        }

        /// <summary>
        /// 订阅
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public string GetSubscribeRequest(string channel)
        {
            return $"{{\"op\": \"subscribe\", \"channel\": \"{channel}\"}}";
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public string GetUnsubscribeRequest(string channel)
        {
            return $"{{\"op\": \"unsubscribe\", \"channel\": \"{channel}\"}}";
        }

        /// <summary>
        /// 订阅
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="instrument"></param>
        /// <returns></returns>
        public static string GetSubscribeRequest(string channel, string instrument)
        {
            return $"{{\"op\": \"subscribe\", \"channel\": \"{channel}\", \"market\": \"{instrument}\"}}";
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="instrument"></param>
        /// <returns></returns>
        public static string GetUnsubscribeRequest(string channel, string instrument)
        {
            return $"{{\"op\": \"unsubscribe\", \"channel\": \"{channel}\", \"market\": \"{instrument}\"}}";
        }

    }
}
