# ADR 0001: Trust the `X-Auth-Subject` header as authenticated identity inside the backend

## Status

Accepted — 2026-06-25

## Context

The system fronts all services with Traefik as the edge gateway. A forward-auth
middleware (`jwt-auth`) delegates authentication to `auth-service` `/verify`:

- Clients authenticate by presenting a JWT (`Authorization: Bearer <token>`).
- `auth-service` validates the signature and expiry. On success it returns `200`
  and emits the response headers `X-Auth-Subject` (the JWT `sub` claim) and
  `X-Auth-Roles`.
- Traefik is configured with
  `authResponseHeaders=X-Auth-Subject,X-Auth-Roles`, which copies those headers
  from the auth response onto the upstream request before routing it to the
  downstream service (e.g. `fix-ingress`).

Downstream services need the caller's identity — `fix-ingress` records it as the
`createdBy` audit field on each Fix document — but they do not themselves parse
or validate the JWT.

The question this ADR answers: **how should a downstream service obtain the
authenticated user identity?**

Options considered:

1. **Re-validate the JWT in each downstream service.** Each service parses the
   Bearer token and verifies the signature itself.
   - Spreads the signing secret and token-format knowledge across every service.
   - Duplicates crypto-sensitive code in many places — more surface to get wrong.
   - Couples every service to the current token implementation.

2. **Trust the `X-Auth-Subject` header injected by the gateway.** Downstream
   services read identity from the header and never see the token.
   - Single authentication choke point; downstream code stays simple and
     decoupled from the token format.
   - Correctness depends on gateway invariants, including perimeter trust (see
     Consequences).

3. **Gateway mints a short-lived signed internal assertion.** The gateway
   validates the real JWT once, then issues a small, short-lived signed token
   (an internal JWT or an HMAC-signed header) that each downstream service
   cheaply verifies.
   - Keeps the single-validation simplicity of option 2 while removing the
     dependence on network-perimeter trust: an in-network actor cannot forge
     identity without the internal signing key.
   - Costs key distribution/rotation and verification code in every service —
     complexity this project does not currently justify. Recorded as the
     migration target if the perimeter-trust trade-off below stops holding.

## Decision

Downstream services treat `X-Auth-Subject` as the **authoritative** authenticated
identity and do **not** re-validate the JWT. The forward-auth middleware is the
single trust anchor for authentication.

This is safe only because of three invariants:

1. `auth-service` `/verify` **always** sets `X-Auth-Subject` on a `200`
   response, so Traefik overwrites any value a client tried to supply for that
   header. A client cannot forge the identity through the gateway.
2. Requests that fail verification never reach a downstream service.
3. Downstream services are reachable only on the internal Docker network. All
   external traffic transits Traefik; no service is published directly.

Invariant #3 is **perimeter trust**: we trust the header because we trust the
network it arrives on. This is a deliberate trade-off — we accept perimeter
trust *instead of* a zero-trust approach (mTLS or signed internal assertions,
option 3 above) because the project's scope does not justify that complexity.
It is an accepted simplification, not a closed risk.

## Consequences

**Positive**

- One component owns token validation and the signing secret.
- Downstream services are simpler and decoupled from the token format — the
  token can change shape without touching them.

**Negative / risks**

- The security of every downstream service now rests on the invariants above. If
  `/verify` ever returns `200` without setting the header, or a service becomes
  reachable bypassing Traefik, identity can be spoofed.
- A request arriving at a downstream service with no `X-Auth-Subject` must be
  treated as a misconfiguration / unauthenticated request and rejected — it must
  never fall back to an empty or anonymous creator.
- Residual risk of the perimeter-trust trade-off: any actor that gains a foothold
  inside the network (a compromised service, SSRF, a misrouted entrypoint) can
  forge `X-Auth-Subject` and impersonate any user. Option 3 is the mitigation we
  have chosen not to pay for yet.

**Revisit trigger**

Today `X-Auth-Subject` feeds only the `createdBy` audit field; nothing
*authorizes* off it, so a forged value pollutes provenance but grants no
privilege — the blast radius is small. The moment identity or `X-Auth-Roles`
starts driving authorization decisions, the stakes rise from "dirty audit log"
to "privilege escalation," and this trade-off must be revisited — most likely by
adopting option 3 in a superseding ADR.

## Governance

How this decision is enforced (not just stated):

- `fix-ingress` rejects any `/fix` request that lacks `X-Auth-Subject`.
- `auth-service` `/verify` must never return `200` without setting
  `X-Auth-Subject`; this is the load-bearing invariant and should be covered by a
  test.
- Compose / network configuration must keep downstream services off any
  externally routable Traefik entrypoint; only the gateway is exposed.
- (Future) an integration test that sends a request through the gateway with a
  forged `X-Auth-Subject` header, asserting the value is overwritten by the
  authenticated subject.

## Notes

- Author: Etheraex
- Related code: `auth-service/main.go` (`/verify`), `docker-compose.yml`
  (`jwt-auth` middleware, `authResponseHeaders`),
  `fix-backend/services/fix-ingress/main.go` (`postFix`).
- Format follows the ADR template from *Software Architecture: The Hard Parts*
  (Ford, Richards, Sadalage, Dehghani), including the Governance section.
