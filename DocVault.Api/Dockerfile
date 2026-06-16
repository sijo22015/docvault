FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
RUN mkdir -p /app/storage

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["DocVault.Api/DocVault.Api.csproj", "DocVault.Api/"]
COPY ["DocVault.Application/DocVault.Application.csproj", "DocVault.Application/"]
COPY ["DocVault.Domain/DocVault.Domain.csproj", "DocVault.Domain/"]
COPY ["DocVault.Infrastructure/DocVault.Infrastructure.csproj", "DocVault.Infrastructure/"]
RUN dotnet restore "DocVault.Api/DocVault.Api.csproj"
COPY . .
RUN dotnet build "DocVault.Api/DocVault.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DocVault.Api/DocVault.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DocVault.Api.dll"]
