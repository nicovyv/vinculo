using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
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

            var donaciones = await _context.Donaciones
                .Include(d => d.Equipamientos)
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
            model.Detalles ??= new List<DetalleDonacionViewModel>();

            // Filtrar cantidades válidas y unificar tipos de equipamiento repetidos
            model.Detalles = model.Detalles
                .Where(d => d.Cantidad > 0)
                .GroupBy(d => d.TipoEquipamiento)
                .Select(g => new DetalleDonacionViewModel
                {
                    TipoEquipamiento = g.Key,
                    Cantidad = g.Sum(d => d.Cantidad)
                })
                .ToList();

            if (!model.Detalles.Any())
            {
                ModelState.AddModelError(
                    nameof(model.Detalles),
                    "Debe agregar al menos un equipamiento.");
            }

            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Usuario");

            var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.UsuarioId == user.Id);
            if (empresa == null) return NotFound("Debe completar su perfil antes de donar.");

            string? rutaRelativaBd = null;
            if (model.FotoArchivo != null)
            {
                rutaRelativaBd = await _imagenStorage.GuardarImagenAsync(model.FotoArchivo, "donaciones");
            }

            var fecha = DateTime.Now;

            // Se crea 1 sola donación (1 lote) con todos los equipamientos cargados
            var nuevaDonacion = new Donacion
            {
                NumeroReferencia = GenerarReferenciaDonacion(),
                EmpresaId = empresa.Id,
                Descripcion = model.Descripcion,
                FotoRuta = rutaRelativaBd,
                Fecha = fecha,
                Estado = EstadoDonacion.Disponible,
                Equipamientos = model.Detalles
                    .Select(d => new DetalleDonacion
                    {
                        TipoEquipamiento = d.TipoEquipamiento,
                        Cantidad = d.Cantidad
                    })
                    .ToList()
            };

            _context.Donaciones.Add(nuevaDonacion);

            var observacion = new Observacion
            {
                Mensaje = $"La empresa creó la donación {nuevaDonacion.NumeroReferencia}.",
                Fecha = fecha,
                Donacion = nuevaDonacion
            };

            _context.Observaciones.Add(observacion);

            await _context.SaveChangesAsync();

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

            if (empresa == null) return NotFound("Empresa no encontrada.");

            var donacion = await _context.Donaciones
                .Include(d => d.Equipamientos)
                .FirstOrDefaultAsync(d => d.Id == id && d.EmpresaId == empresa.Id);

            if (donacion == null) return NotFound("Donación no encontrada.");

            var tiposDonados = donacion.Equipamientos
                .Where(e => e.Cantidad > 0)
                .Select(e => e.TipoEquipamiento)
                .Distinct()
                .ToList();

            // 1. Buscamos las solicitudes candidatas que requieran al menos uno de los tipos del lote
            var solicitudesCandidatas = await _context.Solicitudes
                .Include(s => s.Institucion)
                    .ThenInclude(i => i.Domicilio)
                .Include(s => s.Equipamientos)
                .Where(s => s.Estado == EstadoSolicitud.Pendiente &&
                            s.Equipamientos.Any(e => e.Cantidad > 0 && tiposDonados.Contains(e.TipoEquipamiento)))
                .ToListAsync();

            // 2. Calculamos distancia y ordenamos
            var solicitudesConDistancia = solicitudesCandidatas.Select(s => new SolicitudConDistancia
            {
                Solicitud = s,
                DistanciaKm = Utils.GeoUtils.CalcularDistanciaKm(
                    empresa.Domicilio?.Latitud, empresa.Domicilio?.Longitud,
                    s.Institucion.Domicilio?.Latitud, s.Institucion.Domicilio?.Longitud
                )
            })
            .OrderBy(x => x.DistanciaKm)
            .ToList();

            var model = new CoincidenciasViewModel
            {
                Donacion = donacion,
                Empresa = empresa,
                SolicitudesCompatibles = solicitudesConDistancia
            };

            return View(model);
        }

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
                .Include(d => d.Equipamientos)
                .FirstOrDefaultAsync(d =>
                    d.Id == donacionId &&
                    d.EmpresaId == empresa.Id);

            if (donacion == null)
                return NotFound("Donación no encontrada.");

            if (donacion.Estado != EstadoDonacion.Disponible ||
                !donacion.Equipamientos.Any(e => e.Cantidad > 0))
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

            // Calculamos las coincidencias entre todos los equipamientos del lote donado y los solicitados
            var asignacionesPorTipo = new Dictionary<TipoEquipamiento, int>();

            foreach (var detDon in donacion.Equipamientos.Where(e => e.Cantidad > 0))
            {
                var detSol = solicitud.Equipamientos
                    .FirstOrDefault(s => s.TipoEquipamiento == detDon.TipoEquipamiento && s.Cantidad > 0);

                if (detSol != null)
                {
                    var cantAsignar = Math.Min(detDon.Cantidad, detSol.Cantidad);
                    if (cantAsignar > 0)
                    {
                        asignacionesPorTipo[detDon.TipoEquipamiento] = cantAsignar;
                    }
                }
            }

            if (!asignacionesPorTipo.Any())
            {
                TempData["Error"] = "La solicitud no requiere unidades compatibles con este lote de donación.";
                return RedirectToAction(nameof(Coincidencias), new { id = donacion.Id });
            }

            var fecha = DateTime.Now;
            var cantidadTotalAsignada = asignacionesPorTipo.Values.Sum();

            // Guardamos los sobrantes de TODOS los equipamientos de la donación antes de modificarla
            var detallesSobrantesDonacion = donacion.Equipamientos
                .Select(e => new
                {
                    Tipo = e.TipoEquipamiento,
                    Cantidad = asignacionesPorTipo.TryGetValue(e.TipoEquipamiento, out var asig)
                        ? e.Cantidad - asig
                        : e.Cantidad
                })
                .Where(e => e.Cantidad > 0)
                .ToList();

            // Guardamos los faltantes de TODOS los equipamientos de la solicitud antes de modificarla
            var detallesPendientesSolicitud = solicitud.Equipamientos
                .Select(e => new
                {
                    Tipo = e.TipoEquipamiento,
                    Cantidad = asignacionesPorTipo.TryGetValue(e.TipoEquipamiento, out var asig)
                        ? e.Cantidad - asig
                        : e.Cantidad
                })
                .Where(e => e.Cantidad > 0)
                .ToList();

            await using var transaction = await _context.Database.BeginTransactionAsync();

            // 1. La donación original representa solamente las unidades que se asignaron
            var detallesDonacionOriginales = donacion.Equipamientos.ToList();
            foreach (var det in detallesDonacionOriginales)
            {
                if (asignacionesPorTipo.TryGetValue(det.TipoEquipamiento, out var cantAsig))
                {
                    det.Cantidad = cantAsig;
                }
                else
                {
                    _context.Remove(det);
                    donacion.Equipamientos.Remove(det);
                }
            }
            donacion.Estado = EstadoDonacion.Asignada;

            // 2. La solicitud original representa solamente las unidades asignadas
            var detallesSolicitudOriginales = solicitud.Equipamientos.ToList();
            foreach (var det in detallesSolicitudOriginales)
            {
                if (asignacionesPorTipo.TryGetValue(det.TipoEquipamiento, out var cantAsig))
                {
                    det.Cantidad = cantAsig;
                }
                else
                {
                    _context.Remove(det);
                    solicitud.Equipamientos.Remove(det);
                }
            }

            // 3. La solicitud original pasa a coordinación
            solicitud.Estado = EstadoSolicitud.EnCoordinacion;

            // 4. Registramos la asignación
            var asignacion = new Asignacion
            {
                NumeroReferencia = GenerarReferenciaAsignacion(),
                SolicitudId = solicitud.Id,
                DonacionId = donacion.Id,
                Cantidad = cantidadTotalAsignada,
                Fecha = fecha,
                Estado = EstadoAsignacion.Activa
            };

            _context.Asignaciones.Add(asignacion);

            // Funciones locales para formatear descripciones
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

            string DescribirEquipamientos(IEnumerable<(TipoEquipamiento Tipo, int Cantidad)> elementos)
            {
                return string.Join(", ", elementos.Select(e =>
                    $"{e.Cantidad} {NombreEquipamiento(e.Tipo, e.Cantidad)}"));
            }

            var textoAsignados = DescribirEquipamientos(
                asignacionesPorTipo.Select(kvp => (kvp.Key, kvp.Value)));

            // 5. Crear la nueva solicitud con TODOS los pendientes
            Solicitud? nuevaSolicitud = null;

            if (detallesPendientesSolicitud.Any())
            {
                nuevaSolicitud = new Solicitud
                {
                    NumeroReferencia =
                        $"SOL-{fecha:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
                    InstitucionId = solicitud.InstitucionId,
                    Fecha = fecha,
                    Estado = EstadoSolicitud.Pendiente,
                    Equipamientos = detallesPendientesSolicitud
                        .Select(e => new DetalleSolicitud
                        {
                            TipoEquipamiento = e.Tipo,
                            Cantidad = e.Cantidad
                        })
                        .ToList()
                };

                _context.Solicitudes.Add(nuevaSolicitud);
            }

            // 6. Crear la nueva donación si quedaron unidades o ítems sin asignar en el lote
            Donacion? donacionSobrante = null;

            if (detallesSobrantesDonacion.Any())
            {
                donacionSobrante = new Donacion
                {
                    NumeroReferencia = GenerarReferenciaDonacion(),
                    EmpresaId = donacion.EmpresaId,
                    Descripcion = donacion.Descripcion,
                    FotoRuta = donacion.FotoRuta,
                    Fecha = fecha,
                    Estado = EstadoDonacion.Disponible,
                    Equipamientos = detallesSobrantesDonacion
                        .Select(e => new DetalleDonacion
                        {
                            TipoEquipamiento = e.Tipo,
                            Cantidad = e.Cantidad
                        })
                        .ToList()
                };

                _context.Donaciones.Add(donacionSobrante);
            }

            // 7. Observación exclusiva de la solicitud original
            var mensajeSolicitudOriginal =
                $"Se asignó {textoAsignados} mediante la asignación {asignacion.NumeroReferencia}, " +
                $"utilizando la donación {donacion.NumeroReferencia}.";

            if (nuevaSolicitud != null)
            {
                var pendientes = detallesPendientesSolicitud
                    .Select(e => (Tipo: e.Tipo, Cantidad: e.Cantidad));

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

            // 8. Observación exclusiva de la nueva solicitud
            if (nuevaSolicitud != null)
            {
                var pendientes = detallesPendientesSolicitud
                    .Select(e => (Tipo: e.Tipo, Cantidad: e.Cantidad));

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

            // 9. Observación exclusiva de la donación original
            var mensajeDonacionOriginal =
                $"Se asignó {textoAsignados} a la solicitud {solicitud.NumeroReferencia}. " +
                $"Asignación: {asignacion.NumeroReferencia}.";

            if (donacionSobrante != null)
            {
                var sobrantes = detallesSobrantesDonacion
                    .Select(e => (Tipo: e.Tipo, Cantidad: e.Cantidad));

                mensajeDonacionOriginal +=
                    $" Quedó sin asignar ({DescribirEquipamientos(sobrantes)}), " +
                    $"se creó la donación disponible {donacionSobrante.NumeroReferencia}.";
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

            // 10. Observación exclusiva de la nueva donación
            if (donacionSobrante != null)
            {
                var sobrantes = detallesSobrantesDonacion
                    .Select(e => (Tipo: e.Tipo, Cantidad: e.Cantidad));

                _context.Observaciones.Add(new Observacion
                {
                    Mensaje =
                        $"Donación creada automáticamente a partir de " +
                        $"{donacion.NumeroReferencia} por el sobrante de una asignación. " +
                        $"Quedó disponible: {DescribirEquipamientos(sobrantes)}. " +
                        $"Asignación relacionada: {asignacion.NumeroReferencia}. " +
                        $"Solicitud atendida: {solicitud.NumeroReferencia}.",
                    Fecha = fecha,
                    Donacion = donacionSobrante
                });
            }

            // 11. Observación exclusiva de la asignación
            _context.Observaciones.Add(new Observacion
            {
                Mensaje =
                    $"Asignación registrada: {textoAsignados}. " +
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

            // 12. Guardar todos los cambios en la misma transacción
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] =
                $"Se asignó {textoAsignados}. " +
                (nuevaSolicitud != null
                    ? $"Se creó la solicitud pendiente {nuevaSolicitud.NumeroReferencia}. "
                    : "") +
                (donacionSobrante != null
                    ? $"Se creó la donación disponible {donacionSobrante.NumeroReferencia} con el sobrante."
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

            if (asignacion.Donacion.EmpresaId != empresa.Id)
                return NotFound("No se encontró la asignación solicitada.");

            if (asignacion.Estado != EstadoAsignacion.Activa ||
                asignacion.Donacion.Estado != EstadoDonacion.Asignada)
            {
                TempData["Error"] =
                    "La entrega no se puede confirmar porque la asignación ya fue procesada.";

                return RedirectToAction(nameof(Index));
            }

            var fecha = DateTime.Now;

            await using var transaction = await _context.Database.BeginTransactionAsync();

            asignacion.Donacion.Estado = EstadoDonacion.Entregada;
            asignacion.Estado = EstadoAsignacion.Concretada;

            var quedanAsignacionesActivas = await _context.Asignaciones
                .AnyAsync(a =>
                    a.SolicitudId == asignacion.SolicitudId &&
                    a.Id != asignacion.Id &&
                    a.Estado == EstadoAsignacion.Activa);

            if (!quedanAsignacionesActivas)
            {
                asignacion.Solicitud.Estado = EstadoSolicitud.Concretado;
            }

            _context.Observaciones.Add(new Observacion
            {
                Mensaje =
                    $"La empresa confirmó la entrega de la donación " +
                    $"{asignacion.Donacion.NumeroReferencia}. " +
                    $"Asignación: {asignacion.NumeroReferencia}.",
                Fecha = fecha,
                Donacion = asignacion.Donacion
            });

            _context.Observaciones.Add(new Observacion
            {
                Mensaje =
                    $"La empresa confirmó la entrega de la asignación " +
                    $"{asignacion.NumeroReferencia}.",
                Fecha = fecha,
                Asignacion = asignacion
            });

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

        // REFERENCIAS
        private string GenerarReferenciaDonacion()
        {
            return $"DON-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}";
        }

        private string GenerarReferenciaAsignacion()
        {
            return $"ASIG-{DateTime.Now:yyyyMMddHHmmss}";
        }
    }
}