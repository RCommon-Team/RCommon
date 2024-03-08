# Setup Instructions
## Requirements
* Requires SQL Server Express or beter
* Requires EF Tools to be installed to dotnet CLI [https://docs.microsoft.com/en-us/ef/core/cli/dotnet](https://docs.microsoft.com/en-us/ef/core/cli/dotnet) include updates.

## Instructions
1. Right click on the HR.LeaveManagement.API project
2. Choose "Open in Terminal" from menu
3. Refer to Developer Powershell
4. Run commands below:
5. Right click solution and choose "Set Startup Projects" from menu.
6. Choose "HR.LeaveManagement.MVC" and "HR.LeaveManagement.API" from menu options.
7. Build and Start Solution
```csharp
dotnet ef database update --context LeaveManagementDbContext
dotnet ef database update --context LeaveManagementIdentityDbContext
```

