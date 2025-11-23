using System;

namespace Proyecto__2_Datos_Arbol_Genealogico.Utils
{
    public static class GeoUtils
    {
        private const double EarthRadiusKm = 6371.0;

        public static double HaversineDistanceKm(double lat1Deg, double lon1Deg, double lat2Deg, double lon2Deg)
        {
            double degToRad = Math.PI / 180.0;
            double φ1 = lat1Deg * degToRad;
            double φ2 = lat2Deg * degToRad;
            double Δφ = (lat2Deg - lat1Deg) * degToRad;
            double Δλ = (lon2Deg - lon1Deg) * degToRad;

            double sinΔφ2 = Math.Sin(Δφ / 2.0);
            double sinΔλ2 = Math.Sin(Δλ / 2.0);

            double a = sinΔφ2 * sinΔφ2 + Math.Cos(φ1) * Math.Cos(φ2) * sinΔλ2 * sinΔλ2;
            double c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
            double d = EarthRadiusKm * c;
            return d;
        }

        public static (double x, double y) LatLonToPixel(double lat, double lon, double imageWidth, double imageHeight)
        {
            lon = Math.Clamp(lon, -180, 180);
            lat = Math.Clamp(lat, -90, 90);

            double x = (lon + 180.0) / 360.0 * imageWidth;
            double y = (90.0 - lat) / 180.0 * imageHeight;
            return (x, y);
        }
    }
}

