FROM php:8.4-fpm-alpine AS php-base

RUN apk add --no-cache oniguruma libcurl sqlite-libs libxml2 su-exec \
    && apk add --no-cache --virtual .build-deps $PHPIZE_DEPS oniguruma-dev curl-dev sqlite-dev libxml2-dev \
    && docker-php-ext-install -j"$(nproc)" pdo_sqlite sqlite3 mbstring dom xml curl \
    && apk del .build-deps

WORKDIR /var/www/html

FROM php-base AS build
COPY --from=composer:2 /usr/bin/composer /usr/local/bin/composer
COPY . .
RUN composer install --no-dev --no-interaction --prefer-dist --optimize-autoloader \
    && mkdir -p storage/framework/cache/data storage/framework/sessions storage/framework/views storage/logs bootstrap/cache \
    && chown -R www-data:www-data storage bootstrap/cache

FROM php-base AS runtime
WORKDIR /var/www/html
COPY --from=build --chown=www-data:www-data /var/www/html /var/www/html
COPY docker/entrypoint.sh /usr/local/bin/serverwatch-entrypoint
RUN chmod 0555 /usr/local/bin/serverwatch-entrypoint \
    && mkdir -p /var/lib/serverwatch \
    && chown www-data:www-data /var/lib/serverwatch

EXPOSE 9000
ENTRYPOINT ["/usr/local/bin/serverwatch-entrypoint"]
CMD ["php-fpm"]
