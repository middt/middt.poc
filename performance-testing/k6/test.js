import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '30s', target: 20 }, // Ramp up to 20 users over 30 seconds
    { duration: '1m', target: 20 },  // Stay at 20 users for 1 minute
    { duration: '30s', target: 0 },  // Ramp down to 0 users
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'], // 95% of requests should be below 500ms
  },
};

// Get the test file name from the script
const testFileName = __ENV.K6_SCRIPT || 'test.js';

const BASE_URL = 'https://httpbin.org';

export default function () {
  // Add tags to include the test file name in metrics
  const tags = {
    testname: testFileName
  };

  // Success (200)
  const success = http.get(`${BASE_URL}/status/200`, { tags: { ...tags, endpoint: 'status-200' } });
  check(success, {
    'is status 200': (r) => r.status === 200,
  });

  // Client Error (404)
  const notFound = http.get(`${BASE_URL}/status/404`, { tags: { ...tags, endpoint: 'status-404' } });
  check(notFound, {
    'is status 404': (r) => r.status === 404,
  });

  // Server Error (500)
  const serverError = http.get(`${BASE_URL}/status/500`, { tags: { ...tags, endpoint: 'status-500' } });
  check(serverError, {
    'is status 500': (r) => r.status === 500,
  });

  // Redirect (301)
  const redirect = http.get(`${BASE_URL}/status/301`, { tags: { ...tags, endpoint: 'status-301' } });
  check(redirect, {
    'is status 301': (r) => r.status === 301,
  });

  sleep(1);
}
