# Build context is mcp-server/ (the directory passed as appSourcePath).
FROM node:22-alpine
WORKDIR /app

# Dependencies first so they cache independently of source changes.
COPY package.json package-lock.json ./
RUN npm ci --omit=dev && npm cache clean --force

COPY src ./src

ENV NODE_ENV=production \
    PORT=3000
EXPOSE 3000

# Run as the image's non-root user. Note the gateway keeps DCR client registrations
# in .data/, which is container-local: mount a volume or run a single replica.
USER node

CMD ["node", "src/index.js"]
