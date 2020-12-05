using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MinecraftClient.Mapping;

namespace MinecraftClient.ChatBots
{
    class Look : ChatBot
    {
        Vector3 vector;
        public override void Initialize()
        {
            RegisterChatBotCommand("tt", "", "", c);
        }

        public string c(string cmd, string[] args)
        {
            ConsoleIO.WriteLine(string.Format("Pitch: {0} Yaw: {1}\nEye: {2}", 
                GetPitch(), 
                GetYaw(),
                GetCurrentLocation().EyesLocation().ToString()));
            double ya = Single.Parse(args[0]);
            double p = Single.Parse(args[1]);
            double rotX = ToRadians(ya);
            double rotY = ToRadians(p);
            double x = -Math.Cos(rotY) * Math.Sin(rotX);
            double y = -Math.Sin(rotY);
            double z = Math.Cos(rotY) * Math.Cos(rotX);
            Vector3 vector = new Vector3((float)x, (float)y, (float)z);
            for (int i = 0; i < 5; i++)
            {
                Vector3 v = vector.Multiply(i);
                Location l = GetCurrentLocation().EyesLocation() + new Location(v.X, v.Y, v.Z);
                l.X = Math.Floor(l.X);
                l.Y = Math.Floor(l.Y);
                l.Z = Math.Floor(l.Z);
                Block b = GetWorld().GetBlock(l);
                ConsoleIO.WriteLine(l.ToString() + " " + b.Type.ToString());
                if (b.Type != Material.Air)
                {
                    l.Y++;
                    SendPlaceBlock(l, Direction.Up);
                    //DigBlock(l);
                    break;
                }
            }
            return vector.ToString();
        }

        public double ToRadians(double angle)
        {
            return (Math.PI / 180) * angle;
        }

        public Location LookingAtBlock(double yaw, double pitch)
        {
            double rotX = (Math.PI / 180) * yaw;
            double rotY = (Math.PI / 180) * pitch;
            double x = -Math.Cos(rotY) * Math.Sin(rotX);
            double y = -Math.Sin(rotY);
            double z = Math.Cos(rotY) * Math.Cos(rotX);
            Vector3 vector = new Vector3(x, y, z);
            for (int i = 0; i < 5; i++)
            {
                Vector3 v = vector.Multiply(i);
                Location l = GetCurrentLocation().EyesLocation() + new Location(v.X, v.Y, v.Z);
                l.X = Math.Floor(l.X);
                l.Y = Math.Floor(l.Y);
                l.Z = Math.Floor(l.Z);
                Block b = GetWorld().GetBlock(l);
                if (b.Type != Material.Air)
                {
                    return l;
                }
            }
            return new Location();
        }
    }
}
