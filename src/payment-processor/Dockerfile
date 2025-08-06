FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build

RUN apk add clang build-base zlib-dev libc6-compat

WORKDIR /app
COPY . ./
RUN dotnet restore
RUN dotnet publish -c Release --property PublishDir=out

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["./payment-processor"]
# ENTRYPOINT ["dotnet", "payment-processor.dll"]
