using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using Microsoft.Data.SqlClient; 
using NorthwindWeb.Models;
using System;
using Microsoft.Extensions.Configuration; 

namespace NorthwindWeb.Pages.Customers
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;
        public List<Customer> Clientes { get; set; } = new();

        
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10; 
        public int TotalRecords { get; set; }
        
        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);

        
        
        [BindProperty(SupportsGet = true)]
        public string Search { get; set; } = "";

        public IndexModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet(int pageNumber = 1)
        {
            // Valida el número de página
            if (pageNumber < 1) pageNumber = 1;

            PageNumber = pageNumber;
            string? connStr = _configuration.GetConnectionString("NorthwindConn");

            if (string.IsNullOrEmpty(connStr))
            {
                 
                throw new InvalidOperationException(
                    "La cadena de conexión 'NorthwindConn' no está configurada en appsettings.json.");
            }

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                
                string where = "";
                if (!string.IsNullOrWhiteSpace(Search))
                {
                    
                    where = @"WHERE CustomerID LIKE @search 
                             OR CompanyName LIKE @search 
                             OR ContactName LIKE @search 
                             OR Country LIKE @search";
                }

                
                SqlCommand countCmd = new SqlCommand(
                    $"SELECT COUNT(*) FROM Customers {where}", conn);

                if (!string.IsNullOrWhiteSpace(Search))
                    
                    countCmd.Parameters.AddWithValue("@search", "%" + Search + "%");

                TotalRecords = (int)countCmd.ExecuteScalar();

                
                if (PageNumber > TotalPages && TotalPages > 0)
                {
                    PageNumber = TotalPages;
                }
                
                
                string query = $@"
                    SELECT CustomerID, CompanyName, ContactName, Country
                    FROM Customers
                    {where}
                    ORDER BY CustomerID
                    OFFSET {(PageNumber - 1) * PageSize} ROWS
                    FETCH NEXT {PageSize} ROWS ONLY";

                SqlCommand cmd = new SqlCommand(query, conn);

                if (!string.IsNullOrWhiteSpace(Search))
                    
                    cmd.Parameters.AddWithValue("@search", "%" + Search + "%");

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    Clientes.Add(new Customer
                    {
                        CustomerID = reader["CustomerID"].ToString() ?? "",
                        CompanyName = reader["CompanyName"].ToString() ?? "",
                        ContactName = reader["ContactName"].ToString() ?? "",
                        Country = reader["Country"].ToString() ?? ""
                    });
                }
            }
        }
    }
}