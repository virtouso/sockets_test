FROM reg-pub.pegah.tech/mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY DopPanelAdmin.csproj .

RUN dotnet restore BlazorClient.csproj
COPY . .
RUN dotnet build BlazorClient.csproj -c Release -o /app/build

FROM build AS publish
RUN dotnet publish BlazorClient.csproj -c Release -o /app/publish

FROM reg-pub.pegah.tech/docker.io/library/nginx:alpine AS final
WORKDIR /usr/share/nginx/html
COPY --from=publish /app/publish/wwwroot .

COPY  nginx.conf /etc/nginx/nginx.conf