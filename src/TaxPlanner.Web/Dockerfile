# Build stage: install dependencies and build the Angular app.
FROM node:26-alpine AS build
WORKDIR /app

# Copy the dependency files first so `npm ci` is cached until they change.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npx ng build

# Runtime stage: nginx serves the built files and forwards API calls to the api service.
FROM nginx:1.28-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/tax-planner-web/browser /usr/share/nginx/html
EXPOSE 8080
