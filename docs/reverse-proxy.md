# Reverse Proxy / Forwarded Headers

Admin API can sit behind a TLS-terminating reverse proxy (nginx, IIS with
Application Request Routing, a cloud load balancer, a Kubernetes ingress)
that forwards requests to Kestrel over plain HTTP. When that happens, Admin
API needs to know the *original* scheme and host the client used — not the
internal `http://<container-name>` or `http://localhost:<port>` Kestrel
actually sees — so that values it returns to the client, most notably
`urls.openApiMetadata` on `GET /` (see `ReadInformation.cs`), are correct
and usable.

This applies to all three API modes (V1, V2, V3) — `AppSettings:AdminApiMode`
only selects which `specificationVersion`/swagger doc `GET /` returns; the
forwarded-headers pipeline wiring in `Program.cs` sits above that branch and
runs the same way regardless of mode.

## The setting

Controlled by a top-level `ReverseProxy` section in `appsettings.json`,
alongside other feature-settings sections such as `AuditLogging` and
`SwaggerSettings` (not nested under `AppSettings`, which is a single flat
settings object rather than a place other features' settings live):

```json
"ReverseProxy": {
  "UseForwardedHeaders": false,
  "KnownProxies": "",
  "KnownNetworks": ""
}
```

* **`UseForwardedHeaders`** (bool, default `false`) — master switch. When
  `false`, Admin API never looks at `X-Forwarded-*` headers, regardless of
  the other two values.
* **`KnownProxies`** (comma-separated IP addresses, e.g. `"10.0.0.5,10.0.0.6"`)
  — trust `X-Forwarded-*` headers only when the *immediate* caller's IP is
  in this list.
* **`KnownNetworks`** (comma-separated CIDR ranges, e.g.
  `"172.16.0.0/12,192.168.0.0/16"`) — same, but for a whole network range
  instead of individual addresses.

Both `KnownProxies` and `KnownNetworks` are validated at startup
(`ReverseProxySettingsValidator`) — a malformed IP or CIDR entry fails fast
with a clear error instead of silently being ignored or crashing later on
first request.

## Why it's off by default

When `UseForwardedHeaders` is `false` (or true but the immediate caller
matches neither `KnownProxies` nor `KnownNetworks`), Admin API falls back to
whatever Kestrel itself saw — the direct connection's scheme and host. This
is the correct and safe behavior for:

* A bare Kestrel deployment with no reverse proxy in front.
* IIS hosting the app in-process via the ASP.NET Core Module — IIS already
  gives Kestrel the correct scheme/host directly; no forwarded-header
  handling is needed.

Turning `UseForwardedHeaders` on without also restricting `KnownProxies`/
`KnownNetworks` to your actual proxy would mean **any** caller that can
reach Admin API directly could forge `X-Forwarded-Proto`/`X-Forwarded-Host`
and get back a manipulated `openApiMetadata` URL. Restricting to the
proxy's known address (or network) closes that off: the header is honored
only when it really came from the proxy, not from an arbitrary client.

## When you need to turn it on

Turn `UseForwardedHeaders` on, with `KnownProxies` or `KnownNetworks` set to
your reverse proxy's address, whenever something in front of Kestrel
terminates TLS and/or rewrites the host before forwarding to Admin API. The
shipped Docker Compose files are one example: each stack fronts the
`adminapi` container with an nginx gateway container that terminates TLS on
443 and forwards to `adminapi` over plain HTTP inside the compose network —
`adminapi` itself has no published port, so it's only ever reached through
nginx. Because of that (the gateway is the stack's *only* published
ingress), those compose files enable it with the Docker bridge network's
RFC1918 ranges rather than a single static IP, since the bridge subnet's
exact address is assigned dynamically per environment:

```yaml
ReverseProxy__UseForwardedHeaders: "true"
ReverseProxy__KnownNetworks: "172.16.0.0/12,192.168.0.0/16"
```

See [docker.md](docker.md) for the full nginx/Admin API network diagram.

If you run Admin API behind your own reverse proxy or load balancer outside
these shipped compose files — a custom nginx/Traefik/Envoy config, a cloud
load balancer, an IIS instance with ARR in front of it — set
`KnownProxies`/`KnownNetworks` to that proxy's actual address or network,
not the Docker ranges above (those are specific to the shipped compose
topology).

## What it does not affect

This setting only controls how `Request.Scheme`/`Request.Host` are
resolved for building response values like `openApiMetadata`. It has no
effect on:

* Authentication/authorization decisions.
* `AppSettings:PathBase`, which is applied independently via
  `app.UsePathBase(...)` regardless of this setting.
* Rate limiting. `IpRateLimiting:RealIpHeader`/`ClientIdHeader` look like a
  parallel header-trust mechanism but are currently unused dead
  configuration — the rate limiter partitions by request path/method only,
  not by any client-IP header — so there's nothing to keep in sync between
  the two sections.
