#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Scraper/Scraper.csproj", "Scraper/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "Scraper/Scraper.csproj"
COPY . .
WORKDIR "/src/Scraper"
RUN dotnet build "Scraper.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Scraper.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Scraper.dll"]