import {
  EmbedBuilder,
  SlashCommandBooleanOption,
  SlashCommandBuilder,
  SlashCommandStringOption,
} from "discord.js";
import { Command } from "../index.js";
import { EmbedList, fetchBingus, fetchBingusData } from "../util.js";

let faqConfig = (await fetchBingusData()).faqs.flatMap(
  (x) => x.matched_questions,
);

setInterval(async () => {
  faqConfig = (await fetchBingusData()).faqs.flatMap(
    (x) => x.matched_questions,
  );
}, 60 * 60 * 1000); // Do it every hour

export const askCommand: Command = {
  builder: new SlashCommandBuilder()
    .setName("ask")
    .setDescription("Tries to solve your problems >~<")
    .addStringOption(
      new SlashCommandStringOption()
        .setName("query")
        .setRequired(true)
        .setDescription("What's your question owo")
        .setMaxLength(200)
        .setAutocomplete(true),
    )
    .addBooleanOption(
      new SlashCommandBooleanOption()
        .setName("visible")
        .setRequired(false)
        .setDescription(
          "Should the message be visible and interactable by others?",
        ),
    ),
  async run(interaction) {
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    const query = interaction.options.getString("query")!;
    const ephemeral = !interaction.options.getBoolean("visible") ?? true;
    console.log(`User @${interaction.user.id} asked about "${query}"`);

    try {
      await interaction.deferReply({ ephemeral });
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
  },
  async autocomplete(interaction) {
    const focusedValue = interaction.options.getFocused().toLowerCase();
    const filtered = faqConfig.filter((x) => x.includes(focusedValue));
    filtered.length = Math.min(filtered.length, 25);
    await interaction.respond(filtered.map((x) => ({ name: x, value: x })));
  },
};
