# TP-Link WebUI Frontend Dockerfile

# 1) Build stage
FROM node:18-alpine AS build
WORKDIR /app

# Copy package files & install all deps (including Vite)
COPY package*.json ./
RUN npm ci

# Copy source code and build
COPY . .
RUN npm run build

# 2) Production stage
FROM nginx:alpine AS production

# Copy built assets
COPY --from=build /app/dist /usr/share/nginx/html

# Copy custom nginx config
COPY nginx.conf /etc/nginx/nginx.conf

# Create non-root user and fix permissions
RUN addgroup -g 1001 -S nodejs && \
    adduser -S nextjs -u 1001 && \
    chown -R nextjs:nodejs /var/cache/nginx /var/log/nginx /etc/nginx/conf.d

USER nextjs
EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
