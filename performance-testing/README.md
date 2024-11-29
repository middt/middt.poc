# Performance Testing Framework

This project provides a comprehensive performance testing framework that combines k6 for load testing, Grafana for visualization, and Postman collections for API testing.

## Project Purpose

This framework is designed to:

- Execute performance tests using k6
- Visualize performance metrics in real-time using Grafana dashboards
- Maintain and run API tests using Postman collections
- Provide a containerized environment for consistent test execution

## Converting Postman to k6

You can convert your Postman collections to k6 scripts using the [postman-to-k6](https://github.com/apideck-libraries/postman-to-k6) tool:

1. Install the converter:
   ```bash
   npm install -g @apideck/postman-to-k6
   ```

2. Convert your Postman collection:
   ```bash
   postman-to-k6 collection.json -o k6-script.js
   ```

3. The generated script can be used directly with k6:
   ```bash
   docker-compose run k6 run /scripts/k6-script.js
   ```

Note: The converter supports most Postman features including variables, prerequest scripts, and tests. Check the [documentation](https://github.com/apideck-libraries/postman-to-k6#supported-postman-features) for details.

## Project Structure

```
performance-testing/
├── docker-compose.yml    # Docker composition for the testing environment
├── grafana/             # Grafana configuration and dashboards
├── k6/                  # k6 test scripts and configurations
├── postman/             # Postman collections and environments
└── run.txt             # Runtime instructions and notes
```

## Prerequisites

- Docker and Docker Compose
- k6 (for local test execution)
- Postman (for collection management)

## Setup and Running Instructions

### Initial Setup

1. Clone this repository
2. Ensure Docker and Docker Compose are installed on your system

### Running Tests and Viewing Results

1. Start all services:

   ```bash
   docker-compose up -d
   ```

2. Wait about 30 seconds for all services to initialize

3. Access Grafana:

   - Open your browser and go to: http://localhost:3000
   - Default credentials: admin/admin

4. Run k6 test:

   ```bash
   docker-compose run k6 run /scripts/test.js
   ```

5. Run Postman collections via Newman:
   ```bash
   docker-compose run   newman run /collections/test.json \
    --reporter-cli-no-assertions \
    --iteration-count 10 \
    --delay-request 1000 \
    -r cli,influxdb
   ```

### Viewing Results in Grafana

- The dashboard will automatically show up in Grafana
- You'll see two main panels:
  - HTTP Request Duration: Shows response times
  - Virtual Users: Shows the number of concurrent users
- The dashboard auto-refreshes every 5 seconds

### Important Notes

- Test results are stored in InfluxDB
- Grafana is pre-configured with the InfluxDB data source
- The dashboard shows the last 15 minutes of data
- You can adjust the time range in Grafana's top-right corner

### Troubleshooting and Maintenance

If you encounter issues or need to reset the environment:

1. Clean up and remove all containers:

   ```bash
   docker-compose down --volumes --remove-orphans
   ```

2. Rebuild without cache:

   ```bash
   docker-compose build --no-cache
   ```

3. Start services again:
   ```bash
   docker-compose up -d
   ```

## Monitoring

- Grafana Dashboard: http://localhost:3000
- Real-time metrics and visualization available during test execution

## Example Commands

Here are some example commands for running specific test collections:

### Running Payment Invoice Tests
```bash
docker-compose run newman run /collections/fatura-odeme.json \
    --reporter-cli-no-assertions \
    --iteration-count 10 \
    --delay-request 1000 \
    -r cli,influxdb
```

### Running Money Transfer Tests
```bash
docker-compose run newman run /collections/para-tranfer.json \
    --reporter-cli-no-assertions \
    --iteration-count 10 \
    --delay-request 1000 \
    -r cli,influxdb
```

### Running k6 Tests
```bash
docker-compose run k6 run /scripts/test.js
```

## Additional Resources

- k6 Documentation: https://k6.io/docs/
- Grafana Documentation: https://grafana.com/docs/
- Postman Documentation: https://learning.postman.com/
