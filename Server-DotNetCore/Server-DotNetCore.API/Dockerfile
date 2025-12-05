FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Server-DotNetCore.API/Server-DotNetCore.API.csproj", "Server-DotNetCore.API/"]
RUN dotnet restore "Server-DotNetCore.API/Server-DotNetCore.API.csproj"
COPY . .
WORKDIR "/src/Server-DotNetCore.API"
RUN dotnet build "Server-DotNetCore.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Server-DotNetCore.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Server-DotNetCore.API.dll"]

