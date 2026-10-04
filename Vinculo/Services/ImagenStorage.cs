using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Vinculo.Services
{
    public class ImagenStorage : IImagenStorage
    {
        private readonly IWebHostEnvironment _env;

        public ImagenStorage(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> GuardarImagenAsync(IFormFile archivo, string subcarpeta)
        {
            if (archivo == null || archivo.Length == 0) return null;

            // Define la ruta física absoluta
            string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", subcarpeta);

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Genera el nombre único
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(archivo.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // Guarda el archivo
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await archivo.CopyToAsync(fileStream);
            }

            // Retorna la ruta relativa para la base de datos
            return $"/uploads/{subcarpeta}/{uniqueFileName}";
        }
    }
}