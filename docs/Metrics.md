# Metrics

The framework creates basic metrics of each request to enable the monitoring of
the systems health. Some of these metrics are displayed on the health page,
however, there also exists an endpoint that will return the raw data in JSON
format to enable consumption by other systems.

The url for the metrics is `/metrics.json` - there is no version required and
the response is always JSON (i.e. there is no content negation).

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
