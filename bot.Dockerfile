FROM node:20-slim AS base

COPY ./bingus-bot/ /app/bingus-bot/
COPY ./package*.json /app/
WORKDIR /app

RUN npm ci
RUN npm run -w bingus-bot build

CMD [ "npm", "-w", "bingus-bot", "start" ]
