#!/usr/bin/env bash

timestamp=$(date '+%Y%m%d%H%M%S')

docker buildx build --platform linux/amd64 \
    -t zanfranceschi/payment-processor:amd64-$timestamp \
    -t payment-processor:amd64-$timestamp ../src/payment-processor 
docker push zanfranceschi/payment-processor:amd64-$timestamp

docker buildx build --platform linux/arm64 \
    -t zanfranceschi/payment-processor:arm64-$timestamp ../src/payment-processor
docker push zanfranceschi/payment-processor:arm64-$timestamp

echo "=============="
echo "     tag      "
echo "=============="
echo $timestamp
echo "=============="

# latest:
#   arm64-20250728011553 
#   amd64-20250728011553
