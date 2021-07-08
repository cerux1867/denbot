FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["Denbot.API/Denbot.API.csproj", "Denbot.API/"]
RUN dotnet restore "Denbot.API/Denbot.API.csproj"
COPY . .
WORKDIR "/src/Denbot.API"
RUN dotnet build "Denbot.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Denbot.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Denbot.API.dll"]
