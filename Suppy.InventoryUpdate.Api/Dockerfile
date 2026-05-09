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
COPY ["Suppy.InventoryUpdate.Api/Suppy.InventoryUpdate.Api.csproj", "Suppy.InventoryUpdate.Api/"]
COPY ["Suppy.InventoryUpdate.Api.Abstractions/Suppy.InventoryUpdate.Api.Abstractions.csproj", "Suppy.InventoryUpdate.Api.Abstractions/"]
COPY ["Suppy.InventoryUpdate.Api.Application/Suppy.InventoryUpdate.Api.Application.csproj", "Suppy.InventoryUpdate.Api.Application/"]
COPY ["Suppy.InventoryUpdate.Api.Domain/Suppy.InventoryUpdate.Api.Domain.csproj", "Suppy.InventoryUpdate.Api.Domain/"]
COPY ["Suppy.InventoryUpdate.Api.GenericRepo/Suppy.InventoryUpdate.Api.GenericRepo.csproj", "Suppy.InventoryUpdate.Api.GenericRepo/"]
COPY ["Suppy.InventoryUpdate.Api.Persistence/Suppy.InventoryUpdate.Api.Persistence.csproj", "Suppy.InventoryUpdate.Api.Persistence/"]
RUN dotnet restore "./Suppy.InventoryUpdate.Api/Suppy.InventoryUpdate.Api.csproj"
COPY . .
WORKDIR "/src/Suppy.InventoryUpdate.Api"
RUN dotnet build "./Suppy.InventoryUpdate.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Suppy.InventoryUpdate.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Suppy.InventoryUpdate.Api.dll"]
