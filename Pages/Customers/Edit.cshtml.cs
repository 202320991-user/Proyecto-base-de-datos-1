using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using NorthwindWeb.Models;

namespace NorthwindWeb.Pages.Customers
{
    public class EditModel : PageModel
    {
        private readonly IConfiguration _configuration;
        
        // El cliente se enlaza al formulario para cargar y guardar datos
        [BindProperty]
        public Customer Cliente { get; set; } = new Customer();

        // Usamos CustomerID para identificar el registro a editar
        [BindProperty(SupportsGet = true)]
        public string? CustomerID { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        public EditModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Maneja la solicitud GET: Cargar los datos del cliente
        public IActionResult OnGet()
        {
            if (string.IsNullOrEmpty(CustomerID))
            {
                return RedirectToPage("./Index"); // Si no hay ID, volvemos al listado
            }

            string? connStr = _configuration.GetConnectionString("NorthwindConn");
            if (string.IsNullOrEmpty(connStr))
            {
                ErrorMessage = "Error de Configuración: Cadena de conexión no disponible.";
                return Page();
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    // Consulta para obtener todos los detalles del cliente
                    string query = @"
                        SELECT 
                            CustomerID, CompanyName, ContactName, Address, City, Region, 
                            PostalCode, Country, Phone, Fax
                        FROM 
                            Customers
                        WHERE 
                            CustomerID = @CustomerID;";

                    using SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@CustomerID", CustomerID.Trim().ToUpper());

                    using SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        // Cargar el modelo con los datos actuales
                        Cliente.CustomerID = reader["CustomerID"].ToString() ?? "";
                        Cliente.CompanyName = reader["CompanyName"].ToString() ?? "";
                        Cliente.ContactName = reader["ContactName"].ToString() ?? "";
                        Cliente.Address = reader["Address"].ToString() ?? "";
                        Cliente.City = reader["City"].ToString() ?? "";
                        Cliente.Region = reader["Region"] is DBNull ? null : reader["Region"].ToString();
                        Cliente.PostalCode = reader["PostalCode"] is DBNull ? null : reader["PostalCode"].ToString();
                        Cliente.Country = reader["Country"].ToString() ?? "";
                        Cliente.Phone = reader["Phone"].ToString() ?? "";
                        Cliente.Fax = reader["Fax"] is DBNull ? null : reader["Fax"].ToString();
                    }
                    else
                    {
                        ErrorMessage = $"Cliente con ID '{CustomerID}' no encontrado.";
                        return RedirectToPage("./Index"); 
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar datos del cliente: {ex.Message}";
                return Page();
            }

            return Page();
        }

        // Maneja la solicitud POST: Guardar los cambios
        public IActionResult OnPost()
        {
            // 1. Validar el modelo
            if (!ModelState.IsValid)
            {
                // Si la validación falla, volvemos a la página con los datos ingresados
                return Page();
            }
            
            string? connStr = _configuration.GetConnectionString("NorthwindConn");
            if (string.IsNullOrEmpty(connStr))
            {
                ErrorMessage = "Error de Configuración: Cadena de conexión no disponible.";
                return Page();
            }

            // 2. Lógica de Actualización Segura
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    // Consulta SQL para ACTUALIZAR (solo los campos que cambian)
                    string query = @"
                        UPDATE Customers 
                        SET 
                            CompanyName = @CompanyName,
                            ContactName = @ContactName,
                            Address = @Address,
                            City = @City,
                            Region = @Region,
                            PostalCode = @PostalCode,
                            Country = @Country,
                            Phone = @Phone,
                            Fax = @Fax
                        WHERE CustomerID = @CustomerID;";

                    using SqlCommand cmd = new SqlCommand(query, conn);

                    // Usando parámetros para la actualización segura
                    cmd.Parameters.AddWithValue("@CustomerID", Cliente.CustomerID);
                    cmd.Parameters.AddWithValue("@CompanyName", Cliente.CompanyName);
                    cmd.Parameters.AddWithValue("@ContactName", Cliente.ContactName);
                    cmd.Parameters.AddWithValue("@Address", Cliente.Address);
                    cmd.Parameters.AddWithValue("@City", Cliente.City);
                    // Manejar campos opcionales que pueden ser NULL en la BD
                    cmd.Parameters.AddWithValue("@Region", (object?)Cliente.Region ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PostalCode", (object?)Cliente.PostalCode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Country", Cliente.Country);
                    cmd.Parameters.AddWithValue("@Phone", Cliente.Phone);
                    cmd.Parameters.AddWithValue("@Fax", (object?)Cliente.Fax ?? DBNull.Value);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        SuccessMessage = $"El cliente '{Cliente.CompanyName}' ha sido actualizado exitosamente.";
                        // Redirigir para evitar el reenvío del formulario
                        return RedirectToPage(new { CustomerID = Cliente.CustomerID, SuccessMessage = SuccessMessage });
                    }
                    else
                    {
                        ErrorMessage = "Error: No se pudo encontrar o actualizar el cliente.";
                        return Page();
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al actualizar el cliente: {ex.Message}";
                return Page();
            }
        }
    }
}