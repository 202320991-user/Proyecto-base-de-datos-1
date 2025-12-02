# PROYECTO BD

Aplicación web ASP.NET Core para gestionar y reportar datos sobre la base Northwind. Entregable: funcionalidad CRUD básica, reportes y procedimientos que automatizan la creación y consulta de pedidos.

**Contenido de esta documentación**
- Requisitos y pasos rápidos para ejecutar el proyecto.
- Cómo configurar la cadena de conexión.
- Scripts SQL incluidos (vistas y procedimientos) y cómo ejecutarlos.
- Mapeo de páginas a consultas/procedimientos y pasos de prueba.

---

## Requisitos mínimos
- .NET 8 SDK
- SQL Server con la base `Northwind`

## Ejecución rápida (PowerShell)
Desde la raíz del proyecto (`NorthwindWeb.csproj`):

```powershell
dotnet restore
dotnet build
dotnet run  # Opcional: dotnet watch run


##CONSULTAS()

Ventas por Empleado y Año

SELECT
    CONCAT(E.FirstName, ' ', E.LastName) AS Empleado,
    YEAR(O.OrderDate) AS Año,
    SUM(OD.UnitPrice * OD.Quantity * (1 - OD.Discount)) AS VentaTotal
FROM Employees E
INNER JOIN Orders O
    ON E.EmployeeID = O.EmployeeID
INNER JOIN [Order Details] OD
    ON O.OrderID = OD.OrderID
GROUP BY
    CONCAT(E.FirstName, ' ', E.LastName),
    YEAR(O.OrderDate)
ORDER BY
    Empleado,
    Año;

Ventas totales por cliente por mes (año+mes)
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
ORDER BY Anio DESC, Mes DESC, TotalCompradoMes DESC;


Promedio de valor de pedido por cliente por año (usa AVG):
WITH PedidoTotales AS (
    SELECT
        o.OrderID,
        o.CustomerID,
        YEAR(o.OrderDate) AS Anio,
        SUM(od.UnitPrice * od.Quantity * (1 - od.Discount)) AS TotalPorPedido
    FROM Orders o
    JOIN [Order Details] od ON o.OrderID = od.OrderID
    GROUP BY o.OrderID, o.CustomerID, YEAR(o.OrderDate)
)
SELECT
    c.CustomerID,
    c.CompanyName,
    p.Anio,
    AVG(p.TotalPorPedido) AS PromedioPorPedido,
    SUM(p.TotalPorPedido) AS TotalAnual
FROM PedidoTotales p
JOIN Customers c ON p.CustomerID = c.CustomerID
GROUP BY c.CustomerID, c.CompanyName, p.Anio
ORDER BY p.Anio DESC, TotalAnual DESC;


Ventas totales por cliente por año:
SELECT 
    c.CustomerID,
    c.CompanyName,
    YEAR(o.OrderDate) AS Anio,
    SUM(od.UnitPrice * od.Quantity * (1 - od.Discount)) AS TotalComprado
FROM Customers c
JOIN Orders o        ON c.CustomerID = o.CustomerID
JOIN [Order Details] od ON o.OrderID = od.OrderID
GROUP BY c.CustomerID, c.CompanyName, YEAR(o.OrderDate)
ORDER BY Anio DESC, TotalComprado DESC;

###############################################################################
##VISTAS ()
    Vista_PedidosDetalles_TotalLinea

IF OBJECT_ID('Vista_PedidosDetalles_TotalLinea', 'V') IS NOT NULL
    DROP VIEW Vista_PedidosDetalles_TotalLinea;
GO

CREATE VIEW Vista_PedidosDetalles_TotalLinea
AS
SELECT
    OD.OrderID,
    OD.ProductID,
    P.ProductName,
    OD.UnitPrice,
    OD.Quantity,
    OD.Discount,
    (OD.UnitPrice * OD.Quantity * (1 - OD.Discount)) AS TotalLinea
FROM 
    [Order Details] AS OD
INNER JOIN 
    Products AS P ON OD.ProductID = P.ProductID;
GO
###############################################################################

    Vista_VentasTotales_PorCliente

CREATE OR ALTER VIEW dbo.Vista_VentasTotales_PorCliente
AS
SELECT 
    C.CustomerID,
    C.CompanyName,
    C.Country,
    CAST(SUM(OD.UnitPrice * OD.Quantity * (1 - OD.Discount)) AS DECIMAL(18,2)) AS TotalVenta
FROM Customers C
JOIN Orders O ON C.CustomerID = O.CustomerID
JOIN [Order Details] OD ON O.OrderID = OD.OrderID
GROUP BY C.CustomerID, C.CompanyName, C.Country;
GO
###############################################################################
## PROCESOS ALMACENADOS (SP)
    SP_RegistrarNuevoPedidoCompleto

CREATE PROCEDURE SP_RegistrarNuevoPedidoCompleto  
        @CustomerID NCHAR(5),  
    @EmployeeID INT,  
    @RequiredDate DATETIME,  
    @ShipVia INT,  
    @Freight MONEY,  
    @ShipName NVARCHAR(40),  
    @ShipAddress NVARCHAR(60),  
    @ShipCity NVARCHAR(15),  
    @ShipRegion NVARCHAR(15) = NULL,  
    @ShipPostalCode NVARCHAR(10) = NULL,  
    @ShipCountry NVARCHAR(15),  
        @ProductID INT,  
    @Quantity SMALLINT,  
    @Discount REAL  
AS  
BEGIN  
    SET NOCOUNT ON;  
      
    DECLARE @NewOrderID INT;  
      
      
    BEGIN TRANSACTION  
      
    BEGIN TRY  
          
       
        INSERT INTO Orders (CustomerID, EmployeeID, OrderDate, RequiredDate, ShipVia, Freight, ShipName, ShipAddress, ShipCity, ShipRegion, ShipPostalCode, ShipCountry)  
        VALUES (@CustomerID, @EmployeeID, GETDATE(), @RequiredDate, @ShipVia, @Freight, @ShipName, @ShipAddress, @ShipCity, @ShipRegion, @ShipPostalCode, @ShipCountry);  
          
                SET @NewOrderID = SCOPE_IDENTITY();  
          
                      INSERT INTO [Order Details] (OrderID, ProductID, UnitPrice, Quantity, Discount)  
        SELECT   
            @NewOrderID,   
            @ProductID,   
            UnitPrice,             @Quantity,   
            @Discount  
        FROM Products WHERE ProductID = @ProductID;  
          
                  
               SELECT @NewOrderID AS NewOrderID;  
          
    END TRY  
    BEGIN CATCH   
        IF @@TRANCOUNT > 0  
            ROLLBACK TRANSACTION;  
              
               THROW;  
        RETURN -1; -- Retorna un error  
    END CATCH  
END

###############################################################################
    SP_ObtenerHistorialPedidos

IF OBJECT_ID('SP_ObtenerHistorialPedidos', 'P') IS NOT NULL
    DROP PROCEDURE SP_ObtenerHistorialPedidos;
GO

CREATE PROCEDURE SP_ObtenerHistorialPedidos
    @CustomerID NCHAR(5)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        O.OrderID, 
        O.OrderDate,
        ISNULL(SUM(OD.UnitPrice * OD.Quantity * (1 - OD.Discount)), 0) AS Total 
    FROM 
        Orders AS O
    LEFT JOIN 
        [Order Details] AS OD ON O.OrderID = OD.OrderID
    WHERE 
        O.CustomerID = @CustomerID
    GROUP BY 
        O.OrderID, O.OrderDate
    ORDER BY 
        O.OrderDate DESC;
END
GO

