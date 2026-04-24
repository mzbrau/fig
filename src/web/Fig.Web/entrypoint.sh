#!/bin/sh

sed -i "s|https:\/\/localhost:7281|$FIG_API_URI|g" /usr/share/nginx/html/appsettings.json

# If the API URI uses plain HTTP, update the nginx Content-Security-Policy to allow HTTP connections
if echo "$FIG_API_URI" | grep -qE "^http://"; then
  sed -i "s|connect-src 'self' https:|connect-src 'self' http: https:|g" /etc/nginx/nginx.conf
  echo "Note: FIG_API_URI is configured with HTTP ($FIG_API_URI). Nginx Content-Security-Policy has been updated to allow HTTP API connections."
fi

if [ -n "$FIG_WEB_SSL_PORT" ] && [ -n "$SSL_CERT_PATH" ] && [ -n "$SSL_KEY_PATH" ]; then
  if [ -n "$SSL_TRUSTED_CERT_PATH" ]; then
    envsubst '\$FIG_WEB_SSL_PORT \$SSL_CERT_PATH \$SSL_KEY_PATH \$SSL_TRUSTED_CERT_PATH' < /etc/templates/ssl_ca.conf.template > /etc/nginx/conf.d/ssl_ca.conf
  else
    envsubst '\$FIG_WEB_SSL_PORT \$SSL_CERT_PATH \$SSL_KEY_PATH' < /etc/templates/ssl.conf.template > /etc/nginx/conf.d/ssl.conf
  fi
fi

sh -c "/docker-entrypoint.sh"
nginx -g "daemon off;"
