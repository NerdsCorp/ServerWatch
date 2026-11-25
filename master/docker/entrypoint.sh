#!/bin/sh
set -eu

: "${APP_KEY:?APP_KEY must be set in the master .env file}"
: "${DB_DATABASE:=/var/lib/serverwatch/database.sqlite}"

umask 0007
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
