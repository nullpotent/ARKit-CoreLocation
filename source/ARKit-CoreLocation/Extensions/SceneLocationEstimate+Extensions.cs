using CoreLocation;
using SceneKit;

namespace ARCL.Extensions
{
    public static class SceneLocationEstimate_Extensions
    {
        /// <summary>
        /// Compares the location's position to another position, to determine the translation between them
        /// </summary>
        /// <returns>The translation.</returns>
        /// <param name="self">Self.</param>
        /// <param name="to">To.</param>
        public static LocationTranslation LocationTranslation(this SceneLocationEstimate self, SCNVector3 to)
        {
            return new LocationTranslation(self.Position.Z - to.Z, to.X - self.Position.X, to.Y - self.Position.Y);
        }

        /// <summary>
        /// Translates the location by comparing with a given position.
        /// </summary>
        /// <returns>The location.</returns>
        /// <param name="self">Self.</param>
        /// <param name="to">To.</param>
        public static CLLocation TranslatedLocation(this SceneLocationEstimate self, SCNVector3 to)
        {
            var translation = self.LocationTranslation(to);
            var translatedLocation = self.Location.TranslatedLocation(translation);
            return translatedLocation;
        }
    }
}
