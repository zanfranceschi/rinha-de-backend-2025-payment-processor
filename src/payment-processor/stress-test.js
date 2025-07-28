import { Httpx } from "https://jslib.k6.io/httpx/0.1.0/index.js";
import { uuidv4 } from "https://jslib.k6.io/k6-utils/1.4.0/index.js";
import { sleep, check } from "k6";

const MAX_REQUESTS = __ENV.MAX_REQUESTS ?? 550;
const TOKEN = __ENV.TOKEN ?? "123";
const TEST_DURATION = __ENV.TEST_DURATION ?? "60s";

export const options = {
    summaryTrendStats: [
        "p(90)",
        "p(95)",
        "p(98)",
        "p(99)",
        "count",
    ],
    thresholds: {
        http_req_duration: ['p(99) < 10'],
    },
    scenarios: {
        misc_endpoints: {
            startTime: "0s",
            duration: "1s",
            exec: "miscEndpoints",
            executor: "constant-vus",
            vus: 1,
        },
        instability_simulation: {
            startTime: "0s",
            duration: "1s",
            exec: "instabilitySimulation",
            executor: "constant-vus",
            vus: 1,
        },
        payments: {
            startTime: "3s",
            exec: "payment",
            executor: "ramping-vus",
            startVUs: 1,
            gracefulRampDown: "0s",
            stages: [{ target: MAX_REQUESTS, duration: TEST_DURATION }],
        },
        payments_summary: {
            startTime: "3s",
            duration: TEST_DURATION,
            exec: "paymentsSummary",
            executor: "constant-vus",
            vus: "1",
        },
    },
};

const httpClient = new Httpx({
    baseURL: 'http://localhost:8001',
    //baseURL: 'http://localhost:5245',
    headers: {
        'Content-Type': 'application/json',
        'X-Rinha-Token': TOKEN
    },
    timeout: 1500,
});

export async function setup() {
    const params = { headers: { 'X-Rinha-Token': "123" } };
    const payload = JSON.stringify({
        token: TOKEN
    });
    await httpClient.asyncPut('/admin/configurations/token', payload, params);
    await httpClient.asyncPost('/admin/purge-payments');
}

export async function miscEndpoints() {
    await httpClient.asyncPost('/admin/purge-payments');
    sleep(1);
}

export async function instabilitySimulation() {
    // delay
    const delayPayload01 = JSON.stringify({
        delay: 100
    });
    const delayResponse01 = await httpClient.asyncPut('/admin/configurations/delay', delayPayload01);
    check(delayResponse01, {
        "instability[delay] is ok": (r) => r.status === 200,
    });
    const delayPayload02 = JSON.stringify({
        delay: 0
    });
    const delayResponse02 = await httpClient.asyncPut('/admin/configurations/delay', delayPayload02);
    check(delayResponse02, {
        "instability[delay] is ok": (r) => r.status === 200,
    });

    // failure
    const failurePayload01 = JSON.stringify({
        failure: true
    });
    const failureResponse01 = await httpClient.asyncPut('/admin/configurations/failure', failurePayload01);
    
    const paymentPayload = {
        correlationId: uuidv4(),
        amount: (Math.random() * 10) * (Math.random() * 100),
        requestedAt: new Date().toISOString()
    };
    const paymentResponse = await httpClient.asyncPost('/payments', JSON.stringify(paymentPayload));
    check(failureResponse01, {
        "instability[failure] is ok": (r) => r.status === 200,
    });
    check(paymentResponse, {
        "instability[failure] is ok": (r) => r.status === 500
    });
    const failurePayload02 = JSON.stringify({
        failure: false
    });
    const failureResponse02 = await httpClient.asyncPut('/admin/configurations/failure', failurePayload02);
    check(failureResponse02, {
        "instability[failure] is ok": (r) => r.status === 200,
    });

    sleep(1);
}

export async function payment() {

    const amount = (Math.random() * 10) * (Math.random() * 100);
    const correlationId = uuidv4();
    const requestedAt = new Date().toISOString();

    const payload = {
        correlationId,
        amount,
        requestedAt
    };

    const response = await httpClient.asyncPost('/payments', JSON.stringify(payload));
    check(response, {
        "payment is ok": (r) => r.status === 200
    });

    sleep(1);
}

export async function paymentsSummary() {
    
    const now = new Date();
    const from = new Date(now - 1000 * 10).toISOString();
    const to = new Date(now - 100).toISOString();

    const response = await httpClient.asyncGet(`/admin/payments-summary?from=${from}&to=${to}`);

    check(response, {
        "payments summary is ok": (r) => r.status === 200
    });

    sleep(1);
}
