import {
  Client,
  EmbedBuilder,
  GatewayIntentBits,
  REST,
  Routes,
  SlashCommandBuilder,
  SlashCommandStringOption,
} from "discord.js";
import auth from "../auth.json" assert { type: "json" };
import { EmbedList, fetchBingus } from "./util.js";

const commands = [
  new SlashCommandBuilder()
    .setName("ask")
    .setDescription("Searches for the answer with Bingus")
    .addStringOption(
      new SlashCommandStringOption()
        .setName("query")
        .setRequired(true)
        .setDescription("What's your question owo")
        .setMaxLength(200),
    ),
];

const rest = new REST({ version: "10" }).setToken(auth.token);

try {
  console.log("Started refreshing application (/) commands.");

  await rest.put(Routes.applicationCommands(auth.client), { body: commands });

  console.log("Successfully reloaded application (/) commands.");
} catch (error) {
  console.error(error);
}

const client = new Client({ intents: [GatewayIntentBits.Guilds] });

client.on("ready", () => {
  console.log(`Logged in as ${client.user?.tag}`);
});

client.on("interactionCreate", async (interaction) => {
  if (!interaction.isCommand()) return;

  if (interaction.commandName === "ask") {
    if (!interaction.isChatInputCommand()) return;
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    const query = interaction.options.getString("query")!;
    console.log(`User @${interaction.user.id} asked about "${query}"`);

    try {
      await interaction.deferReply({ ephemeral: true });
      const data = await fetchBingus(query);

      if (data.length === 0) {
        await interaction.editReply("No results found.");
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

      await embedList.sendChatInput(interaction);
    } catch (error) {
      console.error(error);
      interaction.editReply("An error occurred while fetching results.");
    }
  }
});

// Check if a new thread is created on one of the configured forums to check
client.on("threadCreate", async (thread, newly) => {
  if (!newly || !auth.forumsCheck.includes(thread.parentId ?? "")) return;

  try {
    const data = await fetchBingus(thread.name);

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
