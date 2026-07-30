# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["src/API/Oddify.Api/Oddify.Api.csproj", "src/API/Oddify.Api/"]
COPY ["src/Modules/Fixtures/Oddify.Modules.Fixtures.Infrastructure/Oddify.Modules.Fixtures.Infrastructure.csproj", "src/Modules/Fixtures/Oddify.Modules.Fixtures.Infrastructure/"]
COPY ["src/Common/Oddify.Common.Infrastructure/Oddify.Common.Infrastructure.csproj", "src/Common/Oddify.Common.Infrastructure/"]
COPY ["src/Common/Oddify.Common.Application/Oddify.Common.Application.csproj", "src/Common/Oddify.Common.Application/"]
COPY ["src/Common/Oddify.Common.Domain/Oddify.Common.Domain.csproj", "src/Common/Oddify.Common.Domain/"]
COPY ["src/Modules/Fixtures/Oddify.Modules.Fixtures.Application/Oddify.Modules.Fixtures.Application.csproj", "src/Modules/Fixtures/Oddify.Modules.Fixtures.Application/"]
COPY ["src/Modules/Fixtures/Oddify.Modules.Fixtures.Domain/Oddify.Modules.Fixtures.Domain.csproj", "src/Modules/Fixtures/Oddify.Modules.Fixtures.Domain/"]
COPY ["src/Modules/Fixtures/Oddify.Modules.Fixtures.IntegrationEvents/Oddify.Modules.Fixtures.IntegrationEvents.csproj", "src/Modules/Fixtures/Oddify.Modules.Fixtures.IntegrationEvents/"]
COPY ["src/Modules/Fixtures/Oddify.Modules.Fixtures.Presentation/Oddify.Modules.Fixtures.Presentation.csproj", "src/Modules/Fixtures/Oddify.Modules.Fixtures.Presentation/"]
COPY ["src/Common/Oddify.Common.Presentation/Oddify.Common.Presentation.csproj", "src/Common/Oddify.Common.Presentation/"]
COPY ["src/Modules/Fixtures/Oddify.Modules.Fixtures.PublicApi/Oddify.Modules.Fixtures.PublicApi.csproj", "src/Modules/Fixtures/Oddify.Modules.Fixtures.PublicApi/"]
COPY ["src/Modules/Analise/Oddify.Modules.Analise.Infrastructure/Oddify.Modules.Analise.Infrastructure.csproj", "src/Modules/Analise/Oddify.Modules.Analise.Infrastructure/"]
COPY ["src/Modules/Analise/Oddify.Modules.Analise.Application/Oddify.Modules.Analise.Application.csproj", "src/Modules/Analise/Oddify.Modules.Analise.Application/"]
COPY ["src/Modules/Analise/Oddify.Modules.Analise.Domain/Oddify.Modules.Analise.Domain.csproj", "src/Modules/Analise/Oddify.Modules.Analise.Domain/"]
COPY ["src/Modules/Analise/Oddify.Modules.Analise.IntegrationEvents/Oddify.Modules.Analise.IntegrationEvents.csproj", "src/Modules/Analise/Oddify.Modules.Analise.IntegrationEvents/"]
COPY ["src/Modules/Analise/Oddify.Modules.Analise.Presentation/Oddify.Modules.Analise.Presentation.csproj", "src/Modules/Analise/Oddify.Modules.Analise.Presentation/"]
COPY ["src/Modules/Analise/Oddify.Modules.Analise.PublicApi/Oddify.Modules.Analise.PublicApi.csproj", "src/Modules/Analise/Oddify.Modules.Analise.PublicApi/"]
COPY ["src/Modules/Apostas/Oddify.Modules.Apostas.Infrastructure/Oddify.Modules.Apostas.Infrastructure.csproj", "src/Modules/Apostas/Oddify.Modules.Apostas.Infrastructure/"]
COPY ["src/Modules/Apostas/Oddify.Modules.Apostas.Application/Oddify.Modules.Apostas.Application.csproj", "src/Modules/Apostas/Oddify.Modules.Apostas.Application/"]
COPY ["src/Modules/Apostas/Oddify.Modules.Apostas.Domain/Oddify.Modules.Apostas.Domain.csproj", "src/Modules/Apostas/Oddify.Modules.Apostas.Domain/"]
COPY ["src/Modules/Apostas/Oddify.Modules.Apostas.IntegrationEvents/Oddify.Modules.Apostas.IntegrationEvents.csproj", "src/Modules/Apostas/Oddify.Modules.Apostas.IntegrationEvents/"]
COPY ["src/Modules/Apostas/Oddify.Modules.Apostas.Presentation/Oddify.Modules.Apostas.Presentation.csproj", "src/Modules/Apostas/Oddify.Modules.Apostas.Presentation/"]
COPY ["src/Modules/Users/Oddify.Modules.Users.Infrastructure/Oddify.Modules.Users.Infrastructure.csproj", "src/Modules/Users/Oddify.Modules.Users.Infrastructure/"]
COPY ["src/Modules/Users/Oddify.Modules.Users.Application/Oddify.Modules.Users.Application.csproj", "src/Modules/Users/Oddify.Modules.Users.Application/"]
COPY ["src/Modules/Users/Oddify.Modules.Users.Domain/Oddify.Modules.Users.Domain.csproj", "src/Modules/Users/Oddify.Modules.Users.Domain/"]
COPY ["src/Modules/Users/Oddify.Modules.Users.IntegrationEvents/Oddify.Modules.Users.IntegrationEvents.csproj", "src/Modules/Users/Oddify.Modules.Users.IntegrationEvents/"]
COPY ["src/Modules/Users/Oddify.Modules.Users.Presentation/Oddify.Modules.Users.Presentation.csproj", "src/Modules/Users/Oddify.Modules.Users.Presentation/"]
COPY ["src/Modules/Users/Oddify.Modules.Users.PublicApi/Oddify.Modules.Users.PublicApi.csproj", "src/Modules/Users/Oddify.Modules.Users.PublicApi/"]
RUN dotnet restore "./src/API/Oddify.Api/Oddify.Api.csproj"
COPY . .
WORKDIR "/src/src/API/Oddify.Api"
RUN dotnet build "./Oddify.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Oddify.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Oddify.Api.dll"]