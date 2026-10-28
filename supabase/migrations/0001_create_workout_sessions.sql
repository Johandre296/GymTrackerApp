-- Migration history for the Supabase (Postgres) schema. These files are a record only - run
-- manually in the Supabase SQL Editor, in order, whenever a new one is added. Nothing executes
-- them automatically.
--
-- 2026-10-13: Initial schema. One row per logged workout day.
create table public.workout_sessions (
  id uuid primary key,
  user_id uuid not null references auth.users (id) on delete cascade,
  date date not null,
  day_name text not null,
  data jsonb not null,
  updated_at timestamptz not null default now()
);

alter table public.workout_sessions enable row level security;

create policy "select_own_sessions"
  on public.workout_sessions for select
  to authenticated
  using (auth.uid() = user_id);

create policy "insert_own_sessions"
  on public.workout_sessions for insert
  to authenticated
  with check (auth.uid() = user_id);

create policy "update_own_sessions"
  on public.workout_sessions for update
  to authenticated
  using (auth.uid() = user_id)
  with check (auth.uid() = user_id);

create policy "delete_own_sessions"
  on public.workout_sessions for delete
  to authenticated
  using (auth.uid() = user_id);

-- No grant to `anon` here, deliberately: this table holds private workout data behind a
-- login screen, so unauthenticated requests should see nothing at all, not even via RLS.
grant usage on schema public to authenticated;
grant select, insert, update, delete on public.workout_sessions to authenticated;
