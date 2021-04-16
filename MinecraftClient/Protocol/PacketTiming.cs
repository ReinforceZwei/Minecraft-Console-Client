using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;
using MinecraftClient.Protocol.Handlers;
namespace MinecraftClient.Protocol
{
    public static class PacketTiming
    {
        private static Stopwatch stopwatch = new Stopwatch();
        private static PacketTypesIn currentPacket;
        private static bool stopCollecting = false;
        public static List<KeyValuePair<PacketTypesIn, long>> TimingResult = new List<KeyValuePair<PacketTypesIn, long>>();

        public static void Start(PacketTypesIn packetType)
        {
            if (stopCollecting) return;
            currentPacket = packetType;
            stopwatch.Start();
        }

        public static void Stop()
        {
            if (stopCollecting) return;
            stopwatch.Stop();
            var elapsed = stopwatch.ElapsedMilliseconds;
            TimingResult.Add(new KeyValuePair<PacketTypesIn, long>(currentPacket, elapsed));
            stopwatch.Reset();
        }

        public static void StopCollecting()
        {
            stopCollecting = true;
        }

        public static string GenerateReport()
        {
            Dictionary<PacketTypesIn, long> timeSum = new Dictionary<PacketTypesIn, long>();
            Dictionary<PacketTypesIn, int> packetCount = new Dictionary<PacketTypesIn, int>();
            foreach (var x in TimingResult)
            {
                if (timeSum.ContainsKey(x.Key))
                    timeSum[x.Key] += x.Value;
                else
                    timeSum.Add(x.Key, x.Value);

                if (packetCount.ContainsKey(x.Key))
                    packetCount[x.Key]++;
                else
                    packetCount.Add(x.Key, 1);
            }
            StringBuilder sb = new StringBuilder();
            var sorted = timeSum.OrderByDescending(x => x.Value);
            foreach (var x in sorted)
            {
                sb.AppendLine(String.Format("{0} packet(s) of {1} took {2}ms", packetCount[x.Key], x.Key.ToString(), x.Value));
            }
            return sb.ToString();
        }

        public static void GenerateReport(string path)
        {
            File.WriteAllText(path, GenerateReport());
        }
    }
}
