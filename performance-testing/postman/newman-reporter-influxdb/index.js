const Influx = require('influx');

class NewmanInfluxDBReporter {
  constructor(emitter, reporterOptions, options) {
    this.influx = new Influx.InfluxDB({
      host: process.env.INFLUXDB_HOST || 'influxdb',
      port: process.env.INFLUXDB_PORT || 8086,
      database: process.env.INFLUXDB_DATABASE || 'newman',
      username: process.env.INFLUXDB_USER || 'admin',
      password: process.env.INFLUXDB_PASSWORD || 'admin'
    });

    // Store iteration results
    this.iterationResults = new Map();

    // Initialize database if it doesn't exist
    this.influx.getDatabaseNames()
      .then(names => {
        if (!names.includes(process.env.INFLUXDB_DATABASE || 'newman')) {
          return this.influx.createDatabase(process.env.INFLUXDB_DATABASE || 'newman');
        }
      })
      .then(() => {
        console.log('InfluxDB connection and database verified');
      })
      .catch(err => {
        console.error('Error initializing InfluxDB:', err);
      });

    emitter.on('beforeItem', (err, args) => {
      if (err) {
        console.error('Error before item:', err);
        return;
      }
      
      const { cursor, item } = args;
      const iterationKey = `${options.collection.name}_${cursor.iteration}`;
      
      // Initialize iteration results if not exists
      if (!this.iterationResults.has(iterationKey)) {
        console.log(`Initializing results for iteration ${cursor.iteration}`);
        this.iterationResults.set(iterationKey, {
          total_requests: 0,
          failed_requests: 0,
          success_requests: 0,
          has_failures: 0,
          iteration_number: parseInt(cursor.iteration) || 0,
          timestamp: new Date()
        });
      }
    });

    emitter.on('request', (err, args) => {
      if (err) {
        console.error('Error during request:', err);
        return;
      }

      const { cursor, item, response } = args;
      const timestamp = new Date();
      const isSuccess = response.code < 400;
      const iterationKey = `${options.collection.name}_${cursor.iteration}`;

      console.log(`Processing request for iteration ${cursor.iteration}, status: ${isSuccess ? 'success' : 'failed'}`);

      // Update iteration results
      const currentResults = this.iterationResults.get(iterationKey);
      if (currentResults) {
        currentResults.total_requests++;
        if (isSuccess) {
          currentResults.success_requests++;
        } else {
          currentResults.failed_requests++;
          currentResults.has_failures = 1;
        }
        currentResults.timestamp = timestamp;
        
        console.log(`Updated results for iteration ${cursor.iteration}:`, currentResults);
      }

      // Individual request point
      const point = {
        measurement: 'postman_results',
        tags: {
          collection_name: options.collection.name,
          request_name: item.name,
          request_method: item.request.method,
          request_url: typeof item.request.url === 'string' ? item.request.url : item.request.url.toString(),
          status_code: response.code ? response.code.toString() : 'unknown',
          test_status: isSuccess ? 'pass' : 'fail',
          iteration: cursor.iteration.toString()
        },
        fields: {
          response_time: response.responseTime,
          response_code: parseInt(response.code) || 0,
          response_status: response.status,
          failed: isSuccess ? 0 : 1,
          succeeded: isSuccess ? 1 : 0,
          total_tests: item.tests ? item.tests.length : 0,
          failed_tests: 0,
          skipped_tests: 0,
          iteration_number: parseInt(cursor.iteration) || 0
        },
        timestamp
      };

      this.influx.writePoints([point])
        .catch(err => {
          console.error('Error writing request point to InfluxDB:', err);
        });
    });

    emitter.on('beforeDone', (err, args) => {
      if (err) {
        console.error('Error before done:', err);
        return;
      }

      // Write any remaining iteration results
      for (const [iterationKey, results] of this.iterationResults.entries()) {
        const [collectionName, iteration] = iterationKey.split('_');
        
        console.log(`Writing final results for iteration ${iteration}:`, results);

        const summaryPoint = {
          measurement: 'collection_results',
          tags: {
            collection_name: collectionName,
            iteration: iteration
          },
          fields: {
            total_requests: results.total_requests,
            failed_requests: results.failed_requests,
            success_requests: results.success_requests,
            iteration_number: results.iteration_number,
            has_failures: results.has_failures
          },
          timestamp: results.timestamp
        };

        this.influx.writePoints([summaryPoint])
          .then(() => {
            console.log(`Successfully wrote results for iteration ${iteration}`);
          })
          .catch(err => {
            console.error(`Error writing iteration ${iteration} summary to InfluxDB:`, err);
          });
      }

      // Clear all iteration results
      this.iterationResults.clear();
    });

    emitter.on('done', (err, summary) => {
      if (err) {
        console.error('Collection run encountered an error:', err);
      }

      // Write summary data
      const point = {
        measurement: 'postman_summary',
        tags: {
          collection_name: options.collection.name
        },
        fields: {
          total_requests: summary.run.stats.requests.total,
          failed_requests: summary.run.stats.requests.failed,
          total_assertions: summary.run.stats.assertions.total,
          failed_assertions: summary.run.stats.assertions.failed
        },
        timestamp: new Date()
      };

      this.influx.writePoints([point])
        .catch(err => {
          console.error('Error writing summary to InfluxDB:', err);
        });

      console.log('Collection run completed.');
    });
  }
}

module.exports = NewmanInfluxDBReporter;