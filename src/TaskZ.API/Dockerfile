#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/TaskZ.API/TaskZ.API.csproj", "src/TaskZ.API/"]
COPY ["src/TaskZ.Core/TaskZ.Core.csproj", "src/TaskZ.Core/"]
COPY ["src/TaskZ.Infrastructure/TaskZ.Infrastructure.csproj", "src/TaskZ.Infrastructure/"]
RUN dotnet restore "./src/TaskZ.API/TaskZ.API.csproj"
COPY . .
WORKDIR "/src/src/TaskZ.API"
RUN dotnet build "./TaskZ.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TaskZ.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TaskZ.API.dll"]