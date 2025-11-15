using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Data;

namespace NorthwindWeb.Pages.Orders
{
    public class DeleteModel : PageModel
    {
        private readonly IConfiguration _configuration;
        
        [BindProperty(SupportsGet = true)]
        public int? OrderID { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;
        
        // Propiedades para mostrar información del pedido antes de la eliminación
        public string CompanyName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }

        public DeleteModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Maneja la solicitud GET: Muestra la información del pedido y pide confirmación
        public IActionResult OnGet(string? success)
        {
            if (OrderID == null)
            {
                ErrorMessage = "Debe proporcionar un ID de pedido válido.";
                return Page();
            }

            if (!string.IsNullOrEmpty(success))
            {
                SuccessMessage = success;
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

                    // Consulta para obtener detalles básicos del pedido para confirmar
                    string query = @"
                        SELECT O.OrderDate, C.CompanyName
                        FROM Orders AS O
                        JOIN Customers AS C ON O.CustomerID = C.CustomerID
                        WHERE O.OrderID = @OrderID;";

                    using SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@OrderID", OrderID.Value);

                    using SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        OrderDate = reader.GetDateTime("OrderDate");
                        CompanyName = reader["CompanyName"].ToString() ?? "N/A";
                    }
                    else
                    {
                        ErrorMessage = $"Pedido con ID '{OrderID}' no encontrado.";
                        OrderID = null; // Limpiar ID para no mostrar el formulario de confirmación
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar datos del pedido: {ex.Message}";
            }

            return Page();
        }

        // Maneja la solicitud POST: Ejecuta la eliminación
        public IActionResult OnPost()
        {
            if (OrderID == null)
            {
                ErrorMessage = "ID de pedido no proporcionado para la eliminación.";
                return Page();
            }

            string? connStr = _configuration.GetConnectionString("NorthwindConn");
            if (string.IsNullOrEmpty(connStr))
            {
                ErrorMessage = "Error de Configuración: Cadena de conexión no disponible.";
                return Page();
            }

            // 🚨 Lógica de Eliminación Segura usando Transacción
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    SqlTransaction transaction = conn.BeginTransaction();

                    try
                    {
                        // 1. Eliminar los detalles del pedido (Order Details)
                        string deleteDetailsQuery = "DELETE FROM [Order Details] WHERE OrderID = @OrderID;";
                        using (SqlCommand cmdDetails = new SqlCommand(deleteDetailsQuery, conn, transaction))
                        {
                            cmdDetails.Parameters.AddWithValue("@OrderID", OrderID.Value);
                            cmdDetails.ExecuteNonQuery();
                        }

                        // 2. Eliminar el pedido principal (Orders)
                        string deleteOrderQuery = "DELETE FROM Orders WHERE OrderID = @OrderID;";
                        using (SqlCommand cmdOrder = new SqlCommand(deleteOrderQuery, conn, transaction))
                        {
                            cmdOrder.Parameters.AddWithValue("@OrderID", OrderID.Value);
                            int rowsAffected = cmdOrder.ExecuteNonQuery();

                            if (rowsAffected == 0)
                            {
                                throw new Exception("El pedido principal no pudo ser encontrado o eliminado.");
                            }
                        }

                        // Si ambas eliminaciones tienen éxito, confirma los cambios
                        transaction.Commit();
                        
                        // Éxito: Redirigir al listado de pedidos (o clientes)
                        string successMsg = $"El Pedido {OrderID.Value} fue eliminado exitosamente.";
                        return RedirectToPage("/Customers/Index", new { success = successMsg });
                    }
                    catch (Exception)
                    {
                        // Si algo falla, revierte la transacción
                        transaction.Rollback();
                        throw; 
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al eliminar el pedido {OrderID.Value}: {ex.Message}";
                // Limpiar el ID para no intentar eliminar de nuevo
                OrderID = null;
                return Page();
            }
        }
    }
}