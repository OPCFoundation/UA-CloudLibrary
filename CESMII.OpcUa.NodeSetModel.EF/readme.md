Install tools into project:
```
cd ProfileDesigner\api\CESMIINodeSetUtilities
dotnet tool install --global dotnet-ef
dotnet tool update --global dotnet-ef
```
Create migration/database schema
```
dotnet ef migrations add InitialCreate --context NodeSetModelContext
dotnet ef database update --context NodeSetModelContext
```

Recreate database:
```
del .\Migrations\*
dotnet ef migrations add InitialCreate --context NodeSetModelContext
dotnet ef database drop --context NodeSetModelContext
dotnet ef database update --context NodeSetModelContext
```
