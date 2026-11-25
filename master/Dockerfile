FROM php:8.4-fpm-alpine AS php-base

RUN apk add --no-cache icu-libs libzip \
    && apk add --no-cache --virtual .build-deps $PHPIZE_DEPS icu-dev libzip-dev \
    && docker-php-ext-install -j"$(nproc)" intl zip \
    && apk del .build-deps \
    && php -r 'foreach (["curl", "dom", "intl", "mbstring", "pdo_sqlite", "sqlite3", "xml", "zip"] as $extension) { if (!extension_loaded($extension)) { fwrite(STDERR, "Missing required PHP extension: {$extension}\n"); exit(1); } }'

WORKDIR /var/www/html

FROM php-base AS build
COPY --from=composer:2 /usr/bin/composer /usr/local/bin/composer
COPY composer.json ./
RUN composer install --no-dev --no-interaction --prefer-dist --no-autoloader --no-scripts
COPY . .
RUN composer dump-autoload --no-dev --optimize --no-interaction --no-scripts \
    && php artisan package:discover --no-interaction \
    && mkdir -p storage/framework/cache/data storage/framework/sessions storage/framework/views storage/logs bootstrap/cache \
    && chown -R www-data:www-data storage bootstrap/cache

FROM php-base AS runtime
WORKDIR /var/www/html
RUN apk add --no-cache su-exec nginx supervisor
COPY --from=build --chown=www-data:www-data /var/www/html /var/www/html
COPY docker/entrypoint.sh /usr/local/bin/serverwatch-entrypoint
COPY docker/nginx.conf /etc/nginx/http.d/default.conf
COPY docker/supervisord.conf /etc/supervisord.conf
RUN sed -i 's/^user nginx;/user www-data;/' /etc/nginx/nginx.conf \
    && chmod 0555 /usr/local/bin/serverwatch-entrypoint \
    && mkdir -p /var/lib/serverwatch /tmp/nginx/client_body /tmp/nginx/fastcgi \
    && chown -R www-data:www-data /tmp/nginx \
    && chown www-data:www-data /var/lib/serverwatch

EXPOSE 8080
HEALTHCHECK --interval=5s --timeout=2s --retries=12 --start-period=20s \
    CMD php -r 'exit(@file_get_contents("http://127.0.0.1:8080/up") === false ? 1 : 0);'
ENTRYPOINT ["/usr/local/bin/serverwatch-entrypoint"]
CMD ["supervisord"]
