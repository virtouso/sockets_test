#FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
#USER $APP_UID
#WORKDIR /app
#
#FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
#ARG BUILD_CONFIGURATION=Release
#WORKDIR /src
#COPY ["Server-DotNetCore/Server-DotNetCore.csproj", "Server-DotNetCore/"]
#RUN dotnet restore "Server-DotNetCore/Server-DotNetCore.csproj"
#COPY . .
#WORKDIR "/src/Server-DotNetCore"
#RUN dotnet build "./Server-DotNetCore.csproj" -c $BUILD_CONFIGURATION -o /app/build
#
#FROM build AS publish
#ARG BUILD_CONFIGURATION=Release
#RUN dotnet publish "./Server-DotNetCore.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false
#
#FROM base AS final
#WORKDIR /app
#COPY --from=publish /app/publish .
#ENTRYPOINT ["dotnet", "Server-DotNetCore.dll"]



FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app
EXPOSE 8080
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Server-DotNetCore/Server-DotNetCore.csproj", "Server-DotNetCore/"]
RUN dotnet restore "Server-DotNetCore/Server-DotNetCore.csproj"
COPY . .
WORKDIR "/src/Server-DotNetCore"
RUN dotnet build "Server-DotNetCore.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Server-DotNetCore.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Server-DotNetCore.dll"]