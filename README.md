# Rinha de Backend - Terceira Edição - 2025

## Payment Processor

Este repositório contém o código dos Payment Processors usado na [terceira edição da Rinha de Backend](https://github.com/zanfranceschi/rinha-de-backend-2025).


### Teste de Stress

```shell
cd ./src/payment-processor/containerization

docker compose up

cd ../

k6 run -e TEST_DURATION=120s -e MAX_REQUESTS=700 -e TOKEN=123 stress-test.js
```

