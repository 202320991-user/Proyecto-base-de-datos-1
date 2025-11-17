# Resumen del avance

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
dotnet run  
# Opcional: dotnet watch run
```

La aplicación quedará accesible en `http://localhost:5000` y `https://localhost:5001` (puede variar).

## Configurar la conexión a la BD
Editar `appsettings.json` (o usar `appsettings.Development.json`) y definir `ConnectionStrings:NorthwindConn`.

Ejemplos:

```text
Server=localhost\SQLEXPRESS;Database=Northwind;Trusted_Connection=True;TrustServerCertificate=True;
Server=mi_servidor;Database=Northwind;User Id=mi_usuario;Password=mi_contraseña;TrustServerCertificate=True;
```

---

## Scripts SQL incluidos
Se incluyen las vistas y procedimientos usados por la aplicación. Copiar cada bloque en SSMS o guardar como archivos `.sql` en `db-scripts/`.

- Vista `Vista_PedidosDetalles_TotalLinea`: detalle de líneas y cálculo `TotalLinea`.

```sql
IF OBJECT_ID('Vista_PedidosDetalles_TotalLinea', 'V') IS NOT NULL
    DROP VIEW Vista_PedidosDetalles_TotalLinea;
GO
CREATE VIEW Vista_PedidosDetalles_TotalLinea AS
SELECT OD.OrderID, OD.ProductID, P.ProductName, OD.UnitPrice, OD.Quantity, OD.Discount,
       (OD.UnitPrice * OD.Quantity * (1 - OD.Discount)) AS TotalLinea
FROM [Order Details] AS OD
INNER JOIN Products AS P ON OD.ProductID = P.ProductID;
GO
```

- Vista `Vista_VentasTotales_PorCliente`: suma de ventas por cliente.

```sql
IF OBJECT_ID('Vista_VentasTotales_PorCliente', 'V') IS NOT NULL
    DROP VIEW Vista_VentasTotales_PorCliente;
GO
CREATE VIEW Vista_VentasTotales_PorCliente AS
SELECT C.CustomerID, C.CompanyName, C.Country,
       CAST(SUM(OD.UnitPrice * OD.Quantity * (1 - OD.Discount)) AS DECIMAL(10,2)) AS VentaTotal
FROM Customers C
JOIN Orders O ON C.CustomerID = O.CustomerID
JOIN [Order Details] OD ON O.OrderID = OD.OrderID
GROUP BY C.CustomerID, C.CompanyName, C.Country;
GO
```

- Procedimiento `SP_ObtenerHistorialPedidos`: retorna historial de pedidos por cliente.

```sql
CREATE PROCEDURE SP_ObtenerHistorialPedidos @CustomerID NCHAR(5) AS
BEGIN
  SET NOCOUNT ON;
  SELECT O.OrderID, O.OrderDate, O.RequiredDate, O.ShippedDate, O.Freight, O.ShipCountry
  FROM Orders O
  WHERE O.CustomerID = @CustomerID
  ORDER BY O.OrderDate DESC;
END
GO
```

- Procedimiento `SP_RegistrarNuevoPedidoCompleto`: inserta `Orders` y `Order Details` en una transacción y devuelve `NewOrderID`.

```sql
IF OBJECT_ID('SP_RegistrarNuevoPedidoCompleto', 'P') IS NOT NULL
  DROP PROCEDURE SP_RegistrarNuevoPedidoCompleto;
GO
CREATE PROCEDURE SP_RegistrarNuevoPedidoCompleto
  @CustomerID NCHAR(5), @EmployeeID INT, @RequiredDate DATETIME,
  @ShipVia INT, @Freight MONEY, @ShipName NVARCHAR(40), @ShipAddress NVARCHAR(60),
  @ShipCity NVARCHAR(15), @ShipRegion NVARCHAR(15)=NULL, @ShipPostalCode NVARCHAR(10)=NULL,
  @ShipCountry NVARCHAR(15), @ProductID INT, @Quantity SMALLINT, @Discount REAL
AS
BEGIN
  SET NOCOUNT ON;
  DECLARE @NewOrderID INT;
  BEGIN TRANSACTION;
  BEGIN TRY
    INSERT INTO Orders (CustomerID, EmployeeID, OrderDate, RequiredDate, ShipVia, Freight, ShipName, ShipAddress, ShipCity, ShipRegion, ShipPostalCode, ShipCountry)
    VALUES (@CustomerID, @EmployeeID, GETDATE(), @RequiredDate, @ShipVia, @Freight, @ShipName, @ShipAddress, @ShipCity, @ShipRegion, @ShipPostalCode, @ShipCountry);
    SET @NewOrderID = SCOPE_IDENTITY();
    INSERT INTO [Order Details] (OrderID, ProductID, UnitPrice, Quantity, Discount)
    SELECT @NewOrderID, @ProductID, UnitPrice, @Quantity, @Discount FROM Products WHERE ProductID = @ProductID;
    COMMIT TRANSACTION;
    SELECT @NewOrderID AS NewOrderID;
  END TRY
  BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
  END CATCH
END
GO
```

---

## Mapeo página → consulta / procedimiento
- `/Customers` — lista clientes (usar para obtener `CustomerID` válidos: `ALFKI`, `VINET`, ...).
- `/Orders/CreateOrderSP` — formulario que llama a `SP_RegistrarNuevoPedidoCompleto`.
- `/Reports/HistorialCliente` — ejecuta `SP_ObtenerHistorialPedidos`.
- `/Reports/DetallesDePedido?OrderID=<id>` — consulta la vista `Vista_PedidosDetalles_TotalLinea`.
- `/Orders/Delete/<id>` — eliminación de pedido con manejo de integridad.
- `/Reports/VentasPorEmpleado` — consulta agregada para ventas por empleado y año.

## Cómo probar los scripts (PowerShell / sqlcmd)
Guarda cada script como `db-scripts\<nombre>.sql` y ejecuta:

```powershell
sqlcmd -S "localhost\SQLEXPRESS" -d Northwind -i db-scripts\Vista_PedidosDetalles_TotalLinea.sql
sqlcmd -S "localhost\SQLEXPRESS" -d Northwind -i db-scripts\Vista_VentasTotales_PorCliente.sql
sqlcmd -S "localhost\SQLEXPRESS" -d Northwind -i db-scripts\SP_ObtenerHistorialPedidos.sql
sqlcmd -S "localhost\SQLEXPRESS" -d Northwind -i db-scripts\SP_RegistrarNuevoPedidoCompleto.sql
```

O ejecutar los `SELECT`/`EXEC` de prueba en SSMS.

## Notas y recomendaciones
- Usar parámetros en código (`SqlCommand.Parameters`) para evitar inyección SQL.
- Verificar existencia de `CustomerID`, `ProductID`, `EmployeeID` antes de llamar al SP.
- En producción, gestionar las cadenas de conexión mediante secretos o un gestor seguro.

---

Fecha: 17 de noviembre de 2025
```sql
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
  -- Cálculo del total de la línea
  (OD.UnitPrice * OD.Quantity * (1 - OD.Discount)) AS TotalLinea
FROM 
  [Order Details] AS OD
INNER JOIN 
  Products AS P ON OD.ProductID = P.ProductID;
GO

-- Prueba ejemplo (opcional):
-- SELECT * FROM Vista_PedidosDetalles_TotalLinea WHERE OrderID = 10248;
```

2) Vista: `Vista_VentasTotales_PorCliente` (suma ventas por cliente)

```sql
IF OBJECT_ID('Vista_VentasTotales_PorCliente', 'V') IS NOT NULL
  DROP VIEW Vista_VentasTotales_PorCliente;
GO

CREATE VIEW Vista_VentasTotales_PorCliente
AS
SELECT
  C.CustomerID,
  C.CompanyName,
  C.Country,
  CAST(SUM(OD.UnitPrice * OD.Quantity * (1 - OD.Discount)) AS DECIMAL(10, 2)) AS VentaTotal
FROM 
  Customers AS C
JOIN 
  Orders AS O ON C.CustomerID = O.CustomerID
JOIN 
  [Order Details] AS OD ON O.OrderID = OD.OrderID
GROUP BY 
  C.CustomerID, C.CompanyName, C.Country;
GO

-- Prueba ejemplo (opcional):
-- SELECT * FROM Vista_VentasTotales_PorCliente ORDER BY VentaTotal DESC;
```

3) Procedimiento: `SP_ObtenerHistorialPedidos` (retorna historial de pedidos de un cliente)

```sql
CREATE PROCEDURE SP_ObtenerHistorialPedidos
  @CustomerID NCHAR(5)
AS
BEGIN
  SET NOCOUNT ON;
    
  SELECT
    O.OrderID,
    O.OrderDate,
    O.RequiredDate,
    O.ShippedDate,
    O.Freight,
    O.ShipCountry
  FROM
    Orders AS O
  WHERE
    O.CustomerID = @CustomerID
  ORDER BY
    O.OrderDate DESC;
END
GO

-- Prueba ejemplo (opcional):
-- EXEC SP_ObtenerHistorialPedidos 'VINET';
```

4) Procedimiento: `SP_RegistrarNuevoPedidoCompleto` (inserta Orders y Order Details en una transacción)

```sql
IF OBJECT_ID('SP_RegistrarNuevoPedidoCompleto', 'P') IS NOT NULL
  DROP PROCEDURE SP_RegistrarNuevoPedidoCompleto;
GO

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
    
  BEGIN TRANSACTION;
    
  BEGIN TRY
    INSERT INTO Orders (CustomerID, EmployeeID, OrderDate, RequiredDate, ShipVia, Freight, ShipName, ShipAddress, ShipCity, ShipRegion, ShipPostalCode, ShipCountry)
    VALUES (@CustomerID, @EmployeeID, GETDATE(), @RequiredDate, @ShipVia, @Freight, @ShipName, @ShipAddress, @ShipCity, @ShipRegion, @ShipPostalCode, @ShipCountry);

    SET @NewOrderID = SCOPE_IDENTITY();

    INSERT INTO [Order Details] (OrderID, ProductID, UnitPrice, Quantity, Discount)
    SELECT 
      @NewOrderID,
      @ProductID,
      UnitPrice,
      @Quantity,
      @Discount
    FROM Products 
    WHERE ProductID = @ProductID;

    COMMIT TRANSACTION;
        
    SELECT @NewOrderID AS NewOrderID;
  END TRY
  BEGIN CATCH
    IF @@TRANCOUNT > 0
      ROLLBACK TRANSACTION;

    THROW;
    RETURN -1;
  END CATCH
END
GO
```

**Mapeo página → consulta / procedimiento**

- `/Customers` : Interfaz web para listar clientes (usar para obtener `CustomerID` válidos, por ejemplo `ALFKI`, `VINET`).
- `/Orders/CreateOrderSP` : Formulario que llama a `SP_RegistrarNuevoPedidoCompleto` para crear un pedido completo (encabezado + detalle).
- `/Reports/HistorialCliente` : Formulario que ejecuta `SP_ObtenerHistorialPedidos` y muestra el historial de un cliente.
- `/Reports/DetallesDePedido?OrderID=<id>` : Consulta la vista `Vista_PedidosDetalles_TotalLinea` para mostrar detalles y `TotalLinea` de un pedido.
- `/Orders/Delete/<id>` : Operación de eliminación de pedido (DELETE) con manejo de integridad y transacción.
- `/Reports/VentasPorEmpleado` : Ejecuta la consulta agregada (JOIN + SUM + GROUP BY) para listar ventas por empleado y año.

**Ejecución de pruebas y recomendaciones**

- Ejecutar los scripts de creación de vistas y procedimientos en SQL Server Management Studio (SSMS) o mediante `sqlcmd`.

Ejemplo con `sqlcmd` (PowerShell):

```powershell
sqlcmd -S "localhost\SQLEXPRESS" -d Northwind -i db-scripts\Vista_PedidosDetalles_TotalLinea.sql
sqlcmd -S "localhost\SQLEXPRESS" -d Northwind -i db-scripts\Vista_VentasTotales_PorCliente.sql
sqlcmd -S "localhost\SQLEXPRESS" -d Northwind -i db-scripts\SP_ObtenerHistorialPedidos.sql
sqlcmd -S "localhost\SQLEXPRESS" -d Northwind -i db-scripts\SP_RegistrarNuevoPedidoCompleto.sql
```

- Para probar manualmente en SSMS, ejecutar los bloques `EXEC` o `SELECT` de prueba incluidos junto a cada script.

**Notas de seguridad y operación**

- Las consultas que aceptan input del usuario (por ejemplo `CustomerID`) usan parámetros en el código C# (`SqlCommand.Parameters.AddWithValue`) para prevenir SQL Injection.
- Antes de ejecutar `SP_RegistrarNuevoPedidoCompleto`, verifica que existan los `CustomerID`, `ProductID` y `EmployeeID` referenciados.
- En entornos de producción, no incluir cadenas de conexión en texto plano; usar secretos o gestores de configuración.

Si quieres, creo los archivos SQL dentro de `db-scripts/` con los scripts arriba mostrados y los agrego al repositorio.
