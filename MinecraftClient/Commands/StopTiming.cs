using System;
using System.Collections.Generic;

namespace MinecraftClient.Commands
{
    public class StopTiming : Command
    {
        public override string CmdName
        {
            get
            {
                return "stoptime";
            }
        }

        public override string CmdDesc
        {
            get { return "Stop Packet timing"; }
        }

        public override string CmdUsage { get { return "N/A"; } }

        public override string Run(McClient handler, string command, Dictionary<string, object> localVars)
        {
            Protocol.PacketTiming.StopCollecting();
            Protocol.PacketTiming.GenerateReport("packet-timing-report.txt");
            return "Report generated";
        }
    }
}
