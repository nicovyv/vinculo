using System;

namespace Vinculo.Utils
{
    public static class GeoUtils
    {
        // Devuelve la distancia en Kilómetros entre dos puntos GPS
        public static double CalcularDistanciaKm(double? lat1, double? lon1, double? lat2, double? lon2)
        {
            // Si a la empresa o a la escuela le faltan coordenadas, retornamos un valor muy alto 
            // para que queden al final de la lista.
            if (!lat1.HasValue || !lon1.HasValue || !lat2.HasValue || !lon2.HasValue)
                return double.MaxValue;

            const double radioTierraKm = 6371.0;

            double dLat = (lat2.Value - lat1.Value) * Math.PI / 180.0;
            double dLon = (lon2.Value - lon1.Value) * Math.PI / 180.0;

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1.Value * Math.PI / 180.0) * Math.Cos(lat2.Value * Math.PI / 180.0) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return radioTierraKm * c;
        }
    }
}