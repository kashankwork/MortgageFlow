FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY MortgageFlow.sln global.json ./
COPY src/MortgageFlow.Domain/MortgageFlow.Domain.csproj src/MortgageFlow.Domain/
COPY src/MortgageFlow.Application/MortgageFlow.Application.csproj src/MortgageFlow.Application/
COPY src/MortgageFlow.Infrastructure/MortgageFlow.Infrastructure.csproj src/MortgageFlow.Infrastructure/
COPY src/MortgageFlow.Api/MortgageFlow.Api.csproj src/MortgageFlow.Api/
RUN dotnet restore src/MortgageFlow.Api/MortgageFlow.Api.csproj

COPY src/ src/
RUN dotnet publish src/MortgageFlow.Api/MortgageFlow.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "MortgageFlow.Api.dll"]
