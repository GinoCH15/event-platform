FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/sdk:9.0-bookworm-slim AS build
WORKDIR /src
COPY ["NotificationService.csproj", "./"]
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release --no-restore -o /app/publish

FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NotificationService.dll"]
