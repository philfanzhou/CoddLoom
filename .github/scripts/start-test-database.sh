#!/usr/bin/env bash
set -euo pipefail

provider="${1:?Database provider is required.}"
container_name="coddloom-test-database"

case "$provider" in
  PostgreSql)
    docker run --detach --name "$container_name" \
      --env POSTGRES_DB=coddloom \
      --env POSTGRES_PASSWORD=postgres \
      --env POSTGRES_USER=postgres \
      --publish 5432:5432 \
      postgres:16-alpine
    health_command=(docker exec "$container_name" pg_isready -U postgres -d coddloom)
    ;;
  MySql)
    docker run --detach --name "$container_name" \
      --env MYSQL_DATABASE=coddloom \
      --env MYSQL_ROOT_PASSWORD=coddloom \
      --publish 3306:3306 \
      mysql:8.4
    health_command=(docker exec "$container_name" mysqladmin ping --silent -h 127.0.0.1 -uroot -pcoddloom)
    ;;
  MariaDB)
    docker run --detach --name "$container_name" \
      --env MARIADB_DATABASE=coddloom \
      --env MARIADB_ROOT_PASSWORD=coddloom \
      --publish 3306:3306 \
      mariadb:11.4
    health_command=(docker exec "$container_name" healthcheck.sh --connect --innodb_initialized)
    ;;
  SqlServer)
    sql_password='CoddLoom_Strong_Password1!'
    docker run --detach --name "$container_name" \
      --env ACCEPT_EULA=Y \
      --env MSSQL_SA_PASSWORD="$sql_password" \
      --publish 1433:1433 \
      mcr.microsoft.com/mssql/server:2022-latest
    health_command=(docker exec "$container_name" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$sql_password" -C -Q "SELECT 1")
    ;;
  *)
    echo "Unsupported database provider '$provider'." >&2
    exit 2
    ;;
esac

for attempt in {1..60}; do
  if "${health_command[@]}" >/dev/null 2>&1; then
    echo "$provider is ready."
    exit 0
  fi

  if [[ "$(docker inspect --format '{{.State.Running}}' "$container_name" 2>/dev/null || true)" != "true" ]]; then
    docker logs "$container_name" >&2
    exit 1
  fi

  sleep 2
done

docker logs "$container_name" >&2
echo "$provider did not become ready within 120 seconds." >&2
exit 1
