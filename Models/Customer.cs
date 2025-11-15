using System.ComponentModel.DataAnnotations;

namespace NorthwindWeb.Models
{
    public class Customer
    {
        // Campos Clave y Básicos
        [Required(ErrorMessage = "El ID de Cliente es obligatorio.")]
        [StringLength(5, MinimumLength = 5, ErrorMessage = "El ID debe tener exactamente 5 caracteres.")]
        [Display(Name = "ID Cliente")]
        public string CustomerID { get; set; } = string.Empty;

        [Required(ErrorMessage = "El Nombre de la Compañía es obligatorio.")]
        [StringLength(40)]
        [Display(Name = "Compañía")]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El Nombre del Contacto es obligatorio.")]
        [StringLength(30)]
        [Display(Name = "Nombre Contacto")]
        public string ContactName { get; set; } = string.Empty;
        
        // 🚨 PROPIEDADES AÑADIDAS PARA CUMPLIR EL REQUISITO DE EDICIÓN (DIRECCIÓN y TELÉFONO)
        [StringLength(60)]
        [Display(Name = "Dirección")]
        public string Address { get; set; } = string.Empty;
        
        [StringLength(15)]
        [Display(Name = "Ciudad")]
        public string City { get; set; } = string.Empty;
        
        [StringLength(15)]
        [Display(Name = "Región")]
        public string Region { get; set; } = string.Empty; // Campo opcional
        
        [StringLength(10)]
        [Display(Name = "Cód. Postal")]
        public string PostalCode { get; set; } = string.Empty; // Campo opcional

        [StringLength(15)]
        [Display(Name = "País")]
        public string Country { get; set; } = string.Empty;
        
        [StringLength(24)]
        [Display(Name = "Teléfono")]
        public string Phone { get; set; } = string.Empty;
        
        [StringLength(24)]
        [Display(Name = "Fax")]
        public string Fax { get; set; } = string.Empty; // Campo opcional
    }
}