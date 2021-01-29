using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MinecraftClient.Protocol.Handlers;
using MinecraftClient.Protocol.Handlers.PacketPalettes;

namespace MinecraftClient.ChatBots
{
    class PacketDump : ChatBot
    {
        PacketTypePalette p;
        DataTypes d;

        PacketTypesIn[] wanted =
        {
            PacketTypesIn.Respawn,
            PacketTypesIn.WindowItems,
            PacketTypesIn.SetSlot,
            PacketTypesIn.OpenWindow,
            PacketTypesIn.CloseWindow
        };

        public override void Initialize()
        {
            SetNetworkPacketEventEnabled(true);
            p = new PacketTypeHandler(GetProtocolVersion()).GetTypeHandler();
            d = new DataTypes(GetProtocolVersion());
        }

        public override void OnNetworkPacket(int packetID, List<byte> packetData, bool isLogin, bool isInbound)
        {
            var n = p.GetIncommingTypeById(packetID);
            if (!isLogin && isInbound && wanted.Contains(n))
            {
                switch (n)
                {
                    case PacketTypesIn.CloseWindow:
                        int id = d.ReadNextByte(new Queue<byte>(packetData));
                        LogToConsole(string.Format(">>> {0} - #{1}", n, id));
                        break;
                        
                    default:
                        LogToConsole(string.Format(">>> {0}", n));
                        break;
                }
            }
        }
    }
}
