using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Vinculo.Models;

namespace Vinculo.Data
{
    public class VinculoDbContext : IdentityDbContext<Usuario>
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
        public DbSet<Observacion> Observaciones { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);


            builder.Entity<Asignacion>()
                .HasOne(a => a.Donacion)
                .WithMany(d => d.Asignaciones)
                .HasForeignKey(a => a.DonacionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Asignacion>()
                .HasOne(a => a.Solicitud)
                .WithMany()
                .HasForeignKey(a => a.SolicitudId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Observacion>()
                .HasOne(o => o.Donacion)
                .WithMany(d => d.Observaciones)
                .HasForeignKey(o => o.DonacionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Observacion>()
                .HasOne(o => o.Solicitud)
                .WithMany(s => s.Observaciones)
                .HasForeignKey(o => o.SolicitudId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Observacion>()
                .HasOne(o => o.Asignacion)
                .WithMany(a => a.Observaciones)
                .HasForeignKey(o => o.AsignacionId)
                .OnDelete(DeleteBehavior.Restrict);
        }


    }
}
