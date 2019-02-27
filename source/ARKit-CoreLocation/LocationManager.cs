using System;
using System.Diagnostics;
using CoreLocation;

namespace ARCL
{
    /// <summary>
    /// Handles retrieving the location and heading from CoreLocation
    /// Does not contain anything related to ARKit or advanced location
    /// </summary>
    public class LocationManager : CLLocationManagerDelegate
    {
        readonly CLLocationManager locationManager;

        WeakReference<ILocationManagerDelegate> weakDelegate;

        public ILocationManagerDelegate Delegate
        {
            get => weakDelegate.TryGetTarget(out var @delegate) ? @delegate : null;
            set => weakDelegate = new WeakReference<ILocationManagerDelegate>(value);
        }

        public CLLocation CurrentLocation { get; private set; }

        public double Heading { get; private set; }

        public double HeadingAccuracy { get; private set; }

        public LocationManager()
        {
            locationManager = new CLLocationManager
            {
                DesiredAccuracy = CLLocation.AccurracyBestForNavigation,
                DistanceFilter = CLLocationDistance.FilterNone,
                PausesLocationUpdatesAutomatically = false,
                HeadingFilter = CLLocationDistance.FilterNone,
                Delegate = this
            };

            Debug.WriteLine("HeadingFilter => " + locationManager.HeadingFilter);

            locationManager.StartUpdatingHeading();
            locationManager.StartUpdatingLocation();
            locationManager.RequestWhenInUseAuthorization();
            CurrentLocation = locationManager.Location;
        }

        public void RequestAuthorization()
        {
            if (CLLocationManager.Status == CLAuthorizationStatus.AuthorizedAlways || CLLocationManager.Status == CLAuthorizationStatus.AuthorizedWhenInUse)
            {
                return;
            }

            if (CLLocationManager.Status == CLAuthorizationStatus.Denied || CLLocationManager.Status == CLAuthorizationStatus.Restricted)
            {
                return;
            }

            locationManager.RequestWhenInUseAuthorization();
        }

        public override void AuthorizationChanged(CLLocationManager manager, CLAuthorizationStatus status)
        {
        }

        public override void LocationsUpdated(CLLocationManager manager, CLLocation[] locations)
        {
            foreach (var location in locations)
            {
                Delegate?.LocationManagerDidUpdateLocation(this, location);
            }

            CurrentLocation = manager.Location;
        }

        public override void UpdatedHeading(CLLocationManager manager, CLHeading newHeading)
        {
            if (newHeading.HeadingAccuracy >= 0)
            {
                Heading = newHeading.TrueHeading;
            }
            else
            {
                Heading = newHeading.MagneticHeading;
            }
            HeadingAccuracy = newHeading.HeadingAccuracy;
            Delegate?.LocationManagerDidUpdateHeading(this, Heading, newHeading.HeadingAccuracy);
        }

        public override bool ShouldDisplayHeadingCalibration(CLLocationManager manager)
        {
            return true;
        }
    }
}
