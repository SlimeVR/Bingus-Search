import {
  ActionRowBuilder,
  ApplicationCommandType,
  ButtonBuilder,
  ContextMenuCommandBuilder,
  ButtonStyle,
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

      const show = new ButtonBuilder()
      .setCustomId('show')
      .setLabel("Show message")
      .setStyle(ButtonStyle.Primary);

      const showB = new ActionRowBuilder<ButtonBuilder>()
      .addComponents(show);
 

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
      const targetChannel = interaction.targetMessage.channel
      

      await embedList.sendChatInput(interaction);

  
      const message = await interaction.followUp({
        flags: MessageFlags.Ephemeral,
        components: [showB]
      });
      

      await message.awaitMessageComponent()

      interaction.deleteReply();
      interaction.deleteReply(message);
      
      embedList.sendChannel(
        targetChannel,
        interaction.user.id,
        undefined,
        { 
          messageReference: interaction.targetMessage},
        );

    } catch (error) {
      console.error(error);
      interaction.editReply("An error occurred while fetching results.");
    }
  },
};
