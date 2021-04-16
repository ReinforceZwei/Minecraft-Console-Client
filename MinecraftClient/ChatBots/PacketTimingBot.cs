using System;
namespace MinecraftClient.ChatBots
{
    public class PacketTimingBot : ChatBot
    {
        public override bool OnDisconnect(DisconnectReason reason, string message)
        {
            LogToConsole("Disconnected from server. Start generating report");
            Protocol.PacketTiming.StopCollecting();
            Protocol.PacketTiming.GenerateReport("packet-timing-report.txt");
            LogToConsole("Report generated");
            return base.OnDisconnect(reason, message);
        }
    }
}
