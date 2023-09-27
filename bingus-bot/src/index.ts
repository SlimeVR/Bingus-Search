import {
  AutocompleteInteraction,
  ChatInputCommandInteraction,
  Client,
  EmbedBuilder,
  GatewayIntentBits,
  REST,
  Routes,
  SlashCommandBuilder,
} from "discord.js";
// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-ignore
import auth from "../auth.json" assert { type: "json" };
import { EmbedList, REACTION_EMOJIS, SAD_EMOJIS, fetchBingus } from "./util.js";
import { askCommand } from "./commands/ask.js";
import { shippingCommand } from "./commands/shipping.js";
import winkNLP from "wink-nlp";
import model from "wink-eng-lite-web-model";

export interface Command {
  builder: Omit<SlashCommandBuilder, "addSubcommand" | "addSubcommandGroup">;
  run: (interaction: ChatInputCommandInteraction) => Promise<void>;
  autocomplete?: (interaction: AutocompleteInteraction) => Promise<void>;
}

const nlp = winkNLP(model, ["negation", "sentiment"]);
const { its } = nlp;

const commands = [askCommand, shippingCommand];

const rest = new REST({ version: "10" }).setToken(auth.token);

try {
  console.log("Started refreshing application (/) commands.");

  await rest.put(Routes.applicationCommands(auth.client), {
    body: commands.map((x) => x.builder),
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

client.on("ready", (client) => {
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

    const embedList = new EmbedList();
    embedList.push(
      ...data.map(
        (res) =>
          new EmbedBuilder()
            .setTitle(res.title)
            .setDescription(res.text)
            .setColor("#65459A").data,
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
  "855164207615705118"
];

// This can only be for cute stuff!
client.on("messageCreate", async (msg) => {
  await msg.fetch();
  const lowercase = msg.content.toLowerCase();
  // Check if Bingus is being mentioned in some way
  if (msg.mentions.users.has(clientId) || /bot|bing\w{,4}/.test(lowercase)) {
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
  }
});

client.login(auth.token);
