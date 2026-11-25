#!/bin/sh
set -eu

: "${DB_DATABASE:=/var/lib/serverwatch/database.sqlite}"

umask 0007
mkdir -p /var/lib/serverwatch
app_key_is_valid() {
    APP_KEY="$1" php -r '$key = getenv("APP_KEY"); if (str_starts_with($key, "base64:")) { $key = base64_decode(substr($key, 7), true); } exit(is_string($key) && strlen($key) === 32 ? 0 : 1);'
}

if [ -z "${APP_KEY:-}" ]; then
    printf 'APP_KEY is required. Set a valid Laravel key in master/.env before starting the container.\n' >&2
    exit 1
fi
if ! app_key_is_valid "$APP_KEY"; then
    printf 'APP_KEY is invalid for AES-256-CBC. Generate a valid Laravel key and set it in master/.env.\n' >&2
    exit 1
fi

DB_DIRECTORY="$(dirname "$DB_DATABASE")"
mkdir -p "$DB_DIRECTORY" \
    storage/app/private \
    storage/app/public \
    storage/framework/cache/data \
    storage/framework/sessions \
    storage/framework/views \
    storage/logs \
    bootstrap/cache
touch "$DB_DATABASE"
chown -R www-data:www-data "$(dirname "$DB_DATABASE")" storage bootstrap/cache
chmod 0770 "$DB_DIRECTORY"
chmod 0660 "$DB_DATABASE"
chmod -R u+rwX,g+rwX storage bootstrap/cache

if [ "${1:-}" = "supervisord" ]; then
    rm -f /var/lib/serverwatch/.migrated
    su-exec www-data php artisan package:discover --no-interaction
    su-exec www-data php artisan migrate --force
    touch /var/lib/serverwatch/.migrated
    chown www-data:www-data /var/lib/serverwatch/.migrated
    exec /usr/bin/supervisord -c /etc/supervisord.conf -n
fi

exec "$@"
