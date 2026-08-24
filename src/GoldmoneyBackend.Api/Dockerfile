FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["GoldmoneyBackend.Api/GoldmoneyBackend.Api.csproj", "GoldmoneyBackend.Api/"]
COPY ["GoldmoneyBackend.Application/GoldmoneyBackend.Application.csproj", "GoldmoneyBackend.Application/"]
COPY ["GoldmoneyBackend.Infrastructure/GoldmoneyBackend.Infrastructure.csproj", "GoldmoneyBackend.Infrastructure/"]
COPY ["GoldmoneyBackend.Domain/GoldmoneyBackend.Domain.csproj", "GoldmoneyBackend.Domain/"]
RUN dotnet restore "GoldmoneyBackend.Api/GoldmoneyBackend.Api.csproj"

COPY . .
WORKDIR /src/GoldmoneyBackend.Api
RUN dotnet publish "GoldmoneyBackend.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "GoldmoneyBackend.Api.dll"]
