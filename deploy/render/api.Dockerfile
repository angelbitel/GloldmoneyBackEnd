FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/GoldmoneyBackend.Api/GoldmoneyBackend.Api.csproj", "src/GoldmoneyBackend.Api/"]
COPY ["src/GoldmoneyBackend.Application/GoldmoneyBackend.Application.csproj", "src/GoldmoneyBackend.Application/"]
COPY ["src/GoldmoneyBackend.Infrastructure/GoldmoneyBackend.Infrastructure.csproj", "src/GoldmoneyBackend.Infrastructure/"]
COPY ["src/GoldmoneyBackend.Domain/GoldmoneyBackend.Domain.csproj", "src/GoldmoneyBackend.Domain/"]
RUN dotnet restore "src/GoldmoneyBackend.Api/GoldmoneyBackend.Api.csproj"

COPY . .
WORKDIR /src/src/GoldmoneyBackend.Api
RUN dotnet publish "GoldmoneyBackend.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "GoldmoneyBackend.Api.dll"]
