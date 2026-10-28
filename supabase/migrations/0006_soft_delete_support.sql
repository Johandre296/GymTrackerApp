-- 2026-10-21: Switches deletes from hard DELETE to a soft "deleted_at" marker, so a device with a
-- stale local copy can no longer resurrect something deleted elsewhere by pushing it back up
-- during sync (sync now distinguishes "never synced yet" from "deleted elsewhere" using every
-- id/date the server has ever seen, not just the currently-active ones).
alter table public.workout_sessions add column deleted_at timestamptz;
alter table public.body_weight_entries add column deleted_at timestamptz;
