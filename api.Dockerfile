FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /usr/src/app

# Copy everything
COPY . .
# Restore as distinct layers
RUN dotnet restore BingusApi
# Build and publish a release
RUN dotnet publish BingusApi -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /usr/src/app
COPY --from=build-env /usr/src/app/out .
ENTRYPOINT ["./BingusApi"]
