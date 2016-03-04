using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Device.Location;

namespace iNetworkPhoneClient
{
    public class MapFunctions
    {
        public const double EarthRadius = 6371; // in km

        public static double GetDistanceFromLatLon(GeoCoordinate point1, GeoCoordinate point2)
        {
            double dLat = DegreesToRadian(point2.Latitude - point1.Latitude);
            double dLon = DegreesToRadian(point2.Longitude - point1.Longitude);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(DegreesToRadian(point1.Latitude)) * Math.Cos(DegreesToRadian(point2.Latitude)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return EarthRadius * c; // Distance in km
        }

        public static TimeSpan CalculateTimeToTravelDistance(double distance, double speed)
        {
            return TimeSpan.FromHours(distance / speed);
        }

        public static double DegreesToRadian(double degrees)
        {
            return degrees * (Math.PI / 180.0);
        }

    }
}
