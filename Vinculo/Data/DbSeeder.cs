using Microsoft.AspNetCore.Identity;
using Vinculo.Models;
using Vinculo.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Vinculo.Data
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            // Solicitamos los gestores de Identity al contenedor de dependencias
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<Usuario>>();
            var context = serviceProvider.GetRequiredService<VinculoDbContext>();

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
                empresaUser = newEmpresa;
            }

            // CREAR DATOS DE LA EMPRESA

            var empresa = await context.Empresas.FirstOrDefaultAsync(e => e.UsuarioId == empresaUser.Id);

            if (empresa == null)
            {
                var domicilioEmpresa = new Domicilio
                {
                    Calle = "Av. Corrientes",
                    Numero = "1234",
                    Localidad = "Buenos Aires",
                    Provincia = "Buenos Aires",
                    CodigoPostal = "1043"
                };

                context.Domicilios.Add(domicilioEmpresa);

                empresa = new Empresa
                {
                    RazonSocial = "Empresa de Prueba S.A.",
                    Cuit = "30-12345678-9",
                    Telefono = "11-4567-8901",
                    Email = empresaEmail,
                    UsuarioId = empresaUser.Id,
                    Domicilio = domicilioEmpresa
                };

                context.Empresas.Add(empresa);

                await context.SaveChangesAsync();
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
                institucionUser = newInstitucion;

                
            }
            // CREAR DATOS DE LA INSTITUCIÓN


            var institucion = await context.Instituciones
                .FirstOrDefaultAsync(i =>
                    i.UsuarioId == institucionUser.Id);

            if (institucion == null)
            {
                var domicilioInstitucion = new Domicilio
                {
                    Calle = "Av. Santa Fe",
                    Numero = "2500",
                    Localidad = "Buenos Aires",
                    Provincia = "Buenos Aires",
                    CodigoPostal = "1425"
                };

                context.Domicilios.Add(domicilioInstitucion);

                institucion = new Institucion
                {
                    Nombre = "Institución de Prueba",
                    Cuit = "30-98765432-1",
                    Telefono = "11-9876-5432",
                    Email = institucionEmail,
                    UsuarioId = institucionUser.Id,
                    Domicilio = domicilioInstitucion
                };

                context.Instituciones.Add(institucion);

                await context.SaveChangesAsync();
            }
        }
    }
}