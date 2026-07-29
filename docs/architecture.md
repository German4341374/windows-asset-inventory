# Architecture Decisions

## ASP.NET Core controllers

Controllers make the portfolio API explicit: routes, response codes, content types, and DTO
boundaries are visible without introducing a large framework abstraction. `ApiController`
provides automatic server-side validation responses.

## DTO boundary

Requests and responses use types from `Contracts/`. EF Core entities remain inside the
application, which prevents navigation-property cycles, over-posting, and accidental schema
coupling.

## SQLite

SQLite requires no external service and works on Windows, WSL2, Linux, and Docker. It is a good
fit for a single local inventory process. The trade-off is limited write concurrency and no
horizontal multi-instance design.

## Assignment policy

`AssetAssignmentPolicy` owns state transitions:

- only an `In Stock` asset may be assigned;
- an asset with an owner cannot be assigned again;
- `Repair` and `Retired` assets cannot be assigned;
- returning an assigned asset clears the owner and restores `In Stock`.

Keeping these rules outside controllers makes them deterministic and directly unit-testable.

## Automatic migrations

The application applies committed EF Core migrations before accepting traffic. This makes a
local demonstration simple. A production fleet should run the same migration artifact as a
controlled deployment step before starting multiple instances.

## Error contract

Validation errors, business conflicts, missing records, and unhandled errors use RFC 7807 Problem
Details. A trace identifier is included to correlate a response with structured ASP.NET Core logs.

## Container security

The image uses pinned SDK and runtime tags, a multi-stage publish, a non-root runtime user, a
read-only root filesystem in Compose, dropped Linux capabilities, and a named volume dedicated to
SQLite writes.
