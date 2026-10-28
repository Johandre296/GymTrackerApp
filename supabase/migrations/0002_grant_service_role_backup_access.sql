-- 2026-10-18: The weekly backup GitHub Action (using the service_role key) was getting a 403 -
-- service_role bypasses RLS but still needs an explicit table grant, which migration 0001 never gave it.
grant usage on schema public to service_role;
grant select on public.workout_sessions to service_role; -- read-only: only the backup job uses this
