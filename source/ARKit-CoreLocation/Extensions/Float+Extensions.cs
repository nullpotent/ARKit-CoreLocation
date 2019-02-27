using System;

namespace ARCL.Extensions
{
    public static class Float_Extensions
    {
        public static float DegreesToRadians(this float self)
        {
            return (float)(self * Math.PI / 180.0);
        }

        public static float RadiansToDegrees(this float self)
        {
            return (float)(self * 180.0 / Math.PI);
        }
    }
}
