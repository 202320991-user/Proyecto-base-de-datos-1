using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace NorthwindWeb.Pages.Reports
{
    public class DetallesDePedidoModel : PageModel
    {
        private readonly IConfiguration _configuration;
        
        
        [BindProperty(SupportsGet = true)]
        public int OrderID { get; set; }

        
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
            
            OrderID = orderID; 

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
                        
                        decimal totalLinea = Convert.ToDecimal(reader["TotalLinea"]);
                        
                        Detalles.Add(new DetalleLineaReporte
                        {
                            
                            ProductID = reader.GetInt32(reader.GetOrdinal("ProductID")), 
                            ProductName = reader["ProductName"].ToString() ?? "N/A",
                            
                            UnitPrice = Convert.ToDecimal(reader["UnitPrice"]),
                            Quantity = reader.GetInt16(reader.GetOrdinal("Quantity")),
                            Discount = reader.GetFloat(reader.GetOrdinal("Discount")), 
                            TotalLinea = totalLinea
                        });
                        TotalGeneral += totalLinea;
                    }
                }
            }
            catch (Exception ex)
            {
                
                ErrorMessage = $"Error al cargar detalles: {ex.Message}";
            }
        }
    }

    
    public class DetalleLineaReporte
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public short Quantity { get; set; }
        public float Discount { get; set; } 
        public decimal TotalLinea { get; set; }
    }
}