FROM node:16-slim

WORKDIR /usr/src/app

COPY site/wwwroot/ /usr/src/app/

EXPOSE 8080

CMD ["node", "server.js"]