# Postgres MCP — query reference

Starter **read-only** queries for the eventhub boilerplate. Adjust limits and filters as needed.

## Schema discovery

```sql
SELECT table_schema, table_name
FROM information_schema.tables
WHERE table_schema = 'app'
ORDER BY table_name;
```

```sql
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_schema = 'app' AND table_name = 'users'
ORDER BY ordinal_position;
```

## Users

```sql
SELECT id, username, email, created_at
FROM app.users
ORDER BY created_at DESC
LIMIT 20;
```

```sql
SELECT id, username, email
FROM app.users
WHERE username = 'demo';
```

## Sessions

```sql
SELECT id, user_id, expires_at, created_at
FROM app.user_sessions
WHERE user_id = '00000000-0000-0000-0000-000000000000'
ORDER BY created_at DESC
LIMIT 5;
```

## Diagnostics

```sql
SELECT COUNT(*) AS user_count FROM app.users;
```

```sql
SELECT COUNT(*) AS active_sessions
FROM app.user_sessions
WHERE expires_at > NOW();
```

## EF migration history

```sql
SELECT migration_id, product_version
FROM app."__EFMigrationsHistory"
ORDER BY migration_id;
```
