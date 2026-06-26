# syntax=docker/dockerfile:1
# Build Stage
FROM node:22.13.0-alpine AS build
WORKDIR /app
COPY package*.json /app/
RUN npm install
# COPY . .
COPY --chown=app:app . .
EXPOSE 3000
RUN npm run build
# Run the app
CMD ["npm", "run", "dev"]

# Production Stage
# FROM node:22.13.0-alpine AS production
# WORKDIR /app
# COPY package*.json ./
# RUN npm install --only=production
# COPY --from=build /app/dist ./dist
# CMD ["node", "dist/main.js"]
