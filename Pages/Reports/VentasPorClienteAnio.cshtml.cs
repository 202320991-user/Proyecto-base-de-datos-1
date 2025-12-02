using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace NorthwindWeb.Pages.Reports
{
    public class VentasPorClienteAnioModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public VentasPorClienteAnioModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string ErrorMessage { get; set; } = string.Empty;

        public List<VentasClienteAnioDto> Resultados { get; set; } = new List<VentasClienteAnioDto>();

        public void OnGet()
        {
            string? connStr = _configuration.GetConnectionString("NorthwindConn");
            if (string.IsNullOrEmpty(connStr))
            {
                ErrorMessage = "Cadena de conexión no disponible.";
                return;
            }

            string query = @"
                SELECT 
                    c.CustomerID,
                    c.CompanyName,
                    YEAR(o.OrderDate) AS Anio,
                    SUM(od.UnitPrice * od.Quantity * (1 - od.Discount)) AS TotalComprado
                FROM Customers c
                JOIN Orders o        ON c.CustomerID = o.CustomerID
                JOIN [Order Details] od ON o.OrderID = od.OrderID
                GROUP BY c.CustomerID, c.CompanyName, YEAR(o.OrderDate)
                ORDER BY Anio DESC, TotalComprado DESC;";

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
                            Resultados.Add(new VentasClienteAnioDto
                            {
                                CustomerID = reader["CustomerID"].ToString() ?? "",
                                CompanyName = reader["CompanyName"].ToString() ?? "",
                                Anio = reader["Anio"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Anio"]),
                                TotalComprado = reader["TotalComprado"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["TotalComprado"])
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

        public class VentasClienteAnioDto
        {
            public string CustomerID { get; set; } = string.Empty;
            public string CompanyName { get; set; } = string.Empty;
            public int Anio { get; set; }
            public decimal TotalComprado { get; set; }
        }
    }
}