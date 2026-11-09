FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY SolarGrid.csproj ./
RUN dotnet restore SolarGrid.csproj
COPY . ./
RUN dotnet publish SolarGrid.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=10000
EXPOSE 10000
COPY --from=build /app/publish ./
USER app
ENTRYPOINT ["dotnet", "SolarGrid.dll"]
