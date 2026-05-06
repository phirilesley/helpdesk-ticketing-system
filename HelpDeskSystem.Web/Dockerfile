## syntax=docker/dockerfile:1.6
# Build stage
FROM node:18-alpine as build

WORKDIR /app

# Copy package.json and package-lock.json
COPY package*.json ./

# Install dependencies (with retries for transient network failures)
RUN --mount=type=cache,target=/root/.npm \
    npm config set fetch-retries 10 \
    && npm config set fetch-retry-maxtimeout 180000 \
    && npm config set fetch-retry-mintimeout 20000 \
    && sh -c 'for i in 1 2 3; do npm ci --no-audit --no-fund && exit 0; echo "npm ci failed, retry $i/3"; sleep 20; done; exit 1'

# Copy source code
COPY . .

# Build the application
RUN npm run build

# Production stage
FROM nginx:alpine as production

# Copy built application from build stage
COPY --from=build /app/build /usr/share/nginx/html

# Copy nginx configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Expose port
EXPOSE 80

# Start nginx
CMD ["nginx", "-g", "daemon off;"]
