-- 2026-10-21: Bodyweight becomes its own dated log decoupled from workout sessions (one entry per
-- calendar date), so it can be added/edited/deleted independently and logged on rest days too.
-- workout_sessions.body_weight_kg (added in 0003) is no longer used by the app from this point on;
-- it was dropped manually in Supabase rather than via a migration file - see the commit this file
-- shipped in for context: `alter table public.workout_sessions drop column body_weight_kg;`
create table public.body_weight_entries (
  user_id uuid not null references auth.users (id) on delete cascade,
  date date not null,
  weight_kg numeric not null,
  updated_at timestamptz not null default now(),
  primary key (user_id, date)
);

alter table public.body_weight_entries enable row level security;

create policy "select_own_bodyweight"
  on public.body_weight_entries for select
  to authenticated
  using (auth.uid() = user_id);

create policy "insert_own_bodyweight"
  on public.body_weight_entries for insert
  to authenticated
  with check (auth.uid() = user_id);

create policy "update_own_bodyweight"
  on public.body_weight_entries for update
  to authenticated
  using (auth.uid() = user_id)
  with check (auth.uid() = user_id);

create policy "delete_own_bodyweight"
  on public.body_weight_entries for delete
  to authenticated
  using (auth.uid() = user_id);

grant usage on schema public to authenticated, service_role;
grant select, insert, update, delete on public.body_weight_entries to authenticated;
grant select on public.body_weight_entries to service_role; -- read-only, for the weekly backup job
