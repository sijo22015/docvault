FROM node:22-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:alpine AS final
COPY --from=build /app/dist /usr/share/nginx/html
# Template is processed at container startup via envsubst — set BACKEND_URL env var
COPY nginx.conf.template /etc/nginx/templates/default.conf.template
EXPOSE 80
