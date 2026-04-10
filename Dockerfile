# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/DevBrain.Api/DevBrain.Api.csproj", "src/DevBrain.Api/"]
COPY ["src/DevBrain.Domain/DevBrain.Domain.csproj", "src/DevBrain.Domain/"]
COPY ["src/DevBrain.Infrastructure/DevBrain.Infrastructure.csproj", "src/DevBrain.Infrastructure/"]
RUN dotnet restore "src/DevBrain.Api/DevBrain.Api.csproj"

COPY . .
WORKDIR "/src/src/DevBrain.Api"
RUN dotnet build "DevBrain.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "DevBrain.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DevBrain.Api.dll"]
