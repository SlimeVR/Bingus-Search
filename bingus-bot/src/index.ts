import {
  Client,
  GatewayIntentBits,
  REST,
  Routes,
  SlashCommandBuilder,
  SlashCommandStringOption,
} from "discord.js";
import fetch from "node-fetch";
import auth from "../auth.json" assert { type: "json" };

const commands = [
  new SlashCommandBuilder()
    .setName("search")
    .setDescription("Searches for the answer with Bingus")
    .addStringOption(
      new SlashCommandStringOption()
        .setName("query")
        .setRequired(true)
        .setDescription("What's your question owo"),
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

  if (interaction.commandName === "search") {
    if (!interaction.isChatInputCommand()) return;
    const query = interaction.options.getString("query");
    if (query === null) return;
    const url = `https://bingus.bscotch.ca/api/faq/search?question=${encodeURIComponent(
      query,
    )}&responseCount=30`;

    try {
      const data = await fetch(url).then(
        (res) => res.json() as Promise<BingusFaqResponse[]>,
      );

      if (data && data.length > 0) {
        const firstResult = data[0];
        const embed = {
          title: firstResult.title,
          description: firstResult.text,
        };

        interaction.reply({
          content: "Here is the first result:",
          embeds: [embed],
        });
      } else {
        interaction.reply("No results found.");
      }
    } catch (error) {
      console.error(error);
      interaction.reply("An error occurred while fetching results.");
    }
  }
});

interface BingusFaqResponse {
  relevance: number;
  title: string;
  /**
   * Uses markdown
   */
  text: string;
}

client.login(auth.token);
