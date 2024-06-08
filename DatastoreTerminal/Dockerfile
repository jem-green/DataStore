#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM --platform=linux/arm64 mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["DatastoreTerminal/DatastoreTerminal.csproj", "DatastoreTerminal/"]
COPY ["DatastoreLibrary/DatastoreLibrary.csproj", "DatastoreLibrary/"]
RUN dotnet restore "./DatastoreTerminal/DatastoreTerminal.csproj"
COPY . .
WORKDIR "/src/DatastoreTerminal"
RUN dotnet build "./DatastoreTerminal.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./DatastoreTerminal.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DatastoreTerminal.dll"]