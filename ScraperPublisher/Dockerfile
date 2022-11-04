#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ScraperPublisher/ScraperPublisher.csproj", "ScraperPublisher/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "ScraperPublisher/ScraperPublisher.csproj"
COPY . .
WORKDIR "/src/ScraperPublisher"
RUN dotnet build "ScraperPublisher.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ScraperPublisher.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ScraperPublisher.dll"]