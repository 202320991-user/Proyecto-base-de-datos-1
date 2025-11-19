using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace NorthwindWeb.Pages.Reports
{
    public class VentasPorEmpleadoModel : PageModel
    {
        private readonly IConfiguration _configuration;
        // La lista almacenará los resultados de la consulta
        public List<VentasEmpleadoReporte> Resultados { get; set; } = new();

        public VentasPorEmpleadoModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet()
        {
            string? connStr = _configuration.GetConnectionString("NorthwindConn");

            if (string.IsNullOrEmpty(connStr))
            {
                // Manejo de error: la cadena de conexión no está configurada
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    // Consulta SQL compleja: Ventas totales por empleado y año (JOIN, GROUP BY, SUM)
                    string query = @"
                        SELECT 
                            E.FirstName + ' ' + E.LastName AS NombreCompleto,
                            YEAR(O.OrderDate) AS AnioVenta,
                            CAST(SUM(OD.UnitPrice * OD.Quantity * (1 - OD.Discount)) AS DECIMAL(10, 2)) AS VentaTotalAnual
                        FROM 
                            Employees AS E
                        JOIN 
                            Orders AS O ON E.EmployeeID = O.EmployeeID
                        JOIN 
                            [Order Details] AS OD ON O.OrderID = OD.OrderID
                        GROUP BY 
                            E.FirstName, E.LastName, YEAR(O.OrderDate)
                        ORDER BY 
                            NombreCompleto, AnioVenta;";

                    using SqlCommand cmd = new SqlCommand(query, conn);
                    
                    using SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        Resultados.Add(new VentasEmpleadoReporte
                        {
                            NombreCompleto = reader["NombreCompleto"].ToString() ?? "",
                            AnioVenta = (int)reader["AnioVenta"],
                            VentaTotalAnual = (decimal)reader["VentaTotalAnual"]
                        });
                    }
                }
            }
            catch (SqlException)
            {
                // Manejo de errores específicos de SQL
            }
            catch (Exception)
            {
                // Manejo de errores generales
            }
        }
    }
    
    // Modelo de datos utilizado para almacenar los resultados del reporte
    public class VentasEmpleadoReporte
    {
        public string NombreCompleto { get; set; } = string.Empty;
        public int AnioVenta { get; set; }
        public decimal VentaTotalAnual { get; set; }
    }
}