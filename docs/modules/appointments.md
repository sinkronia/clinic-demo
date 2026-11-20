# Module: appointments

Spec for issue #1: "feat: schedule appointments without overlaps".

## Model

- An appointment belongs to a patient and a dental chair, both in the same tenant.
- Fields: `StartsAt` and `EndsAt` (`DateTimeOffset`), and a status: `Scheduled` or `Cancelled`.
- Cancelled appointments are kept, never deleted.

## Endpoints

### `POST /appointments`

Requires `Idempotency-Key` and `X-Tenant-Id`. Rules, in order of the error they produce:

| Rule | Error |
| --- | --- |
| `EndsAt > StartsAt`, and duration between 15 and 240 minutes | `400` `validation.failed` with field error codes |
| Start time is not in the past (use `TimeProvider`) | `400` `appointment.in_past` |
| The patient exists in the tenant and is not archived | `404` `patient.not_found` (archived counts as not found) |
| **No overlap** with another non-cancelled appointment on the same chair | `409` `appointment.overlap` |

Touching boundaries do **not** overlap: 10:00-10:30 and 10:30-11:00 on the same chair are both allowed.
Overlap is checked per chair: the same time on a different chair is allowed.

### `POST /appointments/{id}/cancel`

Sets the status to `Cancelled`. A cancelled appointment frees its slot. Unknown id in the tenant: `404` `appointment.not_found`. Cancelling twice is idempotent (returns the appointment, `200`).

### `GET /appointments?date=YYYY-MM-DD`

Appointments starting on that date (UTC), ordered by `StartsAt`. Includes cancelled ones with their status.

## Tests required

- Overlap, including touching boundaries (10:00-10:30 and 10:30-11:00 do NOT overlap).
- Cancelled appointments free the slot.
- Tenant isolation.
- Idempotent replay.
- Validation (duration limits, end before start), past start time, archived or unknown patient.
