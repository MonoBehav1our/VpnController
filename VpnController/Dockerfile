FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY VpnController/VpnController.csproj VpnController/
RUN dotnet restore VpnController/VpnController.csproj
COPY VpnController/ VpnController/
WORKDIR /src/VpnController
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 5080
ENTRYPOINT ["dotnet", "VpnController.dll"]
