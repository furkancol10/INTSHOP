FROM node:22-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
ARG VITE_API_URL=http://localhost:5081
ENV VITE_API_URL=$VITE_API_URL
RUN npm run build

FROM nginx:alpine
ARG VITE_API_URL=http://localhost:5081
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/build /usr/share/nginx/html

# CSP'nin connect-src'i derlenmis JS'in gercekten cagirdigi API origin'i ile senkron kalsin.
RUN sed -i "s|__API_ORIGIN__|${VITE_API_URL}|g" /etc/nginx/conf.d/default.conf \
    && chown -R nginx:nginx /usr/share/nginx/html /var/cache/nginx /var/log/nginx /etc/nginx/conf.d \
    && touch /var/run/nginx.pid \
    && chown nginx:nginx /var/run/nginx.pid

USER nginx
EXPOSE 8080
