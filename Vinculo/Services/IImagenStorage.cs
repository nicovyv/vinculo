using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Vinculo.Services
{
    public interface IImagenStorage
    {
        // Recibe el archivo y el nombre de la carpeta y devuelve la ruta relativa
        Task<string> GuardarImagenAsync(IFormFile archivo, string subcarpeta);
    }
}