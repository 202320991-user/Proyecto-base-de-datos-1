using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using NorthwindWeb.Models;
using System.Data;
using Microsoft.AspNetCore.Mvc;

namespace NorthwindWeb.Pages.Reports
{
    public class PedidosPorClienteModel : PageModel
    {
        private readonly IConfiguration _configuration;
                public Customer Cliente { get; set; } = new Customer();
        
        public List<OrderReporte> Pedidos { get; set; } = new List<OrderReporte>();
        
        [BindProperty(SupportsGet = true)]
        public string? CustomerID { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;

        public PedidosPorClienteModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet()
        {
            if (string.IsNullOrEmpty(CustomerID))
            {
                
                return;
            }

            string? connStr = _configuration.GetConnectionString("NorthwindConn");
            if (string.IsNullOrEmpty(connStr))
            {
                ErrorMessage = "Error de Configuración: Cadena de conexión no disponible.";
                return;
            }
            
            
            string safeID = CustomerID.Trim().ToUpper();

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    
                    string queryPedidos = @"
                        SELECT 
                            OrderID, OrderDate, RequiredDate, ShippedDate, Freight, ShipCountry
                        FROM 
                            Orders
                        WHERE 
                            CustomerID = @CustomerID
                        ORDER BY 
                            OrderDate DESC;";

                    using (SqlCommand cmdPedidos = new SqlCommand(queryPedidos, conn))
                    {
                        
                        cmdPedidos.Parameters.AddWithValue("@CustomerID", safeID);
                        
                        using (SqlDataReader reader = cmdPedidos.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Pedidos.Add(new OrderReporte
                                {
                                    OrderID = (int)reader["OrderID"],
                                    OrderDate = reader.GetDateTime("OrderDate"),
                                    RequiredDate = reader.GetDateTime("RequiredDate"),
                                    ShippedDate = reader.IsDBNull(reader.GetOrdinal("ShippedDate")) ? (DateTime?)null : reader.GetDateTime("ShippedDate"),
                                    Freight = (decimal)reader["Freight"],
                                    ShipCountry = reader["ShipCountry"].ToString() ?? ""
                                });
                            }
                        }
                    }
                    
                    
                    string queryCliente = "SELECT CompanyName, ContactName, Country FROM Customers WHERE CustomerID = @CustomerID";
                    using (SqlCommand cmdCliente = new SqlCommand(queryCliente, conn))
                    {
                        cmdCliente.Parameters.AddWithValue("@CustomerID", safeID);
                        using (SqlDataReader readerCliente = cmdCliente.ExecuteReader())
                        {
                            if (readerCliente.Read())
                            {
                                Cliente.CustomerID = safeID;
                                Cliente.CompanyName = readerCliente["CompanyName"].ToString() ?? "";
                                Cliente.ContactName = readerCliente["ContactName"].ToString() ?? "";
                                Cliente.Country = readerCliente["Country"].ToString() ?? "";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar pedidos: {ex.Message}";
            }
        }
    }

    
    public class OrderReporte
    {
        public int OrderID { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime RequiredDate { get; set; }
        public DateTime? ShippedDate { get; set; }
        public decimal Freight { get; set; }
        public string ShipCountry { get; set; } = string.Empty;
    }
}