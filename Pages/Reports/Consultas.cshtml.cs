using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Data.SqlClient;
using NorthwindWeb.Models;

namespace NorthwindWeb.Pages.Reports
{
    public class Consulta1Model : PageModel
    {
        private readonly IConfiguration _configuration;
        public List<Customer> Clientes { get; set; } = new();

        public Consulta1Model(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet()
        {
            string connStr = _configuration.GetConnectionString("NorthwindConn");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                // 🔹 Aquí pones TU consulta SQL personalizada
                string query = "SELECT CustomerID, CompanyName, ContactName, Country FROM Customers WHERE Country = 'Mexico'";

                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    Clientes.Add(new Customer
                    {
                        CustomerID = reader["CustomerID"].ToString(),
                        CompanyName = reader["CompanyName"].ToString(),
                        ContactName = reader["ContactName"].ToString(),
                        Country = reader["Country"].ToString()
                    });
                }
            }
        }
    }
}
