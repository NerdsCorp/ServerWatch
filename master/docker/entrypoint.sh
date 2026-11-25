#!/bin/sh
set -eu

: "${APP_KEY:?APP_KEY must be set in the master .env file}"
: "${DB_DATABASE:=/var/lib/serverwatch/database.sqlite}"

mkdir -p "$(dirname "$DB_DATABASE")"
chown -R www-data:www-data "$(dirname "$DB_DATABASE")" storage bootstrap/cache

if [ "${1:-}" = "php-fpm" ]; then
    rm -f /var/lib/serverwatch/.migrated
    su-exec www-data php artisan migrate --force
    touch /var/lib/serverwatch/.migrated
    chown www-data:www-data /var/lib/serverwatch/.migrated
    exec php-fpm -F
fi

exec "$@"
