namespace Vinculo.Models
{
    public class Empresa
    {
        public int Id { get; set; }
        public string RazonSocial { get; set; }
        public string Cuit { get; set; }
        public string Telefono { get; set; }
        public Domicilio Domicilio { get; set; }
        public string Email { get; set; }
    }
}
