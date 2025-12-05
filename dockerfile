FROM node:16-slim

WORKDIR /usr/src/app

COPY site/wwwroot/ /usr/src/app/

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

EXPOSE 8080

CMD ["node", "server.js"]