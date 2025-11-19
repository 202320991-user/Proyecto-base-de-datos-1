using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using NorthwindWeb.Models; 

namespace NorthwindWeb.Pages.Customers
{
    public class CreateModel : PageModel
    {
        private readonly IConfiguration _configuration;

        // Propiedad que enlaza el formulario al modelo Customer, lista para validación.
        [BindProperty]
        public Customer NuevoCliente { get; set; } = new Customer();

        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        public CreateModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Simplemente carga la página del formulario
        public void OnGet() { }

        // Maneja el envío del formulario (POST)
        public IActionResult OnPost()
        {
            // 1. Validar el modelo (usa los atributos [Required], [StringLength], etc.)
            if (!ModelState.IsValid)
            {
                // Si hay errores de validación, vuelve a cargar la página con el formulario y los mensajes de error.
                return Page();
            }
            
            string? connStr = _configuration.GetConnectionString("NorthwindConn");
            if (string.IsNullOrEmpty(connStr))
            {
                ErrorMessage = "Error de Configuración: Cadena de conexión no disponible.";
                return Page();
            }

            // 2. Lógica de Inserción Segura
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    // Consulta SQL para INSERTAR un nuevo cliente
                    string query = @"
                        INSERT INTO Customers (CustomerID, CompanyName, ContactName, Country)
                        VALUES (@CustomerID, @CompanyName, @ContactName, @Country);";

                    using SqlCommand cmd = new SqlCommand(query, conn);

                    // 🔒 Usando parámetros para la inserción segura
                    cmd.Parameters.AddWithValue("@CustomerID", NuevoCliente.CustomerID.ToUpper());
                    cmd.Parameters.AddWithValue("@CompanyName", NuevoCliente.CompanyName);
                    cmd.Parameters.AddWithValue("@ContactName", NuevoCliente.ContactName);
                    // Usar DBNull.Value para campos opcionales si vienen vacíos
                    cmd.Parameters.AddWithValue("@Country", (object?)NuevoCliente.Country ?? DBNull.Value);

                    cmd.ExecuteNonQuery();
                }

                // 3. Limpiar el formulario y mostrar mensaje de éxito
                SuccessMessage = $"El cliente '{NuevoCliente.CompanyName}' ha sido registrado exitosamente.";
                NuevoCliente = new Customer(); // Limpiar el modelo para el siguiente registro
                return Page();
            }
            catch (SqlException ex) when (ex.Number == 2627) // Código de error de llave primaria duplicada
            {
                ErrorMessage = $"Error: El ID de Cliente '{NuevoCliente.CustomerID}' ya existe. Por favor, use otro ID.";
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al registrar el cliente: {ex.Message}";
                return Page();
            }
        }
    }
}