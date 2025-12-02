using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace NorthwindWeb.Pages.Reports
{
    public class HistorialClienteModel : PageModel
    {
        private readonly IConfiguration _configuration;
        
        [BindProperty(SupportsGet = true)]
        public string CustomerID { get; set; } = string.Empty;

        public List<PedidoHistorial> Resultados { get; set; } = new();
        public string ErrorMessage { get; set; } = string.Empty;

        public HistorialClienteModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet()
        {
            if (string.IsNullOrEmpty(CustomerID))
            {
                return;
            }

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

                    using SqlCommand cmd = new SqlCommand("SP_ObtenerHistorialPedidos", conn);
                    cmd.CommandType = CommandType.StoredProcedure; 
                    cmd.Parameters.AddWithValue("@CustomerID", CustomerID);

                    using SqlDataReader reader = cmd.ExecuteReader();

                    if (!reader.HasRows)
                    {
                         ErrorMessage = $"No se encontraron pedidos para el cliente ID '{CustomerID}'.";
                    }

                    while (reader.Read())
                    {
                        
                        int orderId = 0;
                        if (reader["OrderID"] != DBNull.Value)
                        {
                            orderId = Convert.ToInt32(reader["OrderID"]);
                        }
                        
                        
                        DateTime? orderDate = null;
                        if (reader["OrderDate"] != DBNull.Value)
                        {
                            orderDate = Convert.ToDateTime(reader["OrderDate"]);
                        }

                        
                        decimal total = 0M;
                        if (reader["Total"] != DBNull.Value)
                        {
                            
                            total = Convert.ToDecimal(reader["Total"]); 
                        }
                        
                        Resultados.Add(new PedidoHistorial
                        {
                            OrderID = orderId,

                            FechaPedido = orderDate?.ToString("yyyy-MM-dd") ?? "N/A", 
                            Total = total
                        });
                    }
                }
            }
            catch (SqlException ex)
            {
                ErrorMessage = $"Error SQL al ejecutar el SP: {ex.Message}";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar historial: {ex.Message}";
            }
        }
    }

    
    public class PedidoHistorial
    {
        public int OrderID { get; set; }
        
        public string FechaPedido { get; set; } = string.Empty; 
        public decimal Total { get; set; }
    }
}