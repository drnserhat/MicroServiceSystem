// Baseline load profile for the gateway. Run it before and after a performance change so the effect is
// a measured number instead of an opinion.
//
//   docker compose -f deploy/docker/docker-compose.yml \
//     -f deploy/docker/docker-compose.apps.yml \
//     -f deploy/docker/docker-compose.resources.yml up -d
//
//   k6 run -e BASE_URL=http://localhost:8080 -e TOKEN=<jwt> deploy/perf/gateway-load.js
//
// While it runs, capture the runtime side of the picture from the host:
//   dotnet-counters monitor --process-id <pid> \
//     System.Runtime Microsoft.AspNetCore.Hosting Microsoft.AspNetCore.Server.Kestrel
//
// The numbers worth writing down are allocation rate, gen0 collections per second, working set and
// p95 latency. Memory work should move the first three; queue work shows up in consumer throughput.

import http from 'k6/http';
import { check } from 'k6';

const baseUrl = __ENV.BASE_URL || 'http://localhost:8080';
const token = __ENV.TOKEN || '';

export const options = {
  scenarios: {
    // Establishes a steady baseline rather than chasing a peak number.
    steady: {
      executor: 'constant-arrival-rate',
      rate: Number(__ENV.RPS || 200),
      timeUnit: '1s',
      duration: __ENV.DURATION || '2m',
      preAllocatedVUs: 50,
      maxVUs: 500,
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<250', 'p(99)<500'],
  },
};

const params = {
  headers: token
    ? { Authorization: `Bearer ${token}`, Accept: 'application/json' }
    : { Accept: 'application/json' },
};

export default function () {
  const response = http.get(`${baseUrl}/api/v1/countries`, params);

  check(response, {
    'status is 200': (r) => r.status === 200,
  });
}
