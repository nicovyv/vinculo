using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Vinculo.Models;

namespace Vinculo.Data
{
    public class VinculoDbContext :IdentityDbContext<Usuario>
    {
        public VinculoDbContext(DbContextOptions<VinculoDbContext> options)
            : base(options)
        {
        }

        public DbSet<Empresa> Empresas { get; set; }
        public DbSet<Institucion> Instituciones { get; set; }
        public DbSet<Solicitud> Solicitudes { get; set; }
        public DbSet<Donacion> Donaciones { get; set; }




    }
}
