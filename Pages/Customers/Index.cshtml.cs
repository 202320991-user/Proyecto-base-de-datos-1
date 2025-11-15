using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using Microsoft.Data.SqlClient; // Asegúrate de que estás usando Microsoft.Data.SqlClient
using NorthwindWeb.Models;
using System;
using Microsoft.Extensions.Configuration; // Necesario para IConfiguration

namespace NorthwindWeb.Pages.Customers
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;
        public List<Customer> Clientes { get; set; } = new();

        // Propiedades de Paginación
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10; // Muestra 10 registros por página
        public int TotalRecords { get; set; }
        // Propiedad calculada: calcula el número total de páginas
        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);

        
        // Propiedad para recibir el término de búsqueda
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
                 // Manejo de error si la cadena de conexión es nula
                throw new InvalidOperationException(
                    "La cadena de conexión 'NorthwindConn' no está configurada en appsettings.json.");
            }

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                // 🔍 Construcción del filtro (Cláusula WHERE dinámica)
                string where = "";
                if (!string.IsNullOrWhiteSpace(Search))
                {
                    // La cláusula se inyectará en las consultas de COUNT y SELECT
                    where = @"WHERE CustomerID LIKE @search 
                             OR CompanyName LIKE @search 
                             OR ContactName LIKE @search 
                             OR Country LIKE @search";
                }

                // 1. OBTENER EL TOTAL DE REGISTROS (Para calcular TotalPages)
                SqlCommand countCmd = new SqlCommand(
                    $"SELECT COUNT(*) FROM Customers {where}", conn);

                if (!string.IsNullOrWhiteSpace(Search))
                    // Agrega el parámetro de búsqueda para la consulta COUNT
                    countCmd.Parameters.AddWithValue("@search", "%" + Search + "%");

                TotalRecords = (int)countCmd.ExecuteScalar();

                // Asegura que no se intente acceder a una página que no existe
                if (PageNumber > TotalPages && TotalPages > 0)
                {
                    PageNumber = TotalPages;
                }
                
                // 2. OBTENER LOS REGISTROS DE LA PÁGINA ACTUAL
                string query = $@"
                    SELECT CustomerID, CompanyName, ContactName, Country
                    FROM Customers
                    {where}
                    ORDER BY CustomerID
                    OFFSET {(PageNumber - 1) * PageSize} ROWS
                    FETCH NEXT {PageSize} ROWS ONLY";

                SqlCommand cmd = new SqlCommand(query, conn);

                if (!string.IsNullOrWhiteSpace(Search))
                    // Agrega el parámetro de búsqueda para la consulta SELECT
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