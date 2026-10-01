# Build context is the repository root (docker-compose.yml): the client links the shared avatar artwork
# from Scoreboard.WebApp/Avatars, so that file must be inside the context.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Scoreboard.Client/Scoreboard.Client.csproj", "Scoreboard.Client/"]
COPY ["Scoreboard.WebApp/Avatars/animals.json", "Scoreboard.WebApp/Avatars/"]
RUN dotnet restore "Scoreboard.Client/Scoreboard.Client.csproj"
COPY Scoreboard.Client/ Scoreboard.Client/
WORKDIR "/src/Scoreboard.Client"
RUN dotnet build "./Scoreboard.Client.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Scoreboard.Client.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# The SQLite database, player photos and logs live in these folders; they are mounted as volumes
# (docker-compose.yml), so they must exist and belong to the unprivileged app user.
USER root
RUN mkdir -p wwwroot/Db wwwroot/PlayerSet logs \
    && chown -R $APP_UID:$APP_UID wwwroot/Db wwwroot/PlayerSet logs
USER $APP_UID

ENTRYPOINT ["dotnet", "Scoreboard.Client.dll"]
