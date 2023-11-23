#!/bin/sh

sed -i "s|https:\/\/localhost:7281|$FIG_API_URI|g" /usr/share/nginx/html/appsettings.json
sh -c "/docker-entrypoint.sh"
nginx -g "daemon off;"