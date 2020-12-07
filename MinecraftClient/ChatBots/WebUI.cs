using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Reflection;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace MinecraftClient.ChatBots
{
    public class WebUIResourceLoader
    {
        public WebUIResourceLoader()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainAssemblyResolve;
        }

        private Assembly CurrentDomainAssemblyResolve(object sender, ResolveEventArgs args)
        {
            string name = new AssemblyName(args.Name).Name;
            if (name == "websocket-sharp")
                return Assembly.Load(WebUIResource.websocket_sharp);
            else return null;
        }
    }

    // TODO: 
    // Handle disconnect message
    // Handle MCC quit by user
    // Start WebUI more early
    // WebUI authentication
    // SSL
    // fallback data channel for WebSocket
    class WebUI : ChatBot
    {
        private int serverPort = 8080;
        private HttpServer http;
        private WebSocketServiceHost host;

        // Preserve some amount of messages to be sent to web client after connected
        public int MessageRelayLimit = 50;
        public Queue<string> MessageCache;

        public McClient ClientHandler;

        public WebUI(McClient c)
        {
            new WebUIResourceLoader();
            ClientHandler = c;
        }

        public override void Initialize()
        {
            MessageCache = new Queue<string>(MessageRelayLimit);
            ConsoleIO.WriteLineEvent += ConsoleIOWriteLineEvent;
            http = new HttpServer(serverPort);
            http.OnGet += HttpOnGet;
            http.AddWebSocketService<DataChannel>("/data", delegate (DataChannel d) { d.SetRef(this); });
            http.WebSocketServices.TryGetServiceHost("/data", out host);
            http.Start();
            if (http.IsListening)
            {
                ConsoleIO.WriteLine("Http server listening on port " + serverPort);
            }
            else
            {
                ConsoleIO.WriteLine("Http server is not listening. Something went wrong?");
            }
            ConsoleIO.WriteLine("Json test");
            ConsoleIO.WriteLine(JsonMaker.ToArray(new List<string> { "A", "b", "c", "as\ndasd" }));
            ConsoleIO.WriteLine(JsonMaker.ToArray(new List<int> { 1, 2, 3, 444 }));
            ConsoleIO.WriteLine(JsonMaker.ToArray(new List<float> { 1.11f, 2.123f, 3.1415f, 444 }));
            
            var dict2 = new Dictionary<string, object>()
            {
                {"Player1", "Me" },
                {"Player2", "RF" },
                {"Next", "HAHA" },
            };
            var dict = new Dictionary<string, object>()
            {
                {"Key", "This is value" },
                {"Max", 12 },
                {"This is crazy", dict2 },
                {"More", new List<float> { 1.11f, 2.123f, 3.1415f, 444 } }
            };
            var list = new List<Dictionary<string, object>>()
            {
                dict, dict2
            };
            ConsoleIO.WriteLine(JsonMaker.ToJson(dict));
            ConsoleIO.WriteLine(JsonMaker.ToArray(list));
        }

        private void ConsoleIOWriteLineEvent(string text)
        {
            EnqueueMessage(text);
            host.Sessions.Broadcast(DataExchange.ToClient("chat", text));
        }

        public void EnqueueMessage(string msg)
        {
            if (MessageCache.Count >= MessageRelayLimit)
            {
                MessageCache.Dequeue();
            }
            MessageCache.Enqueue(msg);
        }

        private void HttpOnGet(object sender, HttpRequestEventArgs e)
        {
            var req = e.Request;
            var res = e.Response;

            string path = req.RawUrl;
            if (path == "/")
            {
                path = "index";
                byte[] content = Encoding.UTF8.GetBytes(WebUIResource.ResourceManager.GetString(path));
                res.ContentType = "text/html";
                res.ContentLength64 = content.Length;
                res.Close(content, true);
                return;
            }
            else if (path == "/favicon.ico")
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    WebUIResource.AppIcon.Save(ms);
                    byte[] ico = ms.ToArray();
                    ms.Close();
                    res.ContentType = "image/x-icon";
                    res.ContentLength64 = ico.Length;
                    res.Close(ico, true);
                    return;
                }
            }
            else
            {
                res.StatusCode = 404;
                res.Close();
                return;
            }
        }

        #region Event handler

        public override void GetText(string text)
        {
            //EnqueueMessage(text);
            //host.Sessions.Broadcast(DataExchange.ToClient("chat", text));
        }

        #endregion
    }

    class DataChannel : WebSocketBehavior
    {
        WebUI botEvent;
        public DataChannel() : this(null) { }
        public DataChannel(WebUI eventRef)
        {
            botEvent = eventRef;
        }

        public void SetRef(WebUI w)
        {
            botEvent = w;
        }

        protected override void OnOpen()
        {
            foreach (string msg in botEvent.MessageCache)
            {
                Send(DataExchange.ToClient("chat", msg));
            }
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            if (ConnectionState != WebSocketState.Open)
            {
                ConsoleIO.WriteLine("Ws connection not up but data incoming");
                return;
            }
            try
            {
                //ConsoleIO.WriteLine(string.Format("Got ws msg: {0}", e.Data));
                var pair = DataExchange.FromClient(e.Data);
                string action = pair.Key;
                string data = pair.Value;
                //ConsoleIO.WriteLine(string.Format("action: {0}, data: {1}", action, data));
                switch (action.ToLower())
                {
                    case "input":
                        string text = data.Trim();
                        if (text.Length > 0)
                        {
                            if (Settings.internalCmdChar == ' ' || text[0] == Settings.internalCmdChar)
                            {
                                string response_msg = "";
                                string command = Settings.internalCmdChar == ' ' ? text : text.Substring(1);
                                if (!botEvent.PerformInternalCommand(Settings.ExpandVars(command), ref response_msg) && Settings.internalCmdChar == '/')
                                {
                                    botEvent.SendText(text);
                                }
                                else if (response_msg.Length > 0)
                                {
                                    Send(DataExchange.ToClient("chat", response_msg));
                                }
                            }
                            else botEvent.SendText(text);
                        }
                        return;

                    case "ping":
                        Send(DataExchange.ToClient("pong", ""));
                        break;

                    default:
                        Send(e.Data);
                        break;
                }
            }
            catch (Exception exception)
            {
                ConsoleIO.WriteLine("Got exception while handling WebClient message:\n" + exception.Message);
            }
        }
    }

    /// <summary>
    /// Handle JSON data format between server and client and provide standard data format
    /// </summary>
    static class DataExchange
    {
        public static string ToClient(string action, string payload)
        {
            var dict = new Dictionary<string, object>()
            {
                { "action", action },
                { "data", payload },
            };
            return JsonMaker.ToJson(dict);
        }

        public static KeyValuePair<string, string> FromClient(string rawData)
        {
            var json = Json.ParseJson(rawData);
            string action = json.Properties["action"].StringValue;
            string data = "";
            Json.JSONData obj;
            if (json.Properties.TryGetValue("data", out obj))
            {
                data = obj.StringValue;
            }
            return new KeyValuePair<string, string>(action, data);
        }
    }

    /// <summary>
    /// A super simple JSON serializer
    /// By ReinforceZwei
    /// </summary>
    static class JsonMaker
    {
        /// <summary>
        /// Convert a list of string to JSON array string
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static string ToArray(List<string> array)
        {
            StringBuilder json = new StringBuilder();
            json.Append("[");
            for (int i = 0; i < array.Count; i++)
            {
                string item = EscapeString(array[i]);
                if (i != 0) json.Append(", ");
                json.Append("\"" + item + "\"");
            }
            json.Append("]");
            return json.ToString();
        }

        /// <summary>
        /// Convert a list of number to JSON array string
        /// </summary>
        /// <typeparam name="T">Any numeric type</typeparam>
        /// <param name="array"></param>
        /// <returns>String of JSON array</returns>
        public static string ToArray<T>(List<T> array) where T : struct, IComparable<T>
        {
            StringBuilder json = new StringBuilder();
            json.Append("[");
            for (int i = 0; i < array.Count; i++)
            {
                if (i != 0) json.Append(", ");
                json.Append(array[i]);
            }
            json.Append("]");
            return json.ToString();
        }

        public static string ToArray(List<Dictionary<string, object>> array)
        {
            StringBuilder json = new StringBuilder();
            json.Append("[");
            for (int i = 0; i < array.Count; i++)
            {
                if (i != 0) json.Append(", ");
                json.Append(ToJson(array[i]));
            }
            json.Append("]");
            return json.ToString();
        }

        /// <summary>
        /// Convert a dictionary to JSON. Only accept string as key. 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string ToJson(Dictionary<string, object> obj)
        {
            StringBuilder json = new StringBuilder();
            json.Append("{");
            foreach (var pair in obj)
            {
                json.Append("\"" + pair.Key + "\":");
                if (pair.Value is string)
                {
                    json.Append("\"" + EscapeString((string)pair.Value) + "\"");
                }
                else if (pair.Value is byte
                    || pair.Value is sbyte
                    || pair.Value is short
                    || pair.Value is ushort
                    || pair.Value is int
                    || pair.Value is decimal
                    || pair.Value is double
                    || pair.Value is float)
                {
                    json.Append(pair.Value);
                }
                else if (pair.Value is IEnumerable<string>)
                {
                    json.Append(ToArray(((IEnumerable<string>)pair.Value).ToList()));
                }
                else if (pair.Value is IEnumerable<int>)
                {
                    json.Append(ToArray(((IEnumerable<int>)pair.Value).ToList()));
                }
                else if (pair.Value is IEnumerable<decimal>)
                {
                    json.Append(ToArray(((IEnumerable<decimal>)pair.Value).ToList()));
                }
                else if (pair.Value is IEnumerable<float>)
                {
                    json.Append(ToArray(((IEnumerable<float>)pair.Value).ToList()));
                }
                else if (pair.Value is IEnumerable<double>)
                {
                    json.Append(ToArray(((IEnumerable<double>)pair.Value).ToList()));
                }
                else if (pair.Value is List<Dictionary<string, object>>)
                {
                    json.Append(ToArray((List<Dictionary<string, object>>)pair.Value));
                }
                else if (pair.Value is Dictionary<string, object>
                    || pair.Value is Dictionary<string, string>
                    || pair.Value is Dictionary<string, int>
                    || pair.Value is Dictionary<string, float>
                    || pair.Value is Dictionary<string, decimal>
                    || pair.Value is Dictionary<string, double>)
                {
                    json.Append(ToJson((Dictionary<string, object>)pair.Value));
                }

                if (!obj[pair.Key].Equals(obj.Last().Value))
                    json.Append(",");
            }
            json.Append("}");
            return json.ToString();
        }

        private static string EscapeString(string str)
        {
            return str.Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t")
                .Replace("\r", "\\r")
                .Replace("\b", "\\b")
                .Replace("\f", "\\f");
        }
    }
}
