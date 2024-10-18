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

# Install LLamaSharp runtime dependencies
RUN apt update && apt install -y libgomp1 musl libsnappy1v5 libjemalloc2 && apt clean
RUN ln -s /usr/lib/x86_64-linux-musl/libc.so /lib/libc.musl-x86_64.so.1
RUN ln -s /usr/lib/x86_64-linux-gnu/libjemalloc.so.2 /usr/lib/x86_64-linux-gnu/libjemalloc.so.1

COPY --from=build-env /usr/src/app/out .
ENTRYPOINT ["./BingusApi"]
