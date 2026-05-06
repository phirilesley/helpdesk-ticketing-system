import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  vus: 20,
  duration: '60s',
  thresholds: {
    http_req_failed: ['rate<0.02'],
    http_req_duration: ['p(95)<1200'],
  },
};

const baseUrl = __ENV.BASE_URL || 'http://localhost:5229';

export default function () {
  const payload = JSON.stringify({
    provider: 'load-test',
    externalMessageId: `k6-${__VU}-${__ITER}`,
    externalConversationId: `conv-${__VU}`,
    senderAddress: 'load@test.local',
    subject: 'Load Test',
    content: 'Synthetic inbound message'
  });

  const params = {
    headers: {
      'Content-Type': 'application/json',
      'X-Channel-Signature': 'test-signature'
    },
  };

  const res = http.post(`${baseUrl}/api/omnichannel/inbound/1/webhook`, payload, params);
  check(res, {
    'status is 200/202/401/403': (r) => [200, 202, 401, 403].includes(r.status),
  });

  sleep(0.2);
}
