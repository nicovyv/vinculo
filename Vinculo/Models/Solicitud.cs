namespace Vinculo.Models
{
    public class Solicitud
    {
        public int Id { get; set; }
        public string NumeroReferencia { get; set; }
        public Institucion Institucion { get; set; }
        public DateTime Fecha { get; set; }
    }
}
