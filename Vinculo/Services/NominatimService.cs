using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vinculo.Services
{
    public class NominatimService
    {
        private readonly HttpClient _httpClient;

        public NominatimService(HttpClient httpClient)
        {
            _httpClient = httpClient;

            _httpClient.DefaultRequestHeaders.UserAgent.Clear();
            _httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue(
                    "Vinculo",
                    "1.0"));
        }

        // =========================================================
        // GEOCODING
        // Convierte una dirección en Latitud / Longitud
        // =========================================================
        public async Task<(double Latitud, double Longitud)?> BuscarCoordenadasAsync(
            string calle,
            string numero,
            string localidad,
            string provincia,
            string? codigoPostal = null)
        {
            var partes = new List<string>
            {
                $"{calle} {numero}",
                localidad,
                provincia,
                "Argentina"
            };

            if (!string.IsNullOrWhiteSpace(codigoPostal))
            {
                partes.Insert(3, codigoPostal);
            }

            var direccion = string.Join(", ", partes);

            var url =
                $"https://nominatim.openstreetmap.org/search" +
                $"?q={Uri.EscapeDataString(direccion)}" +
                $"&format=json" +
                $"&limit=1" +
                $"&countrycodes=ar";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var contenido =
                await response.Content.ReadAsStringAsync();

            var resultados =
                JsonSerializer.Deserialize<List<NominatimResult>>(
                    contenido);

            var resultado = resultados?.FirstOrDefault();

            if (resultado == null)
            {
                return null;
            }

            if (!double.TryParse(
                    resultado.lat,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var latitud))
            {
                return null;
            }

            if (!double.TryParse(
                    resultado.lon,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var longitud))
            {
                return null;
            }

            return (latitud, longitud);
        }


        // =========================================================
        // REVERSE GEOCODING
        // Convierte Latitud / Longitud en una dirección
        // =========================================================
        public async Task<NominatimReverseResult?> ObtenerDireccionAsync(
            double latitud,
            double longitud)
        {
            var url =
                $"https://nominatim.openstreetmap.org/reverse" +
                $"?lat={latitud.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                $"&lon={longitud.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                $"&format=json" +
                $"&addressdetails=1" +
                $"&accept-language=es";

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    url);

            request.Headers.UserAgent.Clear();

            request.Headers.UserAgent.Add(
                new ProductInfoHeaderValue(
                    "Vinculo",
                    "1.0"));

            var response =
                await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var resultado =
                await response.Content.ReadFromJsonAsync<NominatimReverseResult>();

            return resultado;
        }


        // =========================================================
        // RESULTADO DEL GEOCODING
        // =========================================================
        private class NominatimResult
        {
            public string lat { get; set; } = string.Empty;

            public string lon { get; set; } = string.Empty;
        }
    }


    // =============================================================
    // RESULTADO DEL REVERSE GEOCODING
    // =============================================================
    public class NominatimReverseResult
    {
        [JsonPropertyName("address")]
        public NominatimAddress? Address { get; set; }
    }


    // =============================================================
    // DIRECCIÓN DEVUELTA POR NOMINATIM
    // =============================================================
    public class NominatimAddress
    {
        [JsonPropertyName("road")]
        public string? Road { get; set; }

        [JsonPropertyName("house_number")]
        public string? HouseNumber { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("town")]
        public string? Town { get; set; }

        [JsonPropertyName("village")]
        public string? Village { get; set; }

        [JsonPropertyName("municipality")]
        public string? Municipality { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("postcode")]
        public string? Postcode { get; set; }
    }
}
