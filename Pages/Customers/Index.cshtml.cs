using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using NorthwindWeb.Models;

namespace NorthwindWeb.Pages.Customers
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;
        public List<Customer> Clientes { get; set; } = new();
        public int CurrentPage { get; set; }
        public bool HasNextPage { get; set; }

        public IndexModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet(int pageNumber = 1)
        {
            string? connStr = _configuration.GetConnectionString("NorthwindConn");

            if (string.IsNullOrEmpty(connStr))
            {
                throw new InvalidOperationException(
                    "La cadena de conexión 'NorthwindConn' no está configurada en appsettings.json.");
            }

            int pageSize = 20; // 🔹 Mostrará 20 registros por página
            int offset = (pageNumber - 1) * pageSize;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                string query = $@"
        SELECT CustomerID, CompanyName, ContactName, Country
        FROM Customers
        ORDER BY CustomerID
        OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY;";

                using SqlCommand cmd = new SqlCommand(query, conn);
                using SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    Clientes.Add(new Customer
                    {
                        CustomerID = reader["CustomerID"]?.ToString() ?? "",
                        CompanyName = reader["CompanyName"]?.ToString() ?? "",
                        ContactName = reader["ContactName"]?.ToString() ?? "",
                        Country = reader["Country"]?.ToString() ?? ""
                    });
                }
            }

            // 🔹 Control de paginación
            CurrentPage = pageNumber;
            HasNextPage = Clientes.Count == pageSize;

        }
    }
}