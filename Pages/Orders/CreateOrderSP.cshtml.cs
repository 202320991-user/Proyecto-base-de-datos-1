using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Data;
using System.ComponentModel.DataAnnotations;

namespace NorthwindWeb.Pages.Orders
{
    
    public class PedidoSPInputModel
    {
        [Required(ErrorMessage = "El ID de Cliente es obligatorio.")]
        [StringLength(5, ErrorMessage = "Debe tener 5 caracteres.")]
        [Display(Name = "ID Cliente")]
        public string CustomerID { get; set; } = string.Empty;

        [Required(ErrorMessage = "El Vendedor es obligatorio.")]
        [Display(Name = "ID Vendedor")]
        [Range(1, 9, ErrorMessage = "Debe ser un ID de empleado válido (1-9).")]
        public int EmployeeID { get; set; } = 1;

        [Required(ErrorMessage = "La Fecha Requerida es obligatoria.")]
        [Display(Name = "Fecha Requerida")]
        public DateTime RequiredDate { get; set; } = DateTime.Now.AddDays(7); 

        [Required(ErrorMessage = "La Compañía de Envío es obligatoria.")]
        [Display(Name = "ID Envío")]
        [Range(1, 3, ErrorMessage = "Debe ser un ID de compañía de envío válido (1-3).")]
        public int ShipVia { get; set; } = 1; 

        [Required(ErrorMessage = "El Producto es obligatorio.")]
        [Display(Name = "ID Producto")]
        public int ProductID { get; set; }

        [Required(ErrorMessage = "La Cantidad es obligatoria.")]
        [Range(1, 100, ErrorMessage = "La cantidad debe estar entre 1 y 100.")]
        public short Quantity { get; set; } = 1;

        [Range(0.0, 0.5, ErrorMessage = "El descuento debe estar entre 0 y 50%.")]
        public float Discount { get; set; } = 0f;

        
        public string ShipName { get; set; } = "N/A";
        public decimal Freight { get; set; } = 0.0m;
        public string ShipAddress { get; set; } = "N/A";
        public string ShipCity { get; set; } = "N/A";
        public string ShipCountry { get; set; } = "N/A";
    }

    public class CreateOrderSPModel : PageModel
    {
        private readonly IConfiguration _configuration;
        
        [BindProperty]
        public PedidoSPInputModel Input { get; set; } = new PedidoSPInputModel();
        
        
        public List<(int Id, string Name)> Employees { get; set; } = new List<(int, string)>();
        public List<(int Id, string Name)> Shippers { get; set; } = new List<(int, string)>();
        
        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        public CreateOrderSPModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet()
        {
            LoadLookupData();
        }

        
        private void LoadLookupData()
        {
            
            for (int i = 1; i <= 9; i++) Employees.Add((i, $"Vendedor {i}"));
            Shippers.Add((1, "Speedy Express"));
            Shippers.Add((2, "United Package"));
            Shippers.Add((3, "Federal Shipping"));
        }

        public IActionResult OnPost()
        {
            LoadLookupData(); 
            
            
            if (!ModelState.IsValid)
            {
                return Page();
            }

            string? connStr = _configuration.GetConnectionString("NorthwindConn");
            if (string.IsNullOrEmpty(connStr))
            {
                ErrorMessage = "Error de Configuración: Cadena de conexión no definida.";
                return Page();
            }

            
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    
                    using SqlCommand cmd = new SqlCommand("SP_RegistrarNuevoPedidoCompleto", conn);
                    cmd.CommandType = CommandType.StoredProcedure; 

                    
                    cmd.Parameters.AddWithValue("@CustomerID", Input.CustomerID.ToUpper());
                    cmd.Parameters.AddWithValue("@EmployeeID", Input.EmployeeID);
                    cmd.Parameters.AddWithValue("@RequiredDate", Input.RequiredDate);
                    cmd.Parameters.AddWithValue("@ShipVia", Input.ShipVia);
                    cmd.Parameters.AddWithValue("@Freight", Input.Freight);
                    
                    cmd.Parameters.AddWithValue("@ShipName", Input.ShipName);
                    cmd.Parameters.AddWithValue("@ShipAddress", Input.ShipAddress);
                    cmd.Parameters.AddWithValue("@ShipCity", Input.ShipCity);
                    
                    
                    cmd.Parameters.AddWithValue("@ShipRegion", DBNull.Value); 
                    cmd.Parameters.AddWithValue("@ShipPostalCode", DBNull.Value); 
                    
                    cmd.Parameters.AddWithValue("@ShipCountry", Input.ShipCountry);

                    
                    cmd.Parameters.AddWithValue("@ProductID", Input.ProductID);
                    cmd.Parameters.AddWithValue("@Quantity", Input.Quantity);
                    cmd.Parameters.AddWithValue("@Discount", Input.Discount);

                    
                    var reader = cmd.ExecuteReader();
                    int newOrderID = -1;
                    if (reader.Read())
                    {
                        newOrderID = (int)reader["NewOrderID"];
                    }
                    reader.Close();

                    if (newOrderID > 0)
                    {
                        SuccessMessage = $"Pedido {newOrderID} registrado exitosamente usando el Procedimiento Almacenado.";
                        Input = new PedidoSPInputModel(); 
                    }
                    else
                    {
                        ErrorMessage = "El pedido no pudo ser registrado. Verifique los datos de entrada.";
                    }
                }
            }
            catch (Exception ex)
            {
                
                ErrorMessage = $"Error al registrar el pedido: {ex.Message}";
            }
            return Page();
        }
    }
}