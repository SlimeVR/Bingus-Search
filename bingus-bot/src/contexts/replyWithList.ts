import {
  ApplicationCommandType,
  ButtonBuilder,
  ContextMenuCommandBuilder,
  ButtonStyle,
  MessageFlags,
} from "discord.js";
import { ContextMenu } from "../index.js";
import { EmbedList, fetchBingus, replyEmbed } from "../util.js";

export const replyListContext: ContextMenu = {
  builder: new ContextMenuCommandBuilder()
    .setName("Reply with Bingus (List)")
    .setType(ApplicationCommandType.Message),

  async run(interaction) {
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    const query = interaction.targetMessage.content;
    console.log(
      `User ${interaction.user} asked about "${query}" for ${interaction.targetMessage.author}`,
    );

    try {
      await interaction.deferReply({ flags: MessageFlags.Ephemeral });

      if (!interaction.targetMessage.channel.isSendable()) {
        await interaction.editReply("Unable to send messages to this channel.");
        return;
      }

      const data = await fetchBingus(query);
      if (data.length === 0) {
        await interaction.editReply("No results found.");
        return;
      }

      const show = new ButtonBuilder()
        .setCustomId('show')
        .setLabel("Show message")
        .setStyle(ButtonStyle.Primary)

      const embedListEph = new EmbedList(show);
      embedListEph.push(
        ...data.slice(0, 5).map(
          (res) =>
            replyEmbed(interaction, res)
        ),
      );

      const selectedIndex = await embedListEph.sendChatInput(interaction)

      interaction.targetMessage.reply({
        embeds: [replyEmbed(interaction, data[selectedIndex])],
      });

    } catch (error) {
      console.error(error);
      interaction.editReply("An error occurred while fetching results.");
    }
  },
};
