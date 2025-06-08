FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["LabCore-Gateway/LabCore-Gateway.csproj", "LabCore-Gateway/"]
RUN dotnet restore "LabCore-Gateway/LabCore-Gateway.csproj"
COPY . .
WORKDIR "/src/LabCore-Gateway"
RUN dotnet build "./LabCore-Gateway.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./LabCore-Gateway.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LabCore-Gateway.dll"]
