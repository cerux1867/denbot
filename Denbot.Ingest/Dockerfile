FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["Denbot.Ingest/Denbot.Ingest.csproj", "Denbot.Ingest/"]
RUN dotnet restore "Denbot.Ingest/Denbot.Ingest.csproj"
COPY . .
WORKDIR "/src/Denbot.Ingest"
RUN dotnet build "Denbot.Ingest.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Denbot.Ingest.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Denbot.Ingest.dll"]
