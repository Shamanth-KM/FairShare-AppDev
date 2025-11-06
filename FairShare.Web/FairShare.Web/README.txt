FairShare.Web (.NET 8, EF Core, SQL Server)

1) Open folder in Visual Studio (File > Open > Project/Solution, select FairShare.Web.csproj).
2) Build once to restore packages.
3) Tools > NuGet Package Manager > Package Manager Console, then run:
     Add-Migration InitialCreate
     Update-Database
   (or use CLI: dotnet ef migrations add InitialCreate && dotnet ef database update)
4) Run the project. Browse Home, Users, Groups, Expenses. API under /api/*.
Connection: appsettings.json -> DefaultConnection (MANIDEEP\SQLEXPRESS).
