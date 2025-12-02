using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using NorthwindWeb.Models;

namespace NorthwindWeb.Pages.Customers
{
    public class EditModel : PageModel
    {
        private readonly IConfiguration _configuration;

        [BindProperty]
        public Customer Cliente { get; set; } = new Customer();

        [BindProperty(SupportsGet = true)]
        public string? CustomerID { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        public EditModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult OnGet()
        {
            if (string.IsNullOrEmpty(CustomerID))
                return RedirectToPage("./Index");

            string connStr = _configuration.GetConnectionString("NorthwindConn")!;
            
            try
            {
                using SqlConnection conn = new SqlConnection(connStr);
                conn.Open();

                string query = @"
                    SELECT CustomerID, CompanyName, ContactName, Address, City, Region, 
                           PostalCode, Country, Phone, Fax
                    FROM Customers
                    WHERE CustomerID = @CustomerID;";

                using SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@CustomerID", CustomerID);

                using SqlDataReader reader = cmd.ExecuteReader();

                if (!reader.Read())
                {
                    ErrorMessage = "Cliente no encontrado.";
                    return RedirectToPage("./Index");
                }

                Cliente.CustomerID = reader["CustomerID"].ToString()!;
                Cliente.CompanyName = reader["CompanyName"].ToString()!;
                Cliente.ContactName = reader["ContactName"].ToString()!;
                Cliente.Address = reader["Address"].ToString()!;
                Cliente.City = reader["City"].ToString()!;
                Cliente.Region = reader["Region"] as string;
                Cliente.PostalCode = reader["PostalCode"] as string;
                Cliente.Country = reader["Country"].ToString()!;
                Cliente.Phone = reader["Phone"].ToString()!;
                Cliente.Fax = reader["Fax"] as string;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar datos: {ex.Message}";
            }

            return Page();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
                return Page();

            string connStr = _configuration.GetConnectionString("NorthwindConn")!;

            try
            {
                using SqlConnection conn = new SqlConnection(connStr);
                conn.Open();

                string query = @"
                    UPDATE Customers
                    SET ContactName=@ContactName, Address=@Address,
                        City=@City, Region=@Region, PostalCode=@PostalCode,
                        Country=@Country, Phone=@Phone, Fax=@Fax
                    WHERE CustomerID=@CustomerID;";

                using SqlCommand cmd = new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@CustomerID", Cliente.CustomerID);
                cmd.Parameters.AddWithValue("@ContactName", Cliente.ContactName);
                cmd.Parameters.AddWithValue("@Address", Cliente.Address);
                cmd.Parameters.AddWithValue("@City", Cliente.City);
                cmd.Parameters.AddWithValue("@Region", (object?)Cliente.Region ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PostalCode", (object?)Cliente.PostalCode ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Country", Cliente.Country);
                cmd.Parameters.AddWithValue("@Phone", Cliente.Phone);
                cmd.Parameters.AddWithValue("@Fax", (object?)Cliente.Fax ?? DBNull.Value);

                cmd.ExecuteNonQuery();

                SuccessMessage = $"El cliente '{Cliente.CompanyName}' fue actualizado correctamente.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al actualizar: {ex.Message}";
            }

            return Page(); // ← Igual que Create
        }
    }
}
