using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace NorthwindWeb.Pages.Reports
{
    public class PromedioPedidoClienteAnioModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public PromedioPedidoClienteAnioModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string ErrorMessage { get; set; } = string.Empty;

        public List<PromedioPedidoClienteDto> Resultados { get; set; } = new List<PromedioPedidoClienteDto>();

        public void OnGet()
        {
            string? connStr = _configuration.GetConnectionString("NorthwindConn");
            if (string.IsNullOrEmpty(connStr))
            {
                ErrorMessage = "Cadena de conexión no disponible.";
                return;
            }

            string query = @"
                WITH PedidoTotales AS (
                    SELECT
                        o.OrderID,
                        o.CustomerID,
                        YEAR(o.OrderDate) AS Anio,
                        SUM(od.UnitPrice * od.Quantity * (1 - od.Discount)) AS TotalPorPedido
                    FROM Orders o
                    JOIN [Order Details] od ON o.OrderID = od.OrderID
                    GROUP BY o.OrderID, o.CustomerID, YEAR(o.OrderDate)
                )
                SELECT
                    c.CustomerID,
                    c.CompanyName,
                    p.Anio,
                    AVG(p.TotalPorPedido) AS PromedioPorPedido,
                    SUM(p.TotalPorPedido) AS TotalAnual
                FROM PedidoTotales p
                JOIN Customers c ON p.CustomerID = c.CustomerID
                GROUP BY c.CustomerID, c.CompanyName, p.Anio
                ORDER BY p.Anio DESC, TotalAnual DESC;";

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Resultados.Add(new PromedioPedidoClienteDto
                            {
                                CustomerID = reader["CustomerID"].ToString() ?? "",
                                CompanyName = reader["CompanyName"].ToString() ?? "",
                                Anio = reader["Anio"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Anio"]),
                                PromedioPorPedido = reader["PromedioPorPedido"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["PromedioPorPedido"]),
                                TotalAnual = reader["TotalAnual"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["TotalAnual"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al ejecutar el reporte: {ex.Message}";
            }
        }

        public class PromedioPedidoClienteDto
        {
            public string CustomerID { get; set; } = string.Empty;
            public string CompanyName { get; set; } = string.Empty;
            public int Anio { get; set; }
            public decimal PromedioPorPedido { get; set; }
            public decimal TotalAnual { get; set; }
        }
    }
}