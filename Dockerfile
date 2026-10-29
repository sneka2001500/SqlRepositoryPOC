
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["SqlRepositoryPOC.csproj", "./"]
RUN dotnet restore "SqlRepositoryPOC.csproj"

COPY . .
RUN dotnet publish "SqlRepositoryPOC.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=10000
EXPOSE 10000
ENTRYPOINT ["dotnet", "SqlRepositoryPOC.dll"]
