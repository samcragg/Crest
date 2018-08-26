# Metrics

The framework creates basic metrics of each request to enable the monitoring of
the systems health. Some of these metrics are displayed on the health page,
however, there also exists an endpoint that will return the raw data in JSON
format to enable consumption by other systems.

The url for the metrics is `/metrics.json` - there is no version required and
the response is always JSON (i.e. there is no content negation).

**Note** By default the metrics endpoint is only provided in `Development`
environments.

## Enabling The Endpoint

By default, the metrics page is enabled for
[Development environments](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/environments)
only. If you want to enable the endpoint in other environments then you can set
this option in the `appsettings.json` (or the `appsettings.Environment.json`
file to enable it for specific environments only):

```JSON
{
  "hostingOptions": {
    "DisplayMetrics": true
  }
}
```

## Schema

The JSON returned will be in the following format:

```JSON
{
    "requestCount": counter,
    "requestTime": histogram (microseconds),
    "requestSize": histogram (bytes),
    "responseSize": histogram (bytes)
}
```

```JSON
counter
{
    "value": integer,
    "unit": string
}
```

```JSON
histogram
{
    "count": integer,
    "min": integer,
    "max": integer,
    "mean": integer,
    "stdDev": integer,
    "movingAv1min": integer,
    "movingAv5min": integer,
    "movingAv15min": integer,
    "unit": string
}
```
