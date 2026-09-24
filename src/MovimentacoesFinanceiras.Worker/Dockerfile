FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props ./
COPY .editorconfig ./
COPY src/ ./src/
RUN dotnet restore src/MovimentacoesFinanceiras.Worker/MovimentacoesFinanceiras.Worker.csproj
RUN dotnet publish src/MovimentacoesFinanceiras.Worker/MovimentacoesFinanceiras.Worker.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MovimentacoesFinanceiras.Worker.dll"]
