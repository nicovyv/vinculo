namespace Vinculo.Models
{
    public class Institucion
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Cuit { get; set; }
        public string Telefono { get; set; }
        public Domicilio Domicilio { get; set; }
        public string Email { get; set; }
        public List<Solicitud> Solicitudes { get; set; }

    }
}
