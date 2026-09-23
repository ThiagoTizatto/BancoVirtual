FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props ./
COPY .editorconfig ./
COPY src/ ./src/
RUN dotnet restore src/MovimentacoesFinanceiras.Api/MovimentacoesFinanceiras.Api.csproj
RUN dotnet publish src/MovimentacoesFinanceiras.Api/MovimentacoesFinanceiras.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "MovimentacoesFinanceiras.Api.dll"]
