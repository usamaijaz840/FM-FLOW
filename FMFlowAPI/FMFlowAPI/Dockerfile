# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["FMFlowAPI/FMFlow.AccessValidation/FMFlow.AccessValidation.csproj", "FMFlowAPI/FMFlow.AccessValidation/"]
COPY ["FMFlowAPI/FMFlow.Admin/FMFlow.Admin.Interface.csproj", "FMFlowAPI/FMFlow.Admin/"]
COPY ["FMFlowAPI/FMFlow.Admin.Service/FMFlow.Admin.Service.csproj", "FMFlowAPI/FMFlow.Admin.Service/"]
COPY ["FMFlowAPI/FMFlow.Common/FMFlow.Common.csproj", "FMFlowAPI/FMFlow.Common/"]
COPY ["FMFlowAPI/FMFlow.Customers.Interface/FMFlow.Customers.Interface.csproj", "FMFlowAPI/FMFlow.Customers.Interface/"]
COPY ["FMFlowAPI/FMFlow.Customers.Service/FMFlow.Customers.Service.csproj", "FMFlowAPI/FMFlow.Customers.Service/"]
COPY ["FMFlowAPI/FMFlow.Data/FMFlow.Data.csproj", "FMFlowAPI/FMFlow.Data/"]
COPY ["FMFlowAPI/FMFlow.Email.Interface/FMFlow.Email.Interface.csproj", "FMFlowAPI/FMFlow.Email.Interface/"]
COPY ["FMFlowAPI/FMFlow.Email.Service/FMFlow.Email.Service.csproj", "FMFlowAPI/FMFlow.Email.Service/"]
COPY ["FMFlowAPI/FMFlow.Employees.Interface/FMFlow.Employees.Interface.csproj", "FMFlowAPI/FMFlow.Employees.Interface/"]
COPY ["FMFlowAPI/FMFlow.Employees.Service/FMFlow.Employees.Service.csproj", "FMFlowAPI/FMFlow.Employees.Service/"]
COPY ["FMFlowAPI/FMFlow.Entities/FMFlow.Entities.csproj", "FMFlowAPI/FMFlow.Entities/"]
COPY ["FMFlowAPI/FMFlow.Estimates.Interface/FMFlow.Estimates.Interface.csproj", "FMFlowAPI/FMFlow.Estimates.Interface/"]
COPY ["FMFlowAPI/FMFlow.Estimates.Service/FMFlow.Estimates.Service.csproj", "FMFlowAPI/FMFlow.Estimates.Service/"]
COPY ["FMFlowAPI/FMFlow.Events.Interface/FMFlow.Events.Interface.csproj", "FMFlowAPI/FMFlow.Events.Interface/"]
COPY ["FMFlowAPI/FMFlow.Events.Service/FMFlow.Events.Service.csproj", "FMFlowAPI/FMFlow.Events.Service/"]
COPY ["FMFlowAPI/FMFlow.Files.Interface/FMFlow.Files.Interface.csproj", "FMFlowAPI/FMFlow.Files.Interface/"]
COPY ["FMFlowAPI/FMFlow.Files.Service/FMFlow.Files.Service.csproj", "FMFlowAPI/FMFlow.Files.Service/"]
COPY ["FMFlowAPI/FMFlow.FlowAPI.Interface/FMFlow.FlowAPI.Interface.csproj", "FMFlowAPI/FMFlow.FlowAPI.Interface/"]
COPY ["FMFlowAPI/FMFlow.Identity/FMFlow.Identity.Interface.csproj", "FMFlowAPI/FMFlow.Identity/"]
COPY ["FMFlowAPI/FMFlow.Identity.Service/FMFlow.Identity.Service.csproj", "FMFlowAPI/FMFlow.Identity.Service/"]
COPY ["FMFlowAPI/FMFlow.Integrations.Interface/FMFlow.Integrations.Interface.csproj", "FMFlowAPI/FMFlow.Integrations.Interface/"]
COPY ["FMFlowAPI/FMFlow.Integrations.MxMerchant.Interface/FMFlow.Integrations.MxMerchant.Interface.csproj", "FMFlowAPI/FMFlow.Integrations.MxMerchant.Interface/"]
COPY ["FMFlowAPI/FMFlow.Integrations.Service/FMFlow.Integrations.Service.csproj", "FMFlowAPI/FMFlow.Integrations.Service/"]
COPY ["FMFlowAPI/FMFlow.Leads.Interface/FMFlow.Leads.Interface.csproj", "FMFlowAPI/FMFlow.Leads.Interface/"]
COPY ["FMFlowAPI/FMFlow.Leads.Service/FMFlow.Leads.Service.csproj", "FMFlowAPI/FMFlow.Leads.Service/"]
COPY ["FMFlowAPI/FMFlow.LeadTimelines.Interface/FMFlow.LeadTimelines.Interface.csproj", "FMFlow.LeadTimelines.Interface/"]
COPY ["FMFlowAPI/FMFlow.LeadTimelines.Service/FMFlow.LeadTimelines.Service.csproj", "FMFlow.LeadTimelines.Service/"]
COPY ["FMFlowAPI/FMFlow.Login/FMFlow.Login.Interface.csproj", "FMFlowAPI/FMFlow.Login/"]
COPY ["FMFlowAPI/FMFlow.Login.Service/FMFlow.Login.Service.csproj", "FMFlowAPI/FMFlow.Login.Service/"]
COPY ["FMFlowAPI/FMFlow.Pro.Interface/FMFlow.Pro.Interface.csproj", "FMFlowAPI/FMFlow.Pro.Interface/"]
COPY ["FMFlowAPI/FMFlow.Pro.Service/FMFlow.Pro.Service.csproj", "FMFlowAPI/FMFlow.Pro.Service/"]
COPY ["FMFlowAPI/FMFlow.Projects.Interface/FMFlow.Projects.Interface.csproj", "FMFlowAPI/FMFlow.Projects.Interface/"]
COPY ["FMFlowAPI/FMFlow.Projects.Service/FMFlow.Projects.Service.csproj", "FMFlowAPI/FMFlow.Projects.Service/"]
COPY ["FMFlowAPI/FMFlow.Payment/FMFlow.ProPayments.Interface.csproj", "FMFlowAPI/FMFlow.Payment/"]
COPY ["FMFlowAPI/FMFlow.Payment.Service/FMFlow.ProPayments.Service.csproj", "FMFlowAPI/FMFlow.Payment.Service/"]
COPY ["FMFlowAPI/FMFlow.ProUser/FMFlow.ProUser.Interface.csproj", "FMFlowAPI/FMFlow.ProUser/"]
COPY ["FMFlowAPI/FMFlow.ProUser.Service/FMFlow.ProUser.Service.csproj", "FMFlowAPI/FMFlow.ProUser.Service/"]
COPY ["FMFlowAPI/FMFlow.Referral/FMFlow.Referral.Interface.csproj", "FMFlowAPI/FMFlow.Referral/"]
COPY ["Aspire/FMFlow.ServiceDefaults/FMFlow.ServiceDefaults.csproj", "Aspire/FMFlow.ServiceDefaults/"]
COPY ["FMFlowAPI/FMFlow.Transactions.Interface/FMFlow.Transactions.Interface.csproj", "FMFlow.Transactions.Interface/"]
COPY ["FMFlowAPI/FMFlow.Transactions.Service/FMFlow.Transactions.Service.csproj", "FMFlowWeb/FMFlow.Transactions.Service/"]
COPY ["FMFlowAPI/FMFlowAPI/FMFlowAPI.csproj", "FMFlowAPI/FMFlowAPI/"]
RUN dotnet restore -v diag "./FMFlowAPI/FMFlowAPI/FMFlowAPI.csproj"
COPY . .
WORKDIR "/src/FMFlowAPI/FMFlowAPI"
RUN dotnet build "./FMFlowAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./FMFlowAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

#----------- CUSTOMIZATION STARTS HERE -------------
# Copy the PostgresSeeder files to the final image
COPY ["FMFlowAPI/FMFlow.Data/PostgresSeeder/*", "."]
#------------ CUSTOMIZATION ENDS HERE --------------

ENTRYPOINT ["dotnet", "FMFlowAPI.dll"]
