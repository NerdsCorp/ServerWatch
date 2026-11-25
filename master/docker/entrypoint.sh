#!/bin/sh
set -eu

: "${DB_DATABASE:=/var/lib/serverwatch/database.sqlite}"

umask 0007
APP_KEY_FILE=/var/lib/serverwatch/app_key
mkdir -p /var/lib/serverwatch
app_key_is_valid() {
    APP_KEY="$1" php -r '$key = getenv("APP_KEY"); if (str_starts_with($key, "base64:")) { $key = base64_decode(substr($key, 7), true); } exit(is_string($key) && strlen($key) === 32 ? 0 : 1);'
}

if [ -n "${APP_KEY:-}" ] && ! app_key_is_valid "$APP_KEY"; then
    printf 'Configured APP_KEY is invalid for AES-256-CBC; using the saved key or generating a replacement.\n' >&2
    APP_KEY=
fi

if [ -z "${APP_KEY:-}" ]; then
    if [ -s "$APP_KEY_FILE" ]; then
        APP_KEY="$(cat "$APP_KEY_FILE")"
        if ! app_key_is_valid "$APP_KEY"; then
            printf 'Saved APP_KEY is invalid for AES-256-CBC; generating a replacement.\n' >&2
            APP_KEY=
        fi
    fi
    if [ -z "${APP_KEY:-}" ]; then
        APP_KEY="$(php -r 'echo "base64:".base64_encode(random_bytes(32));')"
        printf '%s' "$APP_KEY" > "$APP_KEY_FILE"
        chmod 0600 "$APP_KEY_FILE"
        printf 'Generated APP_KEY and saved it in the persistent serverwatch-data volume.\n'
    fi
fi
export APP_KEY

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
