using System;
using CoreLocation;
using Foundation;

namespace ARCL.Extensions
{
    public static class CLLocationFactory
    {
        public static CLLocation Create(CLLocationCoordinate2D coordinate, double altitude)
        {
            return new CLLocation(coordinate, altitude, hAccuracy: 0, vAccuracy: 0, timestamp: new NSDate());
        }
    }

    public static class CLLocation_Extensions
    {
        const double earthRadius = 6371000.0;

        /// <summary>
        /// Translates distance in meters between two locations.
        /// Returns the result as the distance in latitude and distance in longitude.
        /// </summary>
        /// <returns>The translation.</returns>
        /// <param name="self">Self.</param>
        /// <param name="toLocation">To location.</param>
        public static LocationTranslation Translation(this CLLocation self, CLLocation toLocation)
        {
            var inbetweenLocation = new CLLocation(latitude: self.Coordinate.Latitude, longitude: toLocation.Coordinate.Longitude);

            var distanceLatitude = toLocation.DistanceFrom(inbetweenLocation);
            var latitudeTranslation = default(double);

            if (toLocation.Coordinate.Latitude > inbetweenLocation.Coordinate.Latitude)
            {
                latitudeTranslation = distanceLatitude;
            }
            else
            {
                latitudeTranslation = 0 - distanceLatitude;
            }

            var distanceLongitude = self.DistanceFrom(inbetweenLocation);
            var longitudeTranslation = default(double);

            if (self.Coordinate.Longitude > inbetweenLocation.Coordinate.Longitude)
            {
                longitudeTranslation = 0 - distanceLongitude;
            }
            else
            {
                longitudeTranslation = distanceLongitude;
            }

            var altitudeTranslation = toLocation.Altitude - self.Altitude;

            return new LocationTranslation(latitudeTranslation, longitudeTranslation, altitudeTranslation);
        }

        /// <summary>
        /// Translateds the location.
        /// </summary>
        /// <returns>The location.</returns>
        /// <param name="self">Self.</param>
        /// <param name="translation">Translation.</param>
        public static CLLocation TranslatedLocation(this CLLocation self, LocationTranslation translation)
        {
            var latitudeCoordinate = self.Coordinate.CoordinateWithBearing(bearing: 0, distanceMeters: translation.LatitudeTranslation);
            var longitudeCoordinate = self.Coordinate.CoordinateWithBearing(bearing: 90, distanceMeters: translation.LongitudeTranslation);
            var coordinate = new CLLocationCoordinate2D(latitudeCoordinate.Latitude, longitudeCoordinate.Longitude);
            var altitude = self.Altitude + translation.AltitudeTranslation;
            return new CLLocation(coordinate: coordinate, altitude: altitude, hAccuracy: self.HorizontalAccuracy, vAccuracy: self.VerticalAccuracy, timestamp: self.Timestamp);
        }

        /// <summary>
        /// Formula by http://www.movable-type.co.uk/scripts/latlong.html
        /// </summary>
        /// <returns>The with bearing.</returns>
        /// <param name="bearing">Bearing.</param>
        /// <param name="distanceMeters">Distance meters.</param>
        public static CLLocationCoordinate2D CoordinateWithBearing(this CLLocationCoordinate2D self, double bearing, double distanceMeters)
        {
            var lat1 = self.Latitude * Math.PI / 180;
            var lon1 = self.Longitude * Math.PI / 180;

            var distance = distanceMeters / earthRadius;
            var angularBering = bearing * Math.PI / 180;

            var lat2 = lat1 + (distance * Math.Cos(angularBering));
            var dLat = lat2 - lat1;
            var dPhi = Math.Log(Math.Tan((lat2 / 2) + (Math.PI / 4)) / Math.Tan((lat1 / 2) + (Math.PI / 4)));
            var q = (Math.Abs(dPhi) > double.Epsilon) ? dLat / dPhi : Math.Cos(lat1);
            var dLon = distance * Math.Sin(angularBering) / q;

            if (Math.Abs(lat2) > Math.PI / 2)
            {
                lat2 = lat2 > 0 ? Math.PI - lat2 : -(Math.PI - lat2);
            }
            var lon2 = lon1 + dLon + (3 * Math.PI);
            while (lon2 > 2 * Math.PI)
            {
                lon2 -= 2 * Math.PI;
            }
            lon2 -= Math.PI;

            return new CLLocationCoordinate2D(latitude: lat2 * 180 / Math.PI, longitude: lon2 * 180 / Math.PI);
        }
    }
}
