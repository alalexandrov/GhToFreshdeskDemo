# GitHub → Freshdesk Sync API

WebAPI that retrieves a **GitHub user** (REST v3) and **creates/updates** a **Freshdesk contact** (API v2).  
It uses **Clean Architecture**, **CQRS with MediatR**, **FluentValidation**, **EF Core (SQLite)**, and a **background worker** for async jobs.

> **What you’ll see**
> - **Async flow**: enqueue a sync job and let the worker process it with retries & dead-letter.
> - **Sync flow**: call the handler directly via the sync endpoint.
> - Simple, minimal, production-style code.

---

## Prerequisites

- .NET 9 SDK
- A GitHub **Personal Access Token** (PAT) – fine-grained or classic; public user info is enough.
- A Freshdesk **API Key** for your subdomain (e.g., `alexdemo.freshdesk.com`).

> **Note:** Freshdesk will return **409** if you try to create a contact with an email that belongs to your agent/admin account. Use a different email (e.g., `octocat@example.com`) when testing.

---

## Configuration

The app reads tokens from these **environment variables** (double underscores map to `:` in .NET configuration):

- `GitHub__Token` → binds to `GitHub:Token`
- `Freshdesk__ApiKey` → binds to `Freshdesk:ApiKey`

You can set them in **`Properties/launchSettings.json`**, or export them in your shell.

---

## Data Flow (Async)

1. `POST /api/jobs/sync-github-user?...` → **Accepted** with `jobId`.
2. Worker claims the next due job:
   - `Pending` → `Processing` (optimistic concurrency)
3. Handler fetches GitHub user and:
   - **Finds** Freshdesk contact by `unique_external_id` → **Update**
   - **Else if** email present, try by email → **Update**
   - **Else** → **Create**
4. On success → `Succeeded`
5. On failure → schedule retry with **backoff**, until **DeadLetter** after `MaxAttempts`.

---

## Troubleshooting

- **Freshdesk 409 Conflict**  
  Happens if the email belongs to your Freshdesk agent/admin account. Test with a different email (e.g., `octocat@example.com`).

- **GitHub email is null**  
  Not uncommon for public profiles. The handler still works via `unique_external_id` (no email fallback).

- **Reset state**  
  Stop the app, delete the SQLite DB file (e.g., `app.db`), run again.
