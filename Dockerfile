FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/WalletApi.Domain/WalletApi.Domain.csproj src/WalletApi.Domain/
COPY src/WalletApi.Application/WalletApi.Application.csproj src/WalletApi.Application/
COPY src/WalletApi.Infrastructure/WalletApi.Infrastructure.csproj src/WalletApi.Infrastructure/
COPY src/WalletApi.Api/WalletApi.Api.csproj src/WalletApi.Api/
RUN dotnet restore src/WalletApi.Api/WalletApi.Api.csproj

COPY src/ src/
RUN dotnet publish src/WalletApi.Api/WalletApi.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app .

ENTRYPOINT ["dotnet", "WalletApi.Api.dll"]
