import {
  ActionRowBuilder,
  ApplicationCommandType,
  ButtonBuilder,
  ButtonStyle,
  Client,
  ContextMenuCommandBuilder,
  EmbedBuilder,
  GatewayIntentBits,
  MessageFlags,
} from "discord.js";
import { ContextMenu } from "../index.js";
import { fetchBingus } from "../util.js";

export const replyContext: ContextMenu = {
  builder: new ContextMenuCommandBuilder()
    .setName("Reply with Bingus")
    .setType(ApplicationCommandType.Message),

  async run(interaction) {
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    const query = interaction.targetMessage.content;
    console.log(
      `User ${interaction.user} asked "${query}" for ${interaction.targetMessage.author}`,
    );

    try {
      await interaction.deferReply({ flags: MessageFlags.Ephemeral });
      const data = await fetchBingus(query);

      if (data.length === 0) {
        await interaction.editReply("No results found.");
        return;
      }

      const show = new ButtonBuilder()
      .setCustomId('show')
      .setLabel("Show message")
      .setStyle(ButtonStyle.Primary);

      const showB = new ActionRowBuilder<ButtonBuilder>()
      .addComponents(show);


      const message = await interaction.editReply({
        embeds: [
          new EmbedBuilder()
            .setAuthor({
              name: `Triggered by ${interaction.user.displayName}`,
              iconURL: interaction.user.avatarURL() ?? undefined,
            })
            .setTitle(data[0].title)
            .setDescription(data[0].text)
            .setColor("#65459A")
            .setFooter({ text: `${data[0].relevance.toFixed()}% relevant` })
            .data,

        ],
        components: [showB],
      });

      const collector = message.createMessageComponentCollector();

      collector.on('collect', async i => {
        interaction.deleteReply();
        interaction.targetMessage.reply({
        embeds: [
          new EmbedBuilder()
            .setAuthor({
              name: `Triggered by ${interaction.user.displayName}`,
              iconURL: interaction.user.avatarURL() ?? undefined,
            })
            .setTitle(data[0].title)
            .setDescription(data[0].text)
            .setColor("#65459A")
            .setFooter({text: `${data[0].relevance.toFixed()}% relevant` })

            
            .data,

        ],
      });
      });




    } catch (error) {
      console.error(error);
      interaction.editReply("An error occurred while fetching results.");
    }
  },
};
