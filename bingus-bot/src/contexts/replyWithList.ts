import {
  ApplicationCommandType,
  ContextMenuCommandBuilder,
  EmbedBuilder,
  MessageFlags,
} from "discord.js";
import { ContextMenu } from "../index.js";
import { EmbedList, fetchBingus } from "../util.js";

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

      const embedList = new EmbedList();
      embedList.push(
        ...data.slice(0, 5).map(
          (res) =>
            new EmbedBuilder()
              .setAuthor({
                name: `Triggered by ${interaction.user.displayName}`,
                iconURL: interaction.user.avatarURL() ?? undefined,
              })
              .setTitle(res.title)
              .setDescription(res.text)
              .setColor("#65459A")
              .setFooter({ text: `(${res.relevance.toFixed()}% relevant)` })
              .data,
        ),
      );

      await embedList.sendChannel(
        interaction.targetMessage.channel,
        interaction.user.id,
        undefined,
        { messageReference: interaction.targetMessage },
      );

      await interaction.editReply("Replied to the message!");
    } catch (error) {
      console.error(error);
      interaction.editReply("An error occurred while fetching results.");
    }
  },
};
