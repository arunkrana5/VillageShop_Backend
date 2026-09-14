# Use official .NET 8.0 SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/VillageShop.Api/VillageShop.Api.csproj", "src/VillageShop.Api/"]
COPY ["src/VillageShop.Application/VillageShop.Application.csproj", "src/VillageShop.Application/"]
COPY ["src/VillageShop.Common/VillageShop.Common.csproj", "src/VillageShop.Common/"]
COPY ["src/VillageShop.Domain/VillageShop.Domain.csproj", "src/VillageShop.Domain/"]
COPY ["src/VillageShop.Infrastructure/VillageShop.Infrastructure.csproj", "src/VillageShop.Infrastructure/"]

RUN dotnet restore "src/VillageShop.Api/VillageShop.Api.csproj"

# Copy full source and publish
COPY . .
WORKDIR "/src/src/VillageShop.Api"
RUN dotnet publish "VillageShop.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Environment configuration
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "VillageShop.Api.dll"]
