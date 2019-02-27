namespace ARCL
{
    /// <summary>
    /// Translation in meters between 2 locations.
    /// </summary>
    public struct LocationTranslation
    {
        public double LatitudeTranslation { get; }

        public double LongitudeTranslation { get; }

        public double AltitudeTranslation { get; }

        public LocationTranslation(double latitudeTranslation, double longitudeTranslation, double altitudeTranslation)
        {
            LatitudeTranslation = latitudeTranslation;
            LongitudeTranslation = longitudeTranslation;
            AltitudeTranslation = altitudeTranslation;
        }
    }
}
