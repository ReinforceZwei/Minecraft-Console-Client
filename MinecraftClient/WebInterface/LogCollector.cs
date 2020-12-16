using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.WebInterface
{
    public class LogCollector : ILogger
    {
        private bool debugEnabled = false;
        private bool warnEnabled = true;
        private bool infoEnabled = true;
        private bool errorEnabled = true;
        public bool DebugEnabled { get => debugEnabled; set => debugEnabled = value; }
        public bool WarnEnabled { get => warnEnabled; set => warnEnabled = value; }
        public bool InfoEnabled { get => infoEnabled; set => infoEnabled = value; }
        public bool ErrorEnabled { get => errorEnabled; set => errorEnabled = value; }

        private int instanceID;
        public int GetInstanceID { get => instanceID; }
        

        public LogCollector()
        {
            instanceID = new Random().Next(0, 9999);
        }
        public LogCollector(int uniqueID)
        {
            instanceID = uniqueID;
        }

        public event EventHandler<OnLogEventArgs> OnLog;

        public void Debug(string msg)
        {
            if (debugEnabled)
                Info("§8" + msg);
        }

        public void Debug(string msg, params object[] args)
        {
            if (debugEnabled)
                Info("§8" + msg, args);
        }

        public void Debug(object msg)
        {
            if (debugEnabled)
                Info("§8" + msg.ToString());
        }

        public void Info(object msg)
        {
            ConsoleIO.WriteLineFormatted(msg.ToString());
            OnLog?.Invoke(this, new OnLogEventArgs()
            {
                InstanceID = instanceID,
                Text = msg.ToString()
            });
        }

        public void Info(string msg)
        {
            ConsoleIO.WriteLineFormatted(msg);
            OnLog?.Invoke(this, new OnLogEventArgs()
            {
                InstanceID = instanceID,
                Text = msg
            });
        }

        public void Info(string msg, params object[] args)
        {
            var final = string.Format(msg, args);
            ConsoleIO.WriteLineFormatted(final);
            OnLog?.Invoke(this, new OnLogEventArgs()
            {
                InstanceID = instanceID,
                Text = final
            });
        }

        public void Warn(string msg)
        {
            Info("§6" + msg);
        }

        public void Warn(string msg, params object[] args)
        {
            Info("§6" + msg, args);
        }

        public void Warn(object msg)
        {
            Info("§6" + msg.ToString());
        }

        public void Error(string msg)
        {
            Info("§c" + msg);
        }

        public void Error(string msg, params object[] args)
        {
            Info("§c" + msg, args);
        }

        public void Error(object msg)
        {
            Info("§c" + msg.ToString());
        }

        public class OnLogEventArgs : EventArgs
        {
            public int InstanceID { get; set; }
            public string Text { get; set; }
        }
    }
}
