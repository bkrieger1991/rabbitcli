#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["src/RabbitMQ.CLI.Proxy/RabbitMQ.CLI.Proxy.csproj", "src/RabbitMQ.CLI.Proxy/"]
COPY ["src/RabbitMQ.Library/RabbitMQ.Library.csproj", "src/RabbitMQ.Library/"]
RUN dotnet restore "src/RabbitMQ.CLI.Proxy/RabbitMQ.CLI.Proxy.csproj"
RUN dotnet restore "src/RabbitMQ.Library/RabbitMQ.Library.csproj"
COPY . .
WORKDIR "/src/src/RabbitMQ.CLI.Proxy"
RUN dotnet build "RabbitMQ.CLI.Proxy.csproj" --no-restore -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RabbitMQ.CLI.Proxy.csproj" --no-build -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RabbitMQ.CLI.Proxy.dll"]