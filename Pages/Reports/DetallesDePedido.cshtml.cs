using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace NorthwindWeb.Pages.Reports
{
    public class DetallesDePedidoModel : PageModel
    {
        private readonly IConfiguration _configuration;
        
        // Propiedad para recibir el OrderID desde el Historial o URL
        [BindProperty(SupportsGet = true)]
        public int OrderID { get; set; }

        // Lista para almacenar los resultados de la Vista
        public List<DetalleLineaReporte> Detalles { get; set; } = new();
        public decimal TotalGeneral { get; set; } = 0;
        public string ErrorMessage { get; set; } = string.Empty;

        public DetallesDePedidoModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet(int orderID)
        {
            if (orderID <= 0)
            {
                ErrorMessage = "Debe proporcionar un ID de Pedido válido.";
                return;
            }
            
            OrderID = orderID; // Aseguramos que el OrderID esté en el modelo

            string? connStr = _configuration.GetConnectionString("NorthwindConn");
            if (string.IsNullOrEmpty(connStr))
            {
                ErrorMessage = "Error de configuración de conexión.";
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    // Consulta SELECT contra la VISTA creada (Req. 3)
                    string query = @"
                        SELECT ProductID, ProductName, UnitPrice, Quantity, Discount, TotalLinea
                        FROM Vista_PedidosDetalles_TotalLinea
                        WHERE OrderID = @OrderID
                        ORDER BY ProductID;";

                    using SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@OrderID", OrderID);
                    
                    using SqlDataReader reader = cmd.ExecuteReader();

                    if (!reader.HasRows)
                    {
                         ErrorMessage = $"No se encontraron detalles de línea para el Pedido ID '{OrderID}'.";
                    }

                    while (reader.Read())
                    {
                        // Usamos Convert.ToDecimal para manejar tipos float/Single de la DB.
                        decimal totalLinea = Convert.ToDecimal(reader["TotalLinea"]);
                        
                        Detalles.Add(new DetalleLineaReporte
                        {
                            // Usamos GetInt32 para leer ProductID de forma segura.
                            ProductID = reader.GetInt32(reader.GetOrdinal("ProductID")), 
                            ProductName = reader["ProductName"].ToString() ?? "N/A",
                            // Usamos Convert.ToDecimal() también aquí si UnitPrice fuera float/money
                            UnitPrice = Convert.ToDecimal(reader["UnitPrice"]),
                            Quantity = reader.GetInt16(reader.GetOrdinal("Quantity")),
                            Discount = reader.GetFloat(reader.GetOrdinal("Discount")), // Se lee como float/Single
                            TotalLinea = totalLinea
                        });
                        TotalGeneral += totalLinea;
                    }
                }
            }
            catch (Exception ex)
            {
                // Captura y muestra cualquier error de conexión o casteo
                ErrorMessage = $"Error al cargar detalles: {ex.Message}";
            }
        }
    }

    // Modelo de datos para la línea de detalle (coincide con la Vista)
    public class DetalleLineaReporte
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public short Quantity { get; set; }
        public float Discount { get; set; } // Discount es un float en la tabla
        public decimal TotalLinea { get; set; }
    }
}