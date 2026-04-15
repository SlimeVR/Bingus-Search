FROM node:24-slim AS base

ENV PNPM_HOME="/pnpm"
ENV PATH="$PNPM_HOME:$PATH"
ENV COREPACK_ENABLE_DOWNLOAD_PROMPT="0"
RUN corepack enable
COPY ./bingus-bot/ /app/bingus-bot/
COPY ./bingus-bot/auth.json.sample /app/bingus-bot/auth.json
COPY ./package*.json ./pnpm*.yaml /app/
WORKDIR /app

FROM base AS prod-deps
RUN --mount=type=cache,id=pnpm,target=/pnpm/store pnpm install --prod --frozen-lockfile

FROM base AS build
RUN --mount=type=cache,id=pnpm,target=/pnpm/store pnpm install --frozen-lockfile
RUN pnpm run --filter=bingus-bot build

FROM base
COPY --from=prod-deps /app/node_modules /app/node_modules
COPY --from=prod-deps /app/bingus-bot/node_modules /app/bingus-bot/node_modules
COPY --from=build /app/bingus-bot/dist /app/bingus-bot/dist
CMD [ "pnpm", "--filter", "bingus-bot", "start" ]
