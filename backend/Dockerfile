FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY *.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app /p:AssemblyName=api

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
# Root yerine imajin hazir non-root kullanicisi ile calis (least privilege)
USER app
ENTRYPOINT ["dotnet", "api.dll"]