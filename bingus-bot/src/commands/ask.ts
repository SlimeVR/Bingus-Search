import {
  EmbedBuilder,
  MessageFlags,
  SlashCommandBuilder,
  SlashCommandStringOption,
} from "discord.js";
import { Command } from "../index.js";
import { EmbedList, fetchBingus, fetchBingusData } from "../util.js";

const textCleanRegex = /[^a-z ]/gi

async function getFaqConfig() {
  return (await fetchBingusData()).faqs.flatMap((x) =>
    x.keywords.filter((x) => x.length > 0 && x.length <= 100),
  ).map((x) => ({
    clean: x.toLowerCase().replaceAll(textCleanRegex, ""),
    orig: x
  }));
}

let faqConfig = await getFaqConfig();

setInterval(async () => {
  faqConfig = await getFaqConfig();
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
    .addStringOption(
      new SlashCommandStringOption()
        .setName("custom")
        .setDescription("Let's you customize how the embed will be shown")
        .addChoices(
          {
            name: "Visible",
            value: "VISIBLE",
          },
          {
            name: "Visible but only you can interact with it",
            value: "KINDA_VISIBLE",
          },
          {
            name: "Only first answer",
            value: "FIRST",
          },
        ),
    ),
  async run(interaction) {
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    const query = interaction.options.getString("query")!;
    const customOption = interaction.options.getString("custom");
    const first = customOption === "FIRST";
    console.log(`User ${interaction.user} asked about "${query}"`);

    try {
      await interaction.deferReply({
        flags: customOption === null ? MessageFlags.Ephemeral : undefined,
      });
      const data = await fetchBingus(query);

      if (data.length === 0) {
        await interaction.editReply("No results found.");
        return;
      }

      if (first) {
        await interaction.editReply({
          embeds: [
            new EmbedBuilder()
              .setTitle(data[0].title)
              .setDescription(data[0].text)
              .setColor("#65459A")
              .setFooter({ text: `${data[0].relevance.toFixed()}% relevant` })
              .data,
          ],
        });

        return;
      }

      const embedList = new EmbedList();
      embedList.push(
        ...data.slice(0, 5).map(
          (res) =>
            new EmbedBuilder()
              .setTitle(res.title)
              .setDescription(res.text)
              .setColor("#65459A")
              .setFooter({ text: `(${res.relevance.toFixed()}% relevant)` })
              .data,
        ),
      );

      await embedList.sendChatInput(interaction, customOption === "KINDA_VISIBLE");
    } catch (error) {
      console.error(error);
      interaction.editReply("An error occurred while fetching results.");
    }
  },
  async autocomplete(interaction) {
    const focusedValue = interaction.options.getFocused().toLowerCase().replaceAll(textCleanRegex, "");
    const filtered = faqConfig.filter((x) => x.clean.includes(focusedValue));
    filtered.length = Math.min(filtered.length, 25);
    await interaction.respond(filtered.map((x) => ({ name: x.orig, value: x.orig })));
  },
};
