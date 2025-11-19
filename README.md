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
dotnet run  # Opcional: dotnet watch run
