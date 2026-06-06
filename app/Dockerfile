# Build context is the repository root (see azure.yaml) so the image can include
# both the app source and the reference use-case data.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY app/ ./app/
RUN dotnet publish app/UsecaseCoach.Mcp.csproj -c Release -o /publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /publish ./
COPY data/reference-usecases/ ./data/reference-usecases/
ENV REFERENCE_USECASES_DIR=/app/data/reference-usecases \
    ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "UsecaseCoach.Mcp.dll"]
