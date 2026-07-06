FROM node:26-alpine AS build
WORKDIR /app

COPY src/mortgageflow-web/package*.json ./
RUN npm ci

COPY src/mortgageflow-web/ ./
RUN npm run build

FROM nginx:1.27-alpine AS runtime
COPY src/mortgageflow-web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 8080
