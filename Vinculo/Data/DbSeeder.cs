using Microsoft.AspNetCore.Identity;
using Vinculo.Models;
using Vinculo.Models.Enums;

namespace Vinculo.Data
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            // Solicitamos los gestores de Identity al contenedor de dependencias
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<Usuario>>();

            // 1. Crear Roles basados en Enum
            string[] roles = {
                TipoUsuario.Administrador.ToString(),
                TipoUsuario.Empresa.ToString(),
                TipoUsuario.Institucion.ToString()
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2. Crear el Usuario Administrador por defecto
            string adminEmail = "admin@vinculo.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var newAdmin = new Usuario
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    TipoUsuario = TipoUsuario.Administrador,
                    Activo = true,
                    FechaAlta = DateTime.Now,
                    EmailConfirmed = false // confirmación por mail
                };

                // Asignamos una contraseña por defecto
                var result = await userManager.CreateAsync(newAdmin, "Admin123");

                if (result.Succeeded)
                {
                    // Lo vinculamos al rol Administrador
                    await userManager.AddToRoleAsync(newAdmin, TipoUsuario.Administrador.ToString());
                }
            }
        }
    }
}