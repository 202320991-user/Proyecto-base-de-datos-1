using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace NorthwindWeb.Pages.Reports
{
    public class VentasTotalesModel : PageModel
    {
        private readonly IConfiguration _config;
        public VentasTotalesModel(IConfiguration config)
        {
            _config = config;
        }

        [BindProperty]
        public string? CustomerID { get; set; }

        [BindProperty]
        public string? Region { get; set; }

        public List<VentaDTO> Ventas { get; set; } = new List<VentaDTO>();

        public bool Posted { get; set; } = false;
        public string ErrorMessage { get; set; } = "";

        public void OnGet()
        {
        }

        public void OnPost()
        {
            Posted = true;

            string connStr = _config.GetConnectionString("NorthwindConn");

            string query = @"
                SELECT 
                    CustomerID,
                    CompanyName,
                    Country AS Region,
                    CAST(SUM(TotalVenta) AS DECIMAL(10,2)) AS VentaTotal
                FROM Vista_VentasTotales_PorCliente
                WHERE 
                    (@CustomerID IS NULL OR @CustomerID = '' OR CustomerID = @CustomerID)
                    AND (@Region IS NULL OR @Region = '' OR Country = @Region)
                GROUP BY CustomerID, CompanyName, Country
                ORDER BY VentaTotal DESC;
            ";

            try
            {
                using SqlConnection conn = new SqlConnection(connStr);
                conn.Open();

                using SqlCommand cmd = new SqlCommand(query, conn);
                cmd.CommandTimeout = 60;

                cmd.Parameters.AddWithValue("@CustomerID", (object?)CustomerID ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Region", (object?)Region ?? DBNull.Value);

                using SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    Ventas.Add(new VentaDTO
                    {
                        CustomerID = reader["CustomerID"].ToString() ?? "",
                        CompanyName = reader["CompanyName"].ToString() ?? "",
                        Region = reader["Region"].ToString() ?? "",
                        VentaTotal = Convert.ToDecimal(reader["VentaTotal"])
                    });
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al obtener datos: " + ex.Message;
            }
        }

        public class VentaDTO
        {
            public string CustomerID { get; set; } = "";
            public string CompanyName { get; set; } = "";
            public string Region { get; set; } = "";
            public decimal VentaTotal { get; set; }
        }
    }
}
