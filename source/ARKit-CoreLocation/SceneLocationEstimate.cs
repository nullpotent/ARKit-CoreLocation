using System;
using CoreLocation;
using SceneKit;

namespace ARCL
{
    public class SceneLocationEstimate
    {
        public CLLocation Location { get; private set; }

        public SCNVector3 Position { get; private set; }

        public SceneLocationEstimate(CLLocation location, SCNVector3 position)
        {
            Location = location;
            Position = position;
        }
    }
}
