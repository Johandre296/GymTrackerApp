-- 2026-10-21: Session notes, a (since superseded - see 0004) bodyweight column on
-- workout_sessions, and the personal_bests table for PB tracking/celebrations.
alter table public.workout_sessions add column body_weight_kg numeric;
alter table public.workout_sessions add column notes text not null default '';

-- New table: one row per exercise per user, holding its all-time best working-set weight.
create table public.personal_bests (
  user_id uuid not null references auth.users (id) on delete cascade,
  exercise_name text not null,
  weight numeric not null,
  reps int not null,
  session_id uuid not null,
  achieved_at timestamptz not null default now(),
  primary key (user_id, exercise_name)
);

alter table public.personal_bests enable row level security;

create policy "select_own_pbs"
  on public.personal_bests for select
  to authenticated
  using (auth.uid() = user_id);

create policy "insert_own_pbs"
  on public.personal_bests for insert
  to authenticated
  with check (auth.uid() = user_id);

create policy "update_own_pbs"
  on public.personal_bests for update
  to authenticated
  using (auth.uid() = user_id)
  with check (auth.uid() = user_id);

create policy "delete_own_pbs"
  on public.personal_bests for delete
  to authenticated
  using (auth.uid() = user_id);

-- No grant to `anon`, same reasoning as workout_sessions: this is private data behind a login.
grant usage on schema public to authenticated, service_role;
grant select, insert, update, delete on public.personal_bests to authenticated;
grant select on public.personal_bests to service_role; -- read-only, for the weekly backup job
