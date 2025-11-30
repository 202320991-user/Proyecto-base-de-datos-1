using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace NorthwindWeb.Pages.Orders
{
    public class DeleteModel : PageModel
    {
        private readonly IConfiguration _configuration;

        [BindProperty(SupportsGet = true)]
        public int? OrderID { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }

        public DeleteModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // ----------------------------
        //   🔥 Método GET corregido
        // ----------------------------
        public IActionResult OnGet(int? OrderID, string? success)
        {
            this.OrderID = OrderID;

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
                ErrorMessage = "Cadena de conexión no disponible.";
                return Page();
            }

            try
            {
                using SqlConnection conn = new SqlConnection(connStr);
                conn.Open();

                string query = @"
                    SELECT O.OrderDate, C.CompanyName
                    FROM Orders AS O
                    JOIN Customers AS C ON O.CustomerID = C.CustomerID
                    WHERE O.OrderID = @OrderID;
                ";

                using SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@OrderID", OrderID.Value);

                using SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    OrderDate = reader.GetDateTime(reader.GetOrdinal("OrderDate"));
                    CompanyName = reader["CompanyName"].ToString() ?? "N/A";
                }
                else
                {
                    ErrorMessage = $"Pedido con ID {OrderID} no encontrado.";
                    this.OrderID = null;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al obtener datos: {ex.Message}";
            }

            return Page();
        }

        // Método POST
        public IActionResult OnPost()
        {
            if (OrderID == null)
            {
                ErrorMessage = "ID no proporcionado.";
                return Page();
            }

            string? connStr = _configuration.GetConnectionString("NorthwindConn");

            if (string.IsNullOrEmpty(connStr))
            {
                ErrorMessage = "Cadena de conexión no disponible.";
                return Page();
            }

            try
            {
                using SqlConnection conn = new SqlConnection(connStr);
                conn.Open();

                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    // Eliminar detalles
                    string deleteDetailsQuery = "DELETE FROM [Order Details] WHERE OrderID = @OrderID;";
                    using (SqlCommand cmdDetails = new SqlCommand(deleteDetailsQuery, conn, transaction))
                    {
                        cmdDetails.Parameters.AddWithValue("@OrderID", OrderID.Value);
                        cmdDetails.ExecuteNonQuery();
                    }

                    // Eliminar pedido principal
                    string deleteOrderQuery = "DELETE FROM Orders WHERE OrderID = @OrderID;";
                    using (SqlCommand cmdOrder = new SqlCommand(deleteOrderQuery, conn, transaction))
                    {
                        cmdOrder.Parameters.AddWithValue("@OrderID", OrderID.Value);
                        int rows = cmdOrder.ExecuteNonQuery();

                        if (rows == 0)
                            throw new Exception("No se pudo eliminar el pedido.");
                    }

                    transaction.Commit();

                    string msg = $"Pedido {OrderID.Value} eliminado correctamente.";

                    return RedirectToPage("/Customers/Index", new { success = msg });
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al eliminar: {ex.Message}";
                OrderID = null;
                return Page();
            }
        }
    }
}
