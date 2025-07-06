#!/usr/bin/env bash

docker build -t zanfranceschi/payment-processor ../src/payment-processor
docker push zanfranceschi/payment-processor
