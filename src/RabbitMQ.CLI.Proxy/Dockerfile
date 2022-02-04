FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build

### Build
WORKDIR /src
COPY ["src/RabbitMQ.CLI.Proxy/RabbitMQ.CLI.Proxy.csproj", "src/RabbitMQ.CLI.Proxy/"]
COPY ["src/RabbitMQ.CLI.Proxy.Shared/RabbitMQ.CLI.Proxy.Shared.csproj", "src/RabbitMQ.CLI.Proxy.Shared/"]
COPY ["src/RabbitMQ.Library/RabbitMQ.Library.csproj", "src/RabbitMQ.Library/"]
RUN dotnet restore "src/RabbitMQ.Library/RabbitMQ.Library.csproj"
RUN dotnet restore "src/RabbitMQ.CLI.Proxy.Shared/RabbitMQ.CLI.Proxy.Shared.csproj"
RUN dotnet restore "src/RabbitMQ.CLI.Proxy/RabbitMQ.CLI.Proxy.csproj"
COPY . .
WORKDIR "/src/src/RabbitMQ.CLI.Proxy"
RUN dotnet build --no-restore -c Release
RUN dotnet publish --no-restore --no-build -c Release -o /app/publish

### Run
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5000
ENTRYPOINT ["dotnet", "rabbitcliproxy.dll"]