-- 2026-10-21: Track whether a weigh-in was done clothed (incl. shoes etc.) or not, for the
-- bodyweight graph's split/filter view.
alter table public.body_weight_entries add column is_clothed boolean not null default false;
