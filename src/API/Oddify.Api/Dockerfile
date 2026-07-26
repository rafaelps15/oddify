FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["src/API/Oddify.Api/Oddify.Api.csproj", "src/API/Oddify.Api/"]
COPY ["src/Common/Oddify.Common.Application/Oddify.Common.Application.csproj", "src/Common/Oddify.Common.Application/"]
COPY ["src/Common/Oddify.Common.Domain/Oddify.Common.Domain.csproj", "src/Common/Oddify.Common.Domain/"]
COPY ["src/Common/Oddify.Common.Infrastructure/Oddify.Common.Infrastructure.csproj", "src/Common/Oddify.Common.Infrastructure/"]
COPY ["src/Common/Oddify.Common.Presentation/Oddify.Common.Presentation.csproj", "src/Common/Oddify.Common.Presentation/"]
COPY ["src/Modules/Fixtures/Oddify.Modules.Fixtures.Domain/Oddify.Modules.Fixtures.Domain.csproj", "src/Modules/Fixtures/Oddify.Modules.Fixtures.Domain/"]
COPY ["src/Modules/Fixtures/Oddify.Modules.Fixtures.Application/Oddify.Modules.Fixtures.Application.csproj", "src/Modules/Fixtures/Oddify.Modules.Fixtures.Application/"]
COPY ["src/Modules/Fixtures/Oddify.Modules.Fixtures.Infrastructure/Oddify.Modules.Fixtures.Infrastructure.csproj", "src/Modules/Fixtures/Oddify.Modules.Fixtures.Infrastructure/"]
COPY ["src/Modules/Fixtures/Oddify.Modules.Fixtures.Presentation/Oddify.Modules.Fixtures.Presentation.csproj", "src/Modules/Fixtures/Oddify.Modules.Fixtures.Presentation/"]
COPY ["src/Modules/Fixtures/Oddify.Modules.Fixtures.PublicApi/Oddify.Modules.Fixtures.PublicApi.csproj", "src/Modules/Fixtures/Oddify.Modules.Fixtures.PublicApi/"]
COPY ["src/Modules/Fixtures/Oddify.Modules.Fixtures.IntegrationEvents/Oddify.Modules.Fixtures.IntegrationEvents.csproj", "src/Modules/Fixtures/Oddify.Modules.Fixtures.IntegrationEvents/"]
COPY ["src/Modules/Analise/Oddify.Modules.Analise.Domain/Oddify.Modules.Analise.Domain.csproj", "src/Modules/Analise/Oddify.Modules.Analise.Domain/"]
COPY ["src/Modules/Analise/Oddify.Modules.Analise.Application/Oddify.Modules.Analise.Application.csproj", "src/Modules/Analise/Oddify.Modules.Analise.Application/"]
COPY ["src/Modules/Analise/Oddify.Modules.Analise.Infrastructure/Oddify.Modules.Analise.Infrastructure.csproj", "src/Modules/Analise/Oddify.Modules.Analise.Infrastructure/"]
COPY ["src/Modules/Analise/Oddify.Modules.Analise.Presentation/Oddify.Modules.Analise.Presentation.csproj", "src/Modules/Analise/Oddify.Modules.Analise.Presentation/"]
COPY ["src/Modules/Analise/Oddify.Modules.Analise.PublicApi/Oddify.Modules.Analise.PublicApi.csproj", "src/Modules/Analise/Oddify.Modules.Analise.PublicApi/"]
COPY ["src/Modules/Analise/Oddify.Modules.Analise.IntegrationEvents/Oddify.Modules.Analise.IntegrationEvents.csproj", "src/Modules/Analise/Oddify.Modules.Analise.IntegrationEvents/"]
COPY ["src/Modules/Apostas/Oddify.Modules.Apostas.Domain/Oddify.Modules.Apostas.Domain.csproj", "src/Modules/Apostas/Oddify.Modules.Apostas.Domain/"]
COPY ["src/Modules/Apostas/Oddify.Modules.Apostas.Application/Oddify.Modules.Apostas.Application.csproj", "src/Modules/Apostas/Oddify.Modules.Apostas.Application/"]
COPY ["src/Modules/Apostas/Oddify.Modules.Apostas.Infrastructure/Oddify.Modules.Apostas.Infrastructure.csproj", "src/Modules/Apostas/Oddify.Modules.Apostas.Infrastructure/"]
COPY ["src/Modules/Apostas/Oddify.Modules.Apostas.Presentation/Oddify.Modules.Apostas.Presentation.csproj", "src/Modules/Apostas/Oddify.Modules.Apostas.Presentation/"]
COPY ["src/Modules/Apostas/Oddify.Modules.Apostas.PublicApi/Oddify.Modules.Apostas.PublicApi.csproj", "src/Modules/Apostas/Oddify.Modules.Apostas.PublicApi/"]
COPY ["src/Modules/Apostas/Oddify.Modules.Apostas.IntegrationEvents/Oddify.Modules.Apostas.IntegrationEvents.csproj", "src/Modules/Apostas/Oddify.Modules.Apostas.IntegrationEvents/"]
RUN dotnet restore "./src/API/Oddify.Api/Oddify.Api.csproj"
COPY . .
WORKDIR "/src/src/API/Oddify.Api"
RUN dotnet build "./Oddify.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Oddify.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Oddify.Api.dll"]
