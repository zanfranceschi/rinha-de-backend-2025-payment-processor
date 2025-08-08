FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build

# AOT
RUN apk add clang build-base zlib-dev

WORKDIR /app
COPY . ./
RUN dotnet restore
RUN dotnet publish -c Release --property PublishDir=out

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine
WORKDIR /app
COPY --from=build /app/out .
# AOT
ENTRYPOINT ["./payment-processor"]

# JIT
# ENTRYPOINT ["dotnet", "payment-processor.dll"]
