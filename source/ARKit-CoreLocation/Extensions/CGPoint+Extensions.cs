using System;
using CoreGraphics;
using SceneKit;

namespace ARCL.Extensions
{
    public static class CGPointFactory
    {
        public static CGPoint PointWithVector(SCNVector3 vector)
        {
            return new CGPoint(vector.X, 0 - vector.Z);
        }
    }

    public static class CGPoint_Extensions
    {
        public static bool RadiusContainsPoint(this CGPoint self, double radius, CGPoint point)
        {
            var x = Math.Pow(point.X - self.X, 2);
            var y = Math.Pow(point.Y - self.Y, 2);
            var radiusSquared = Math.Pow(radius, 2);
            return x + y <= radiusSquared;
        }
    }
}
