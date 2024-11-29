# Distributed Cache with Redis

A .NET Core minimal API project that implements distributed caching using Redis. The project features configurable caching for specific endpoints using regex patterns.

## Features

- Redis-based distributed caching
- Regex pattern matching for endpoint caching
- Configurable cache duration per endpoint
- Support for various URL patterns
- Cache invalidation endpoints
- Pattern-based cache management
- Docker support with docker-compose

## Docker Deployment

### Prerequisites
- Docker
- Docker Compose

### Running with Docker Compose

1. Build and start the services:
```bash
docker-compose up -d --build
```

2. Check the services are running:
```bash
docker-compose ps
```

3. View logs:
```bash
# All services
docker-compose logs

# Specific service
docker-compose logs api
docker-compose logs redis
```

4. Stop the services:
```bash
docker-compose down
```

### Docker Configuration

The solution includes two services:

1. **API Service**:
   - Builds from the Dockerfile
   - Exposes ports 8080 (HTTP) and 8081 (HTTPS)
   - Configured to use Redis container
   - Environment variables set for development

2. **Redis Service**:
   - Uses official Redis image
   - Persistent storage with Docker volume
   - Append-only file enabled for durability
   - Exposed on default port 6379

### Docker Volumes

- `redis-data`: Persists Redis data across container restarts

### Networks

- `cache-network`: Isolated network for API and Redis communication

## Configuration

Cache settings are configured in `appsettings.json`:

```json
{
  "Redis": {
    "Configuration": "localhost:6379",
    "InstanceName": "DistributedCache_",
    "Endpoints": [
      {
        "PathPattern": "^/weatherforecast$",
        "TimeToLiveMinutes": 15
      }
    ]
  }
}
```

## Cache Management Endpoints

### 1. Invalidate Specific Cache Key
```http
DELETE /cache/{key}
```
Invalidates the cache for a specific key.

Example:
```bash
# Invalidate cache for weatherforecast
curl -X DELETE http://localhost:8080/cache/weatherforecast
```

### 2. Invalidate by Pattern
```http
DELETE /cache/pattern/{pattern}
```
Invalidates all cache entries matching the specified pattern.

Example:
```bash
# Invalidate all product-related caches
curl -X DELETE http://localhost:8080/cache/pattern/api/products
```

### 3. List Cache Keys
```http
GET /cache/keys/{pattern?}
```
Lists all cache keys, optionally filtered by a pattern.

Example:
```bash
# List all cache keys
curl http://localhost:8080/cache/keys

# List all product-related cache keys
curl http://localhost:8080/cache/keys/api/products
```

## Endpoint Pattern Types

### 1. Exact Matches (`^/path$`)

Matches only the exact path specified.

Examples for `^/weatherforecast$`:
- ✅ `/weatherforecast`
- ❌ `/weatherforecast/`
- ❌ `/weatherforecast/daily`
- ❌ `/api/weatherforecast`

### 2. Optional Parameters (`^/path(/\d+)?$`)

Matches a base path with an optional numeric parameter.

Examples for `^/api/products(/\d+)?$`:
- ✅ `/api/products`
- ✅ `/api/products/1`
- ✅ `/api/products/42`
- ❌ `/api/products/abc`
- ❌ `/api/products/1/details`
- ❌ `/api/products/`

### 3. Wildcards (`^/path/.*$`)

Matches a base path with any suffix.

Examples for `^/api/categories/.*$`:
- ✅ `/api/categories/1`
- ✅ `/api/categories/electronics`
- ✅ `/api/categories/1/products`
- ✅ `/api/categories/electronics/featured`
- ❌ `/api/categories` (missing trailing path)
- ❌ `/categories/1` (wrong prefix)

## More Pattern Examples

Here are some additional useful patterns:

```json
{
  "Endpoints": [
    {
      "PathPattern": "^/api/users/\\d+/profile$",
      "TimeToLiveMinutes": 30
    },
    {
      "PathPattern": "^/api/products/(\\d+)/reviews(/\\d+)?$",
      "TimeToLiveMinutes": 60
    },
    {
      "PathPattern": "^/api/search\\?.*$",
      "TimeToLiveMinutes": 15
    }
  ]
}
```

Examples explained:
- `^/api/users/\d+/profile$`
  - ✅ `/api/users/123/profile`
  - ❌ `/api/users/abc/profile`

- `^/api/products/(\d+)/reviews(/\d+)?$`
  - ✅ `/api/products/1/reviews`
  - ✅ `/api/products/1/reviews/5`
  - ❌ `/api/products/1/reviews/abc`

- `^/api/search\?.*$`
  - ✅ `/api/search?q=phone`
  - ✅ `/api/search?category=electronics&brand=samsung`
  - ❌ `/api/search/phones`

## Common Regex Components

- `^` - Start of the path
- `$` - End of the path
- `\d+` - One or more digits
- `.*` - Any characters (zero or more)
- `?` - Optional (zero or one)
- `|` - OR operator
- `()` - Grouping

## Usage Tips

1. Always start patterns with `^` and end with `$` to ensure full path matching
2. Use `\d+` for numeric parameters
3. Use `.*` carefully as it's very permissive
4. Test patterns thoroughly to ensure they match only intended endpoints

## Cache Management Tips

1. Use specific cache keys when possible to avoid unintended cache invalidation
2. When invalidating by pattern, test the pattern first using the `/cache/keys/{pattern}` endpoint
3. Monitor cache invalidation operations through application logs
4. Consider implementing cache warming after invalidation for critical endpoints

## Getting Started

### Local Development
1. Ensure Redis is installed and running
2. Configure Redis connection in `appsettings.json`
3. Add endpoint patterns and cache durations
4. Run the application

### Docker Development
1. Install Docker and Docker Compose
2. Run `docker-compose up -d --build`
3. Access the API at http://localhost:8080
4. Access Swagger UI at http://localhost:8080/swagger

## Example Endpoints

The project includes several example endpoints:
- `/weatherforecast` - Weather data (15-minute cache)
- `/api/products` - Product listing (60-minute cache)
- `/api/products/{id}` - Single product (60-minute cache)
- `/api/categories/*` - Category-related endpoints (120-minute cache)

## Troubleshooting

### Docker Issues
1. If services won't start:
   ```bash
   # Check logs
   docker-compose logs
   
   # Rebuild from scratch
   docker-compose down
   docker-compose up -d --build --force-recreate
   ```

2. If Redis connection fails:
   - Check if Redis container is running: `docker-compose ps`
   - Verify network connectivity: `docker network inspect cache-network`
   - Check Redis logs: `docker-compose logs redis`

3. To reset all data:
   ```bash
   docker-compose down -v
   docker-compose up -d --build
   ```
