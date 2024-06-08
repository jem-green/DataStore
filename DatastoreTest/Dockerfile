#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM --platform=linux/arm64 mcr.microsoft.com/dotnet/sdk:6.0 as base

WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["DatastoreTest/DatastoreTest.csproj", "DatastoreTest/"]
COPY ["DatastoreLibrary/DatastoreLibrary.csproj", "DatastoreLibrary/"]
COPY ["DatastoreTerminal/DatastoreTerminal.csproj", "DatastoreTerminal/"]
RUN dotnet restore "./DatastoreTest/DatastoreTest.csproj"
COPY . .
WORKDIR "/src/DatastoreTest"
RUN dotnet build "./DatastoreTest.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./DatastoreTest.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DatastoreTest.dll"]