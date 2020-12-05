using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Mapping
{
    public class Vector3
    {
        public double X;
        public double Y;
        public double Z;
        public Vector3(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3 Add(Vector3 other)
        {
            return new Vector3(X + other.X, Y + other.Y, Z + other.Z);
        }

        public Vector3 Multiply(double factor)
        {
            return new Vector3(X * factor, Y * factor, Z * factor);
        }

        public override string ToString()
        {
            return string.Format("X: {0} Y: {1} Z: {2}", X, Y, Z);
        }
    }
}
