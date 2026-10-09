
using System;
using System.Collections.Generic;

namespace Vinculo.Models.ViewModels
{
    public class AdminEmpresaViewModel
    {
        public List<AdminEmpresaItemViewModel> Empresas { get; set; } = new();

        public string? Busqueda { get; set; }
    }

    public class AdminEmpresaItemViewModel
    {
        public int Id { get; set; }

        public string RazonSocial { get; set; } = string.Empty;

        public string Cuit { get; set; } = string.Empty;

        public string Telefono { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PersonaContacto { get; set; } = string.Empty;
    }
}