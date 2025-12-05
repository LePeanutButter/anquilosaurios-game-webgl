FROM node:20-slim

WORKDIR /usr/src/app

COPY site/wwwroot/ /usr/src/app/

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

EXPOSE 8080

CMD ["node", "server.js"]