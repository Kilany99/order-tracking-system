FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["DriverService.API/DriverService.API.csproj", "DriverService.API/"]
RUN dotnet restore "DriverService.API/DriverService.API.csproj"
COPY . .
RUN dotnet build "DriverService.API/DriverService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DriverService.API/DriverService.API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DriverService.API.dll"]