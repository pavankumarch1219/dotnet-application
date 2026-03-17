FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copy full repo (IMPORTANT)
COPY . .

# Go to project folder
WORKDIR /src/src/Catalog.API

RUN dotnet publish Catalog.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Catalog.API.dll"]
