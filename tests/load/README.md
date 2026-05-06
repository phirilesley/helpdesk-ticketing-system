# Load Test Scripts

## Omnichannel Smoke Load

Run with k6:

```bash
k6 run tests/load/omnichannel-smoke.js -e BASE_URL=http://localhost:5229
```

Purpose:
- Exercise inbound omnichannel webhook throughput path.
- Validate p95 latency and error-rate thresholds under concurrent traffic.

Notes:
- This is a starter suite for CI/CD and staging runs.
- For production parity, extend with sustained duration, mixed channel payloads, and queue-depth assertions.
