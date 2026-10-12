using System;

namespace Vinculo.Utils
{
    public static class GeoUtils
    {
        // Devuelve la distancia en Kilómetros entre dos puntos GPS

        public static double CalcularDistanciaKm(
            double? lat1, double? lon1,
            double? lat2, double? lon2)
        {
            if (!lat1.HasValue || !lon1.HasValue ||
                !lat2.HasValue || !lon2.HasValue)
            {
                return double.MaxValue;
            }

            // Validar que las coordenadas estén dentro de los rangos geográficos.
            if (lat1.Value < -90 || lat1.Value > 90 ||
                lat2.Value < -90 || lat2.Value > 90 ||
                lon1.Value < -180 || lon1.Value > 180 ||
                lon2.Value < -180 || lon2.Value > 180)
            {
                return double.MaxValue;
            }

            const double radioTierraKm = 6371.0;

            double gradosARadianes = Math.PI / 180.0;

            double latitud1 = lat1.Value * gradosARadianes;
            double latitud2 = lat2.Value * gradosARadianes;

            double dLat = (lat2.Value - lat1.Value) * gradosARadianes;
            double dLon = (lon2.Value - lon1.Value) * gradosARadianes;

            double a =
                Math.Pow(Math.Sin(dLat / 2), 2) +
                Math.Cos(latitud1) * Math.Cos(latitud2) *
                Math.Pow(Math.Sin(dLon / 2), 2);

            // Evita pequeñas imprecisiones de coma flotante.
            a = Math.Clamp(a, 0.0, 1.0);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));

            return radioTierraKm * c;
        }

    }
}