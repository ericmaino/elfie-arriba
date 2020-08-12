FROM ericmaino/elfie-arriba:latest as build

FROM node:10.21.0 as node-build

COPY --from=build /src/Arriba/Arriba.Web/package.json /app/package.json
WORKDIR /app
RUN npm install

COPY --from=build /src/Arriba/Arriba.Web/ /app/
ADD ./Configuration /app/configuration
RUN npm run build

FROM nginx:1.19.0

COPY nginx.conf /etc/nginx/nginx.conf
COPY --from=node-build /app/ /var/www
