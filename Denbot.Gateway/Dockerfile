FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["Denbot.Gateway/Denbot.Gateway.csproj", "Denbot.Gateway/"]
RUN dotnet restore "Denbot.Gateway/Denbot.Gateway.csproj"
COPY . .
WORKDIR "/src/Denbot.Gateway"
RUN dotnet build "Denbot.Gateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Denbot.Gateway.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Denbot.Gateway.dll"]
