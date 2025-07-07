#!/usr/bin/env bash

docker buildx build --platform linux/amd64 \
    -t zanfranceschi/payment-processor:amd64 ../src/payment-processor 
docker push zanfranceschi/payment-processor:amd64

docker buildx build --platform linux/arm64 \
    -t zanfranceschi/payment-processor:arm64 ../src/payment-processor
docker push zanfranceschi/payment-processor:arm64
