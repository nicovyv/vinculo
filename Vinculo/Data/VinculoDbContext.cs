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
        public DbSet<Domicilio> Domicilios { get; set; }
        public DbSet<Asignacion> Asignaciones { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Asignacion>()
                .HasOne(a => a.Solicitud)
                .WithMany()
                .HasForeignKey(a => a.SolicitudId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Asignacion>()
                .HasOne(a => a.Donacion)
                .WithMany()
                .HasForeignKey(a => a.DonacionId)
                .OnDelete(DeleteBehavior.NoAction);
        }


    }
}
