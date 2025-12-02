using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using NorthwindWeb.Models;

namespace NorthwindWeb.Pages.Reports
{
    public class VentasPorClienteMesModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public VentasPorClienteMesModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string ErrorMessage { get; set; } = string.Empty;

        public List<VentasClienteMesDto> Resultados { get; set; } = new List<VentasClienteMesDto>();

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
                    MONTH(o.OrderDate) AS Mes,
                    SUM(od.UnitPrice * od.Quantity * (1 - od.Discount)) AS TotalCompradoMes
                FROM Customers c
                JOIN Orders o ON c.CustomerID = o.CustomerID
                JOIN [Order Details] od ON o.OrderID = od.OrderID
                GROUP BY c.CustomerID, c.CompanyName, YEAR(o.OrderDate), MONTH(o.OrderDate)
                ORDER BY Anio DESC, Mes DESC, TotalCompradoMes DESC;";

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
                            Resultados.Add(new VentasClienteMesDto
                            {
                                CustomerID = reader["CustomerID"].ToString() ?? "",
                                CompanyName = reader["CompanyName"].ToString() ?? "",
                                Anio = reader["Anio"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Anio"]),
                                Mes = reader["Mes"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Mes"]),
                                TotalCompradoMes = reader["TotalCompradoMes"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["TotalCompradoMes"])
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

        public class VentasClienteMesDto
        {
            public string CustomerID { get; set; } = string.Empty;
            public string CompanyName { get; set; } = string.Empty;
            public int Anio { get; set; }
            public int Mes { get; set; }
            public decimal TotalCompradoMes { get; set; }
        }
    }
}