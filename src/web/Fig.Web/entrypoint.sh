#!/bin/sh

FIG_API_URI="${FIG_API_URI:-https://localhost:7281}"

sed -i "s|https:\/\/localhost:7281|$FIG_API_URI|g" /usr/share/nginx/html/appsettings.json

connect_src="'self' https:"
if echo "$FIG_API_URI" | grep -qE "^http://"; then
  connect_src="'self' http: https:"
  echo "Note: FIG_API_URI is configured with HTTP ($FIG_API_URI). Shared nginx Content-Security-Policy will allow HTTP API connections."
fi

cat <<EOF > /etc/nginx/conf.d/fig-web-server-common.conf
add_header X-Content-Type-Options "nosniff" always;
add_header X-Frame-Options "DENY" always;
add_header Referrer-Policy "strict-origin-when-cross-origin" always;
add_header Permissions-Policy "camera=(), microphone=(), geolocation=()" always;
add_header Content-Security-Policy "default-src 'self'; script-src 'self' 'wasm-unsafe-eval'; script-src-attr 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; worker-src 'self' data:; connect-src ${connect_src};" always;

location / {
    root /usr/share/nginx/html;
    try_files \$uri \$uri/ /index.html =404;
}
EOF

if [ -n "$FIG_WEB_SSL_PORT" ] && [ -n "$SSL_CERT_PATH" ] && [ -n "$SSL_KEY_PATH" ]; then
  if [ -n "$SSL_TRUSTED_CERT_PATH" ]; then
    envsubst '\$FIG_WEB_SSL_PORT \$SSL_CERT_PATH \$SSL_KEY_PATH \$SSL_TRUSTED_CERT_PATH' < /etc/templates/ssl_ca.conf.template > /etc/nginx/conf.d/ssl_ca.conf
  else
    envsubst '\$FIG_WEB_SSL_PORT \$SSL_CERT_PATH \$SSL_KEY_PATH' < /etc/templates/ssl.conf.template > /etc/nginx/conf.d/ssl.conf
  fi
fi

sh -c "/docker-entrypoint.sh"
nginx -g "daemon off;"
