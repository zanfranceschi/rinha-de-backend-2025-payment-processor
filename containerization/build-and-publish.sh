#!/usr/bin/env bash

timestamp=$(date '+%Y%m%d%H%M%S')

docker buildx build --platform linux/amd64 \
    -t zanfranceschi/payment-processor:amd64-$timestamp ../src/payment-processor 
docker push zanfranceschi/payment-processor:amd64-$timestamp

docker buildx build --platform linux/arm64 \
    -t zanfranceschi/payment-processor:arm64-$timestamp ../src/payment-processor
docker push zanfranceschi/payment-processor:arm64-$timestamp
