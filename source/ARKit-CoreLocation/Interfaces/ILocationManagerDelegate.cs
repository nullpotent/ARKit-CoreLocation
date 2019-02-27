using CoreLocation;

namespace ARCL
{
    public interface ILocationManagerDelegate
    {
        void LocationManagerDidUpdateLocation(LocationManager locationManager, CLLocation location);

        void LocationManagerDidUpdateHeading(LocationManager locationManager, double heading, double accuracy);
    }
}
