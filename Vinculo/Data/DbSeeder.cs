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


            // 3. Crear Usuario Empresa de Prueba
            string empresaEmail = "empresa@vinculo.com";
            var empresaUser = await userManager.FindByEmailAsync(empresaEmail);

            if (empresaUser == null)
            {
                var newEmpresa = new Usuario
                {
                    UserName = empresaEmail,
                    Email = empresaEmail,
                    TipoUsuario = TipoUsuario.Empresa,
                    Activo = true,
                    FechaAlta = DateTime.Now,
                    EmailConfirmed = false // confirmación por mail
                };

                // Asignamos una contraseña por defecto
                var result = await userManager.CreateAsync(newEmpresa, "Empresa123");

                if (result.Succeeded)
                {
                    // Lo vinculamos al rol Empresa
                    await userManager.AddToRoleAsync(newEmpresa, TipoUsuario.Empresa.ToString());
                }
            }

            // 4. Crear Usuario Institución de Prueba
            string institucionEmail = "institucion@vinculo.com";
            var institucionUser = await userManager.FindByEmailAsync(institucionEmail);

            if (institucionUser == null)
            {
                var newInstitucion = new Usuario
                {
                    UserName = institucionEmail,
                    Email = institucionEmail,
                    TipoUsuario = TipoUsuario.Institucion,
                    Activo = true,
                    FechaAlta = DateTime.Now,
                    EmailConfirmed = false // confirmación por mail
                };

                // Asignamos una contraseña por defecto
                var result = await userManager.CreateAsync(newInstitucion, "Institucion123");

                if (result.Succeeded)
                {
                    // Lo vinculamos al rol Institución
                    await userManager.AddToRoleAsync(newInstitucion, TipoUsuario.Institucion.ToString());
                }
            }


        }
    }
}