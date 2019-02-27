using System;
using SceneKit;

namespace ARCL.Extensions
{
    public static class SCNVector3_Extensions
    {
        public static double Distance(this SCNVector3 self, SCNVector3 to)
        {
            return Math.Sqrt(Math.Pow(to.X - self.X, 2) + Math.Pow(to.Z - self.Z, 2));
        }
    }
}
