import {
  ApplicationCommandType,
  AutocompleteInteraction,
  ChatInputCommandInteraction,
  Client,
  ContextMenuCommandBuilder,
  EmbedBuilder,
  GatewayIntentBits,
  MessageContextMenuCommandInteraction,
  REST,
  Routes,
  SlashCommandOptionsOnlyBuilder,
  UserContextMenuCommandInteraction,
} from "discord.js";
import auth from "../auth.json" with { type: "json" };
import {
  BINGUS_EMOJI,
  EmbedList,
  GUNNYA_EMOJI,
  LANGUAGE_EMOJI,
  NYAGUN_EMOJI,
  BINGUSGUN_EMOJI,
  NIGHTYGUN_EMOJI,
  QUESTION_EMOJI,
  REACTION_EMOJIS,
  SAD_EMOJIS,
  fetchBingus,
  EmojiSearch,
} from "./util.js";
import { askCommand } from "./commands/ask.js";
import winkNLP from "wink-nlp";
import model from "wink-eng-lite-web-model";
import { replyContext } from "./contexts/reply.js";
import { replyListContext } from "./contexts/replyWithList.js";

interface ContainsBuilder {
  builder: unknown;
}

export interface Command {
  builder: SlashCommandOptionsOnlyBuilder;
  run: (interaction: ChatInputCommandInteraction) => Promise<void>;
  autocomplete?: (interaction: AutocompleteInteraction) => Promise<void>;
}

export interface ContextMenu<
  T extends ContextMenuCommandBuilder = ContextMenuCommandBuilder,
> {
  builder: T;
  run: (
    interaction: T["type"] extends ApplicationCommandType.User
      ? UserContextMenuCommandInteraction
      : MessageContextMenuCommandInteraction,
  ) => Promise<void>;
}

const nlp = winkNLP(model, ["negation", "sentiment"]);
const { its } = nlp;

const commands = [askCommand];
const contexts = [replyContext, replyListContext];

const rest = new REST({ version: "10" }).setToken(auth.token);

try {
  console.log("Started refreshing application (/) commands.");

  await rest.put(Routes.applicationCommands(auth.client), {
    body: (commands as ContainsBuilder[])
      .concat(contexts)
      .map((x) => x.builder),
  });

  console.log("Successfully reloaded application (/) commands.");
} catch (error) {
  console.error(error);
}

const client = new Client({
  intents: [
    GatewayIntentBits.Guilds,
    GatewayIntentBits.MessageContent,
    GatewayIntentBits.GuildMessages,
  ],
});

let clientId: string;

client.on("clientReady", (client) => {
  console.log(`Logged in as ${client.user.tag}`);
  clientId = client.user.id;
});

// Autocomplete handler
client.on("interactionCreate", async (interaction) => {
  if (!interaction.isAutocomplete()) return;
  const cmd = commands.find(
    (x) =>
      x.autocomplete !== undefined &&
      x.builder.name === interaction.commandName,
  );
  if (cmd) await cmd.autocomplete!(interaction);
});

// Chat command handler
client.on("interactionCreate", async (interaction) => {
  if (!interaction.isChatInputCommand()) return;

  const cmd = commands.find((x) => x.builder.name === interaction.commandName);
  if (cmd) await cmd.run(interaction);
});

client.on("interactionCreate", async (interaction) => {
  if (!interaction.isContextMenuCommand()) return;

  const ctx = contexts.find((x) => x.builder.name === interaction.commandName);
  if (ctx) await ctx.run(interaction as MessageContextMenuCommandInteraction);
});

// Check if a new thread is created on one of the configured forums to check
client.on("threadCreate", async (thread, newly) => {
  if (!newly || !auth.forumsCheck.includes(thread.parentId ?? "")) return;
  const lastMessage = await thread.fetchStarterMessage().catch(() => null);
  const content = lastMessage?.content
    ? `${thread.name}. ${lastMessage.content}`
    : thread.name;
  console.log(
    `Answering to @${thread.ownerId} on #${thread.id} because of creating a thread with query "${content}"`,
  );

  try {
    const data = await fetchBingus(content);

    if (data.length === 0) {
      return;
    }

    // React if not confident with the first answer
    if (data[0].relevance <= 40) {
      lastMessage?.react(QUESTION_EMOJI);
    }

    const embedList = new EmbedList();
    embedList.push(
      ...data.slice(0, 5).map(
        (res) =>
          new EmbedBuilder()
            .setTitle(res.title)
            .setDescription(res.text)
            .setColor("#65459A")
            .setFooter({ text: `(${res.relevance.toFixed()}% relevant)` }).data,
      ),
    );

    await thread.fetch();

    await embedList.sendChannel(
      thread,
      thread.ownerId,
      `Maybe this will help?`,
    );
  } catch (error) {
    console.error(error);
  }
});

const TRY_REACT_CHANNELS: string[] = [
  "818062236492759050",
  "1129107343058153623",
  "903962635161174076",
  "855164207615705118",
];

// This can only be for cute stuff!
client.on("messageCreate", async (msg) => {
  await msg.fetch();
  const lowercase = msg.content.toLowerCase();
  const emojisIncluded = new EmojiSearch(msg.content);
  // Check if Bingus is being mentioned in some way
  if (emojisIncluded.has(BINGUS_EMOJI.toString())) {
    // React back with the emote
    return await msg.react(BINGUS_EMOJI);
  }

  if (
    msg.mentions.users.has(clientId) ||
    /\b(bot|bing\w{0,4})\b/.test(lowercase)
  ) {
    // React back with gun nya when nya gun
    if (emojisIncluded.has(NYAGUN_EMOJI.toString())) {
      return await msg.react(GUNNYA_EMOJI);
    }

    // React back with nya gun when gun nya
    if (emojisIncluded.has(GUNNYA_EMOJI.toString())) {
      return await msg.react(NYAGUN_EMOJI);
    }

    // React back with bingus_gun when bingus_gun or nighty_gun
    if (
      emojisIncluded.has(BINGUSGUN_EMOJI.toString()) ||
      emojisIncluded.has(NIGHTYGUN_EMOJI.toString())
    ) {
      return await msg.react(BINGUSGUN_EMOJI);
    }

    // Check if Bingus recently sent a message
    const lastMessages = await msg.channel.messages.fetch({
      limit: 10,
      before: msg.id,
      cache: false,
    });

    if (
      !lastMessages.some((m) => m.author.id === clientId) &&
      Math.random() > 0.25
    ) {
      return;
    }
  } else if (TRY_REACT_CHANNELS.some((x) => x === msg.channelId)) {
    1;
    // React if mentioning translator role
  } else if (msg.mentions.roles.has("1055961071313223810")) {
    await msg.react(LANGUAGE_EMOJI);
  } else {
    return;
  }

  const sentiment = nlp.readDoc(msg.content).out(its.sentiment);
  if (typeof sentiment === "string") return;

  // React with an emoji
  if (sentiment >= 0.35) {
    await msg.react(
      REACTION_EMOJIS[Math.floor(Math.random() * REACTION_EMOJIS.length)],
    );
  } else if (sentiment <= -0.4) {
    await msg.react(SAD_EMOJIS[Math.floor(Math.random() * SAD_EMOJIS.length)]);
  } else if (
    (msg.embeds.length > 0 || msg.attachments.size > 0) &&
    TRY_REACT_CHANNELS.some((x) => x === msg.channelId)
  ) {
    const random = Math.random();
    if (random < 0.75) {
      if (random <= 0.15) {
        await msg.react(
          SAD_EMOJIS[Math.floor(Math.random() * SAD_EMOJIS.length)],
        );
      } else {
        await msg.react(
          REACTION_EMOJIS[Math.floor(Math.random() * REACTION_EMOJIS.length)],
        );
      }
    }
  }
});

client.login(auth.token);
