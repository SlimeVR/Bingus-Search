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
import { EmbedList, fetchBingus } from "./util.js";
import { askCommand } from "./commands/ask.js";
import { shippingCommand } from "./commands/shipping.js";

export interface Command {
  builder: Omit<SlashCommandBuilder, "addSubcommand" | "addSubcommandGroup">;
  run: (interaction: ChatInputCommandInteraction) => Promise<void>;
  autocomplete?: (interaction: AutocompleteInteraction) => Promise<void>;
}

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
  intents: [GatewayIntentBits.Guilds, GatewayIntentBits.MessageContent],
});

client.on("ready", () => {
  console.log(`Logged in as ${client.user?.tag}`);
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
  const lastMessage = await thread.fetchStarterMessage();
  if (lastMessage === null) {
    console.error(
      `Couldn't read @${thread.ownerId}'s message on thread #${thread.id}, message ${thread.lastMessageId}`,
    );
    return;
  }
  const content = lastMessage.content
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

client.login(auth.token);
