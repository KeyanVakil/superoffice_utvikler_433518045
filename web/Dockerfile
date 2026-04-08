# Stage 1: Build
FROM node:20-alpine AS build
WORKDIR /app
COPY package.json package-lock.json* ./
RUN npm ci
COPY . .
RUN npm run build

# Stage 2: Serve
FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf.template /etc/nginx/templates/default.conf.template
EXPOSE 3000
# nginx:alpine auto-renders /etc/nginx/templates/*.template using envsubst
# Default API_UPSTREAM for docker-compose; override via env var for ACI
ENV API_UPSTREAM=api:5000
CMD ["nginx", "-g", "daemon off;"]
