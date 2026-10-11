using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Vinculo.Data;
using Vinculo.Models;
using Vinculo.Models.Enums;
using Vinculo.Models.ViewModels;
using Vinculo.Services;

namespace Vinculo.Controllers
{
    [Authorize(Roles = "Empresa")]
    public class DonacionController : Controller
    {
        private readonly VinculoDbContext _context;
        private readonly UserManager<Usuario> _userManager;
        private readonly IImagenStorage _imagenStorage;

        // Inyectamos IImagenStorage en lugar de IWebHostEnvironment
        public DonacionController(
            VinculoDbContext context,
            UserManager<Usuario> userManager,
            IImagenStorage imagenStorage)
        {
            _context = context;
            _userManager = userManager;
            _imagenStorage = imagenStorage;
        }



        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Usuario");

            var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.UsuarioId == user.Id);
            if (empresa == null) return NotFound("Debe completar su perfil antes de ver sus donaciones.");

            // Consulta con "Eager Loading" para traer toda la cadena de datos hacia el Modal
            var donaciones = await _context.Donaciones
                .Include(d => d.Observaciones)
                .Include(d => d.Asignaciones)
                .ThenInclude(a => a.Solicitud)
                    .ThenInclude(s => s.Institucion)
                            .ThenInclude(i => i.Domicilio)

                .Where(d => d.EmpresaId == empresa.Id)
                .OrderByDescending(d => d.Fecha)
                .ToListAsync();

            return View(donaciones);
        }

        [HttpGet]
        public IActionResult RegistrarDonacion()
        {
            return View(new RegistrarDonacionViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarDonacion(RegistrarDonacionViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Usuario");

            var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.UsuarioId == user.Id);
            if (empresa == null) return NotFound("Debe completar su perfil antes de donar.");

            string rutaRelativaBd = null;
            if (model.FotoArchivo != null)
            {
                rutaRelativaBd = await _imagenStorage.GuardarImagenAsync(model.FotoArchivo, "donaciones");
            }

            var nuevaDonacion = new Donacion
            {
                NumeroReferencia = GenerarReferenciaDonacion(),
                EmpresaId = empresa.Id,
                Tipo = model.Tipo.Value,
                Cantidad = model.Cantidad,
                Descripcion = model.Descripcion,
                FotoRuta = rutaRelativaBd,
                Fecha = DateTime.Now,
                Estado = EstadoDonacion.Disponible
            };


            _context.Donaciones.Add(nuevaDonacion);

            // Registrar la creación en el historial.
            var observacion = new Observacion
            {
                Mensaje = $"La empresa creó la donación {nuevaDonacion.NumeroReferencia}.",
                Fecha = DateTime.Now,
                Donacion = nuevaDonacion
            };

            _context.Observaciones.Add(observacion);

            // Guardar la donación y la observación juntas.
            await _context.SaveChangesAsync();

            // Mantener la redirección original al motor de coincidencias.
            return RedirectToAction(nameof(Coincidencias), new { id = nuevaDonacion.Id });

        }



        [HttpGet]
        public async Task<IActionResult> Coincidencias(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Usuario");

            var empresa = await _context.Empresas
                .Include(e => e.Domicilio)
                .FirstOrDefaultAsync(e => e.UsuarioId == user.Id);

            var donacion = await _context.Donaciones
                .FirstOrDefaultAsync(d => d.Id == id && d.EmpresaId == empresa.Id);

            if (donacion == null) return NotFound("Donación no encontrada.");

            // 1. Buscamos las solicitudes candidatas
            var solicitudesCandidatas = await _context.Solicitudes
                .Include(s => s.Institucion)
                    .ThenInclude(i => i.Domicilio)
                .Include(s => s.Equipamientos)
                .Where(s => s.Estado == EstadoSolicitud.Pendiente &&
                            s.Equipamientos.Any(e => e.TipoEquipamiento == donacion.Tipo))
                .ToListAsync();

            // 2. Calculamos distancia y ordenamos (El Core del proyecto)
            var solicitudesConDistancia = solicitudesCandidatas.Select(s => new SolicitudConDistancia
            {
                Solicitud = s,
                DistanciaKm = Utils.GeoUtils.CalcularDistanciaKm(
                    empresa.Domicilio?.Latitud, empresa.Domicilio?.Longitud,
                    s.Institucion.Domicilio?.Latitud, s.Institucion.Domicilio?.Longitud
                )
            })
            .OrderBy(x => x.DistanciaKm) // Se ordenan de menor a mayor distancia
            .ToList();

            var model = new CoincidenciasViewModel
            {
                Donacion = donacion,
                Empresa = empresa,
                SolicitudesCompatibles = solicitudesConDistancia
            };

            return View(model);
        }

        // Metodo Asignar donacion

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Asignar(int donacionId, int solicitudId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "Usuario");

            var empresa = await _context.Empresas
                .FirstOrDefaultAsync(e => e.UsuarioId == user.Id);

            if (empresa == null)
                return NotFound("Debe completar su perfil antes de asignar una donación.");

            var donacion = await _context.Donaciones
                .FirstOrDefaultAsync(d =>
                    d.Id == donacionId &&
                    d.EmpresaId == empresa.Id);

            if (donacion == null)
                return NotFound("Donación no encontrada.");

            if (donacion.Estado != EstadoDonacion.Disponible ||
                donacion.Cantidad <= 0)
            {
                TempData["Error"] = "La donación ya no está disponible para asignación.";
                return RedirectToAction(nameof(Index));
            }

            var solicitud = await _context.Solicitudes
                .Include(s => s.Equipamientos)
                .FirstOrDefaultAsync(s => s.Id == solicitudId);

            if (solicitud == null)
                return NotFound("Solicitud no encontrada.");

            if (solicitud.Estado != EstadoSolicitud.Pendiente)
            {
                TempData["Error"] = "La solicitud ya no está pendiente de asignación.";
                return RedirectToAction(nameof(Coincidencias), new { id = donacion.Id });
            }

            var detalleCompatible = solicitud.Equipamientos
                .FirstOrDefault(e =>
                    e.TipoEquipamiento == donacion.Tipo &&
                    e.Cantidad > 0);

            if (detalleCompatible == null)
            {
                TempData["Error"] =
                    "La solicitud no requiere unidades pendientes de este equipamiento.";

                return RedirectToAction(nameof(Coincidencias), new { id = donacion.Id });
            }

            var fecha = DateTime.Now;

            var cantidadOriginalDonacion = donacion.Cantidad;
            var cantidadOriginalSolicitada = detalleCompatible.Cantidad;

            var cantidadAsignada = Math.Min(
                cantidadOriginalDonacion,
                cantidadOriginalSolicitada);

            var cantidadSobranteDonacion =
                cantidadOriginalDonacion - cantidadAsignada;

            // Guardamos los faltantes de TODOS los equipamientos
            // antes de modificar la solicitud original.
            var detallesPendientes = solicitud.Equipamientos
                .Select(e => new
                {
                    Tipo = e.TipoEquipamiento,
                    Cantidad = e.Id == detalleCompatible.Id
                        ? e.Cantidad - cantidadAsignada
                        : e.Cantidad
                })
                .Where(e => e.Cantidad > 0)
                .ToList();

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            // 1. La donación original representa solamente
            // las unidades que se asignaron.
            donacion.Cantidad = cantidadAsignada;
            donacion.Estado = EstadoDonacion.Asignada;

            // 2. La solicitud original representa solamente
            // las unidades asignadas.
            var detallesOriginales = solicitud.Equipamientos.ToList();

            foreach (var detalle in detallesOriginales)
            {
                if (detalle.Id != detalleCompatible.Id)
                {
                    _context.Remove(detalle);
                    solicitud.Equipamientos.Remove(detalle);
                }
            }

            detalleCompatible.Cantidad = cantidadAsignada;

            // 3. La solicitud original pasa a coordinación.
            solicitud.Estado = EstadoSolicitud.EnCoordinacion;

            // 4. Registramos la asignación.
            var asignacion = new Asignacion
            {
                NumeroReferencia = GenerarReferenciaAsignacion(),
                SolicitudId = solicitud.Id,
                DonacionId = donacion.Id,
                Cantidad = cantidadAsignada,
                Fecha = fecha,
                Estado = EstadoAsignacion.Activa
            };

            _context.Asignaciones.Add(asignacion);

            // 5. Observación de la solicitud original.

            // Función local para mostrar correctamente el nombre del equipamiento.
            string NombreEquipamiento(TipoEquipamiento tipo, int cantidad)
            {
                return tipo switch
                {
                    TipoEquipamiento.Notebook =>
                        cantidad == 1 ? "notebook" : "notebooks",

                    TipoEquipamiento.Monitor =>
                        cantidad == 1 ? "monitor" : "monitores",

                    TipoEquipamiento.Estabilizador =>
                        cantidad == 1 ? "estabilizador" : "estabilizadores",

                    TipoEquipamiento.AllInOne =>
                        cantidad == 1 ? "equipo All-in-One" : "equipos All-in-One",

                    TipoEquipamiento.MiniPC =>
                        cantidad == 1 ? "mini PC" : "mini PC",

                    TipoEquipamiento.Desktop =>
                        cantidad == 1 ? "equipo de escritorio" : "equipos de escritorio",

                    _ => cantidad == 1
                        ? tipo.ToString()
                        : tipo.ToString() + "s"
                };
            }

            string DescribirEquipamientos(
                IEnumerable<(TipoEquipamiento Tipo, int Cantidad)> elementos)
            {
                return string.Join(", ", elementos.Select(e =>
                    $"{e.Cantidad} {NombreEquipamiento(e.Tipo, e.Cantidad)}"));
            }

            // 5. Crear la nueva solicitud con TODOS los pendientes.
            Solicitud? nuevaSolicitud = null;

            if (detallesPendientes.Any())
            {
                nuevaSolicitud = new Solicitud
                {
                    NumeroReferencia =
                        $"SOL-{fecha:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
                    InstitucionId = solicitud.InstitucionId,
                    Fecha = fecha,
                    Estado = EstadoSolicitud.Pendiente,
                    Equipamientos = detallesPendientes
                        .Select(e => new DetalleSolicitud
                        {
                            TipoEquipamiento = e.Tipo,
                            Cantidad = e.Cantidad
                        })
                        .ToList()
                };

                _context.Solicitudes.Add(nuevaSolicitud);
            }

            // 6. Crear la nueva donación si quedaron unidades sin asignar.
            Donacion? donacionSobrante = null;

            if (cantidadSobranteDonacion > 0)
            {
                donacionSobrante = new Donacion
                {
                    NumeroReferencia = GenerarReferenciaDonacion(),
                    EmpresaId = donacion.EmpresaId,
                    Tipo = donacion.Tipo,
                    Cantidad = cantidadSobranteDonacion,
                    Descripcion = donacion.Descripcion,
                    FotoRuta = donacion.FotoRuta,
                    Fecha = fecha,
                    Estado = EstadoDonacion.Disponible
                };

                _context.Donaciones.Add(donacionSobrante);
            }

            // 7. Observación exclusiva de la solicitud original.
            var mensajeSolicitudOriginal =
                $"Se asignó {cantidadAsignada} " +
                $"{NombreEquipamiento(donacion.Tipo, cantidadAsignada)} " +
                $"mediante la asignación {asignacion.NumeroReferencia}, " +
                $"utilizando la donación {donacion.NumeroReferencia}.";

            if (nuevaSolicitud != null)
            {
                var pendientes = detallesPendientes
                    .Select(e => (
                        Tipo: e.Tipo,
                        Cantidad: e.Cantidad));

                mensajeSolicitudOriginal +=
                    $" Se creó la nueva solicitud {nuevaSolicitud.NumeroReferencia} " +
                    $"con los siguientes pendientes: {DescribirEquipamientos(pendientes)}.";
            }
            else
            {
                mensajeSolicitudOriginal +=
                    " No quedaron equipamientos pendientes en esta solicitud.";
            }

            _context.Observaciones.Add(new Observacion
            {
                Mensaje = mensajeSolicitudOriginal,
                Fecha = fecha,
                Solicitud = solicitud
            });

            // 8. Observación exclusiva de la nueva solicitud.
            if (nuevaSolicitud != null)
            {
                var pendientes = detallesPendientes
                    .Select(e => (
                        Tipo: e.Tipo,
                        Cantidad: e.Cantidad));

                _context.Observaciones.Add(new Observacion
                {
                    Mensaje =
                        $"Solicitud creada automáticamente a partir de " +
                        $"{solicitud.NumeroReferencia} debido a una asignación parcial. " +
                        $"Se trasladaron los siguientes equipamientos pendientes: " +
                        $"{DescribirEquipamientos(pendientes)}.",
                    Fecha = fecha,
                    Solicitud = nuevaSolicitud
                });
            }

            // 9. Observación exclusiva de la donación original.
            var mensajeDonacionOriginal =
                $"Se asignó {cantidadAsignada} " +
                $"{NombreEquipamiento(donacion.Tipo, cantidadAsignada)} " +
                $"a la solicitud {solicitud.NumeroReferencia}. " +
                $"Asignación: {asignacion.NumeroReferencia}.";

            if (donacionSobrante != null)
            {
                mensajeDonacionOriginal +=
                    $"Quedó sin asignar {cantidadSobranteDonacion} " +
                    $"{NombreEquipamiento(donacion.Tipo, cantidadSobranteDonacion)} " +
                    $"sin asignar, se creó la donación disponible " +
                    $"{donacionSobrante.NumeroReferencia}.";
            }
            else
            {
                mensajeDonacionOriginal +=
                    " Se utilizó la totalidad de la donación; no quedaron unidades sobrantes.";
            }

            _context.Observaciones.Add(new Observacion
            {
                Mensaje = mensajeDonacionOriginal,
                Fecha = fecha,
                Donacion = donacion
            });

            // 10. Observación exclusiva de la nueva donación.
            if (donacionSobrante != null)
            {
                _context.Observaciones.Add(new Observacion
                {
                    Mensaje =
                        $"Donación creada automáticamente a partir de " +
                        $"{donacion.NumeroReferencia} por el sobrante de una asignación. " +
                        $"Quedó disponible {cantidadSobranteDonacion} " +
                        $"{NombreEquipamiento(donacion.Tipo, cantidadSobranteDonacion)}. " +
                        $"Asignación relacionada: {asignacion.NumeroReferencia}. " +
                        $"Solicitud atendida parcialmente: {solicitud.NumeroReferencia}.",
                    Fecha = fecha,
                    Donacion = donacionSobrante
                });
            }

            // 11. Observación exclusiva de la asignación.
            _context.Observaciones.Add(new Observacion
            {
                Mensaje =
                    $"Asignación registrada: {cantidadAsignada} " +
                    $"{NombreEquipamiento(donacion.Tipo, cantidadAsignada)}. " +
                    $"Donación: {donacion.NumeroReferencia}. " +
                    $"Solicitud atendida: {solicitud.NumeroReferencia}." +
                    (nuevaSolicitud != null
                        ? $" Solicitud de pendientes creada: {nuevaSolicitud.NumeroReferencia}."
                        : "") +
                    (donacionSobrante != null
                        ? $" Donación con sobrante creada: {donacionSobrante.NumeroReferencia}."
                        : ""),
                Fecha = fecha,
                Asignacion = asignacion
            });

            // 12. Guardar todos los cambios en la misma transacción.
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] =
                 $"Se asignó {cantidadAsignada} " +
                 $"{NombreEquipamiento(donacion.Tipo, cantidadAsignada)}. " +
                 (nuevaSolicitud != null
                     ? $"Se creó la solicitud pendiente {nuevaSolicitud.NumeroReferencia}. "
                     : "") +
                 (donacionSobrante != null
                     ? $"Se creó la donación disponible {donacionSobrante.NumeroReferencia} " +
                       $"con {cantidadSobranteDonacion} " +
                       $"{NombreEquipamiento(donacion.Tipo, cantidadSobranteDonacion)}."
                     : "");

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarEntrega(int asignacionId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "Usuario");

            var empresa = await _context.Empresas
                .FirstOrDefaultAsync(e => e.UsuarioId == user.Id);

            if (empresa == null)
                return NotFound("Debe completar su perfil antes de confirmar una entrega.");

            var asignacion = await _context.Asignaciones
                .Include(a => a.Donacion)
                .Include(a => a.Solicitud)
                .FirstOrDefaultAsync(a => a.Id == asignacionId);

            if (asignacion == null)
                return NotFound("Asignación no encontrada.");

            // Verificar que la donación pertenezca a la empresa autenticada.
            if (asignacion.Donacion.EmpresaId != empresa.Id)
                return NotFound("No se encontró la asignación solicitada.");

            // Evitar confirmar dos veces una misma entrega.
            if (asignacion.Estado != EstadoAsignacion.Activa ||
                asignacion.Donacion.Estado != EstadoDonacion.Asignada)
            {
                TempData["Error"] =
                    "La entrega no se puede confirmar porque la asignación ya fue procesada.";

                return RedirectToAction(nameof(Index));
            }

            var fecha = DateTime.Now;

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            // Actualizar la donación asignada y su asignación.
            asignacion.Donacion.Estado = EstadoDonacion.Entregada;
            asignacion.Estado = EstadoAsignacion.Concretada;

            // Completar la solicitud solamente si no quedan
            // otras asignaciones activas asociadas.
            var quedanAsignacionesActivas = await _context.Asignaciones
                .AnyAsync(a =>
                    a.SolicitudId == asignacion.SolicitudId &&
                    a.Id != asignacion.Id &&
                    a.Estado == EstadoAsignacion.Activa);

            if (!quedanAsignacionesActivas)
            {
                asignacion.Solicitud.Estado = EstadoSolicitud.Concretado;
            }

            // Registrar el evento en el historial de la donación.
            _context.Observaciones.Add(new Observacion
            {
                Mensaje =
                    $"La empresa confirmó la entrega de la donación " +
                    $"{asignacion.Donacion.NumeroReferencia}. " +
                    $"Asignación: {asignacion.NumeroReferencia}.",
                Fecha = fecha,
                Donacion = asignacion.Donacion
            });

            // Registrar el evento en el historial de la asignación.
            _context.Observaciones.Add(new Observacion
            {
                Mensaje =
                    $"La empresa confirmó la entrega de la asignación " +
                    $"{asignacion.NumeroReferencia}.",
                Fecha = fecha,
                Asignacion = asignacion
            });

            // Registrar el evento en el historial de la solicitud.
            _context.Observaciones.Add(new Observacion
            {
                Mensaje =
                    $"Se confirmó la entrega correspondiente a la asignación " +
                    $"{asignacion.NumeroReferencia}, de la donación " +
                    $"{asignacion.Donacion.NumeroReferencia}.",
                Fecha = fecha,
                Solicitud = asignacion.Solicitud
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] = "La entrega se confirmó correctamente.";

            return RedirectToAction(nameof(Index));
        }



        //REFERENCIAS
        private string GenerarReferenciaDonacion()
        {
            return $"DON-{DateTime.Now:yyyyMMddHHmmss}";
        }

        private string GenerarReferenciaAsignacion()
        {
            return $"ASIG-{DateTime.Now:yyyyMMddHHmmss}";
        }

    }
}