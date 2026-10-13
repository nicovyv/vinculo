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

        // =========================================================
        // INDEX
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var empresa = await ObtenerEmpresaActual();
            if (empresa == null)
                return NotFound("Debe completar su perfil antes de ver sus donaciones.");

            var donaciones = await _context.Donaciones
                .AsNoTracking()
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

        // =========================================================
        // REGISTRAR DONACIÓN - GET & POST
        // =========================================================

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

            var empresa = await ObtenerEmpresaActual();
            if (empresa == null)
                return NotFound("Debe completar su perfil antes de donar.");

            string? rutaRelativaBd = null;
            if (model.FotoArchivo != null)
            {
                rutaRelativaBd = await _imagenStorage.GuardarImagenAsync(model.FotoArchivo, "donaciones");
            }

            var fecha = DateTime.Now;

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

            _context.Observaciones.Add(new Observacion
            {
                Mensaje = $"La empresa creó la donación {nuevaDonacion.NumeroReferencia}.",
                Fecha = fecha,
                Donacion = nuevaDonacion
            });

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Coincidencias), new { id = nuevaDonacion.Id });
        }

        // =========================================================
        // EDIT - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var empresa = await ObtenerEmpresaActual();
            if (empresa == null) return NotFound();

            var donacion = await _context.Donaciones
                .AsNoTracking()
                .Include(d => d.Equipamientos)
                .FirstOrDefaultAsync(d =>
                    d.Id == id &&
                    d.EmpresaId == empresa.Id);

            if (donacion == null) return NotFound();

            if (donacion.Estado != EstadoDonacion.Disponible)
            {
                TempData["Error"] =
                    "La donación no puede modificarse porque ya no se encuentra disponible.";

                return RedirectToAction(nameof(Index));
            }

            var model = new RegistrarDonacionViewModel
            {
                Id = donacion.Id,
                NumeroReferencia = donacion.NumeroReferencia,
                Fecha = donacion.Fecha,
                Estado = donacion.Estado,
                Descripcion = donacion.Descripcion,
                FotoRutaActual = donacion.FotoRuta,
                Detalles = donacion.Equipamientos
                    .Select(e => new DetalleDonacionViewModel
                    {
                        TipoEquipamiento = e.TipoEquipamiento,
                        Cantidad = e.Cantidad
                    })
                    .ToList()
            };

            return View(model);
        }

        // =========================================================
        // EDIT - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RegistrarDonacionViewModel model)
        {
            var empresa = await ObtenerEmpresaActual();
            if (empresa == null) return NotFound();

            var donacion = await _context.Donaciones
                .Include(d => d.Equipamientos)
                .FirstOrDefaultAsync(d =>
                    d.Id == id &&
                    d.EmpresaId == empresa.Id);

            if (donacion == null) return NotFound();

            if (donacion.Estado != EstadoDonacion.Disponible)
            {
                TempData["Error"] =
                    "La donación no puede modificarse porque ya no se encuentra disponible.";

                return RedirectToAction(nameof(Index));
            }

            model.Detalles ??= new List<DetalleDonacionViewModel>();

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
                    "Debe existir al menos un equipamiento.");
            }

            if (!ModelState.IsValid)
            {
                model.Id = donacion.Id;
                model.NumeroReferencia = donacion.NumeroReferencia;
                model.Fecha = donacion.Fecha;
                model.Estado = donacion.Estado;
                model.FotoRutaActual = donacion.FotoRuta;

                return View(model);
            }

            donacion.Descripcion = model.Descripcion;

            if (model.FotoArchivo != null)
            {
                donacion.FotoRuta = await _imagenStorage.GuardarImagenAsync(model.FotoArchivo, "donaciones");
            }

            _context.RemoveRange(donacion.Equipamientos);

            donacion.Equipamientos = model.Detalles
                .Select(d => new DetalleDonacion
                {
                    DonacionId = donacion.Id,
                    TipoEquipamiento = d.TipoEquipamiento,
                    Cantidad = d.Cantidad
                })
                .ToList();

            _context.Observaciones.Add(new Observacion
            {
                Mensaje = $"La empresa modificó los equipamientos de la donación {donacion.NumeroReferencia}.",
                Fecha = DateTime.Now,
                Donacion = donacion
            });

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"La donación {donacion.NumeroReferencia} fue modificada correctamente.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // CANCELAR DONACIÓN - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id)
        {
            var empresa = await ObtenerEmpresaActual();
            if (empresa == null) return NotFound();

            var donacion = await _context.Donaciones
                .FirstOrDefaultAsync(d =>
                    d.Id == id &&
                    d.EmpresaId == empresa.Id);

            if (donacion == null) return NotFound();

            if (donacion.Estado == EstadoDonacion.Cancelada)
            {
                TempData["Error"] = "La donación ya se encuentra cancelada.";
                return RedirectToAction(nameof(Index));
            }

            if (donacion.Estado != EstadoDonacion.Disponible)
            {
                TempData["Error"] =
                    "Solo se pueden cancelar donaciones que estén disponibles. Si tiene una asignación activa, primero debés cancelar la asignación.";
                return RedirectToAction(nameof(Index));
            }

            var tieneAsignacionesActivas = await _context.Asignaciones
                .AnyAsync(a =>
                    a.DonacionId == donacion.Id &&
                    a.Estado == EstadoAsignacion.Activa);

            if (tieneAsignacionesActivas)
            {
                TempData["Error"] =
                    "No se puede cancelar la donación mientras tenga asignaciones activas.";
                return RedirectToAction(nameof(Index));
            }

            donacion.Estado = EstadoDonacion.Cancelada;

            _context.Observaciones.Add(new Observacion
            {
                Mensaje = $"La empresa canceló la donación {donacion.NumeroReferencia}.",
                Fecha = DateTime.Now,
                Donacion = donacion
            });

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"La donación {donacion.NumeroReferencia} fue cancelada correctamente.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // CANCELAR ASIGNACIÓN - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelarAsignacion(int asignacionId)
        {
            var empresa = await ObtenerEmpresaActual();
            if (empresa == null) return NotFound();

            var asignacion = await _context.Asignaciones
                .Include(a => a.Donacion)
                .Include(a => a.Solicitud)
                .FirstOrDefaultAsync(a => a.Id == asignacionId);

            if (asignacion == null || asignacion.Donacion.EmpresaId != empresa.Id)
                return NotFound("Asignación no encontrada.");

            if (asignacion.Estado != EstadoAsignacion.Activa ||
                asignacion.Donacion.Estado != EstadoDonacion.Asignada)
            {
                TempData["Error"] =
                    "La asignación no se puede cancelar porque ya fue procesada o cancelada anteriormente.";
                return RedirectToAction(nameof(Index));
            }

            var fecha = DateTime.Now;

            await using var transaction = await _context.Database.BeginTransactionAsync();

            // 1. Cancelar la asignación
            asignacion.Estado = EstadoAsignacion.Cancelada;

            // 2. Devolver la donación a estado Disponible
            asignacion.Donacion.Estado = EstadoDonacion.Disponible;

            // 3. Devolver la solicitud de la institución a Pendiente si no tiene otras asignaciones activas
            var tieneOtraAsignacionActiva = await _context.Asignaciones
                .AnyAsync(a =>
                    a.SolicitudId == asignacion.SolicitudId &&
                    a.Id != asignacion.Id &&
                    a.Estado == EstadoAsignacion.Activa);

            if (!tieneOtraAsignacionActiva &&
                asignacion.Solicitud.Estado == EstadoSolicitud.EnCoordinacion)
            {
                asignacion.Solicitud.Estado = EstadoSolicitud.Pendiente;
            }

            // 4. Registrar observaciones en las 3 entidades
            _context.Observaciones.Add(new Observacion
            {
                Mensaje =
                    $"La empresa canceló la asignación {asignacion.NumeroReferencia} " +
                    $"(Solicitud {asignacion.Solicitud.NumeroReferencia}). " +
                    $"La donación volvió a estar disponible.",
                Fecha = fecha,
                Donacion = asignacion.Donacion
            });

            _context.Observaciones.Add(new Observacion
            {
                Mensaje =
                    $"La empresa canceló la asignación {asignacion.NumeroReferencia} " +
                    $"correspondiente a la donación {asignacion.Donacion.NumeroReferencia}. " +
                    $"La solicitud volvió a estado pendiente.",
                Fecha = fecha,
                Solicitud = asignacion.Solicitud
            });

            _context.Observaciones.Add(new Observacion
            {
                Mensaje =
                    $"La empresa canceló la asignación {asignacion.NumeroReferencia}.",
                Fecha = fecha,
                Asignacion = asignacion
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] =
                $"La asignación {asignacion.NumeroReferencia} fue cancelada y la donación {asignacion.Donacion.NumeroReferencia} volvió a estar disponible.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // COINCIDENCIAS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Coincidencias(int id)
        {
            var empresa = await ObtenerEmpresaActual();
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

            var solicitudesCandidatas = await _context.Solicitudes
                .Include(s => s.Institucion)
                    .ThenInclude(i => i.Domicilio)
                .Include(s => s.Equipamientos)
                .Where(s => s.Estado == EstadoSolicitud.Pendiente &&
                            s.Equipamientos.Any(e => e.Cantidad > 0 && tiposDonados.Contains(e.TipoEquipamiento)))
                .ToListAsync();

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

        // =========================================================
        // ASIGNAR - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Asignar(int donacionId, int solicitudId)
        {
            var empresa = await ObtenerEmpresaActual();
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

            solicitud.Estado = EstadoSolicitud.EnCoordinacion;

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

        // =========================================================
        // CONFIRMAR ENTREGA - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarEntrega(int asignacionId)
        {
            var empresa = await ObtenerEmpresaActual();
            if (empresa == null)
                return NotFound("Debe completar su perfil antes de confirmar una entrega.");

            var asignacion = await _context.Asignaciones
                .Include(a => a.Donacion)
                .Include(a => a.Solicitud)
                .FirstOrDefaultAsync(a => a.Id == asignacionId);

            if (asignacion == null || asignacion.Donacion.EmpresaId != empresa.Id)
                return NotFound("Asignación no encontrada.");

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

        // =========================================================
        // AUXILIARES
        // =========================================================

        private async Task<Empresa?> ObtenerEmpresaActual()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return null;

            return await _context.Empresas
                .Include(e => e.Domicilio)
                .FirstOrDefaultAsync(e => e.UsuarioId == userId);
        }

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