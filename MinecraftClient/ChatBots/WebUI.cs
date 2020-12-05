using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace MinecraftClient.ChatBots
{
    class WebUI : ChatBot
    {
        private int serverPort = 8080;
        private HttpServer http;
        private WebSocketServiceHost host;

        // Preserve some amount of messages to be sent to web client after connected
        public int MessageRelayLimit = 50;
        public Queue<string> MessageCache;

        public WebUI()
        {
            MessageCache = new Queue<string>(MessageRelayLimit);
            http = new HttpServer(serverPort);
            http.OnGet += Http_OnGet;
            http.AddWebSocketService<DataChannel>("/data", delegate(DataChannel d) { d.SetRef(this); });
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
        }

        public void EnqueueMessage(string msg)
        {
            if (MessageCache.Count >= MessageRelayLimit)
            {
                MessageCache.Dequeue();
            }
            MessageCache.Enqueue(msg);
        }

        public override void GetText(string text)
        {
            EnqueueMessage(text);
            host.Sessions.Broadcast("chat|" + text);
        }

        private void Http_OnGet(object sender, HttpRequestEventArgs e)
        {
            var req = e.Request;
            var res = e.Response;

            string path = req.RawUrl;
            if (req.RawUrl == "/")
                path = "index";
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

            byte[] content = Encoding.UTF8.GetBytes(WebUIResource.ResourceManager.GetString(path));
            res.ContentType = "text/html";
            res.ContentLength64 = content.Length;
            res.Close(content, true);
        }
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
                Send("chat|" + msg);
            }
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            if (ConnectionState != WebSocketState.Open)
            {
                ConsoleIO.WriteLine("Ws connection not up but data incoming");
                return;
            }
            ConsoleIO.WriteLine(string.Format("Got ws msg: {0}", e.Data));
            string[] cmd = e.Data.Split(new char[] { '|' }, 2);
            string action = "";
            string data = "";
            if (cmd.Length >= 2)
            {
                action = cmd[0];
                data = cmd[1];
            }
            else if (cmd.Length == 1)
            {
                action = cmd[0];
            }
            ConsoleIO.WriteLine(string.Format("action: {0}, data: {1}", action, data));
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
                                Send("chat|" + response_msg);
                            }
                        }
                        else botEvent.SendText(text);
                    }
                    return;

                case "ping":
                    Send("pong");
                    break;

                default: 
                    Send(e.Data); 
                    break;
            }
        }
    }
}
