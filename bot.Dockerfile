FROM node:24-slim AS base

COPY ./bingus-bot/ /app/bingus-bot/
COPY ./package*.json /app/
WORKDIR /app

RUN pnpm ci
RUN pnpm run -w bingus-bot build

CMD [ "pnpm", "-w", "bingus-bot", "start" ]
