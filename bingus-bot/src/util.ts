import {
  APIEmbed,
  ActionRowBuilder,
  ButtonBuilder,
  ButtonStyle,
  ChatInputCommandInteraction,
  ComponentType,
  EmbedBuilder,
  EmojiIdentifierResolvable,
  ReplyOptions,
  TextBasedChannel,
} from "discord.js";

export const BINGUS_SITE =
  process.env.BINGUS_SITE || "https://bingus.slimevr.io";

export async function fetchBingus(query: string) {
  const url = `${BINGUS_SITE}/faq/search?question=${encodeURIComponent(
    query,
  )}&responseCount=5`;

  return await fetch(url).then(
    (res) => res.json() as Promise<BingusFaqResponse[]>,
  );
}

export interface BingusFaqResponse {
  relevance: number;
  title: string;
  /**
   * Uses markdown
   */
  text: string;
}

export class EmbedList {
  static MAX_TIME = 300_000;
  embeds: APIEmbed[] = [];
  index = 0;

  push(...embed: APIEmbed[]): number {
    return this.embeds.push(...embed);
  }

  getActionRow(): ActionRowBuilder<ButtonBuilder> {
    const next = new ButtonBuilder()
      .setCustomId("next")
      .setLabel("Next >")
      .setStyle(ButtonStyle.Secondary)
      .setDisabled(this.index === this.embeds.length - 1);

    const prev = new ButtonBuilder()
      .setCustomId("prev")
      .setLabel("< Prev")
      .setStyle(ButtonStyle.Secondary)
      .setDisabled(this.index === 0);

    return new ActionRowBuilder<ButtonBuilder>().addComponents(prev, next);
  }

  get(): EmbedBuilder {
    const embed = this.embeds[this.index];
    return new EmbedBuilder(embed).setFooter({
      text: `${this.index + 1}/${this.embeds.length} ${
        embed.footer?.text
      }`.trim(),
    });
  }

  async sendChannel(
    channel: TextBasedChannel,
    who: string | null,
    content?: string,
    reply?: ReplyOptions,
  ) {
    const edit = await channel.send({
      content,
      embeds: [this.get()],
      components: [this.getActionRow()],
      reply,
    });

    const collector = edit.createMessageComponentCollector({
      componentType: ComponentType.Button,
      time: EmbedList.MAX_TIME,
      filter: who ? (i) => i.user.id === who : undefined,
    });

    collector.on("collect", async (i) => {
      switch (i.customId) {
        case "next":
          if (this.index === this.embeds.length - 1) {
            await i.update({});
            return;
          }
          this.index++;
          break;
        case "prev":
          if (this.index === 0) {
            await i.update({});
            return;
          }
          this.index--;
      }

      await i.update({
        embeds: [this.get()],
        components: [this.getActionRow()],
      });
    });

    collector.on("end", async () => {
      await edit.edit({ components: [] });
    });
  }

  async sendChatInput(interaction: ChatInputCommandInteraction) {
    const reply = await (interaction.deferred
      ? interaction.editReply({
          embeds: [this.get()],
          components: [this.getActionRow()],
        })
      : interaction.reply({
          embeds: [this.get()],
          components: [this.getActionRow()],
        }));

    const collector = reply.createMessageComponentCollector({
      componentType: ComponentType.Button,
      time: EmbedList.MAX_TIME,
      // filter: (i) => i.user.id === interaction.user.id,
    });

    collector.on("collect", async (i) => {
      switch (i.customId) {
        case "next":
          if (this.index === this.embeds.length - 1) {
            await i.update({});
            return;
          }
          this.index++;
          break;
        case "prev":
          if (this.index === 0) {
            await i.update({});
            return;
          }
          this.index--;
      }

      await i.update({
        embeds: [this.get()],
        components: [this.getActionRow()],
      });
    });

    collector.on("end", async () => {
      await interaction.editReply({ components: [] });
    });

    return collector;
  }
}

export interface FaqConfig {
  average_questions: boolean;
  faqs: {
    title: string;
    answer: string;
    matched_questions: string[];
  }[];
}

export function fetchBingusData(): Promise<FaqConfig> {
  return fetch(`${BINGUS_SITE}/faq/config`).then((r) =>
    r.json(),
  ) as Promise<FaqConfig>;
}

export const BINGUS_EMOJI: EmojiIdentifierResolvable =
  "<:bingus:1157717351861596200>";

export const NYAGUN_EMOJI: EmojiIdentifierResolvable =
  "<:nya_gun:957426272030576671>";
export const GUNNYA_EMOJI: EmojiIdentifierResolvable =
  "<:gun_nya:962115210670383184>";

export const REACTION_EMOJIS: EmojiIdentifierResolvable[] = [
  "<:langwidjnom:1055961639842750534>",
  "<:Heart:1117832451620868136>",
  "<:nya_a:847203539352551544>",
  "<:nya_umu:850498715617198080>",
  "<:pcbnom:833469925980110908>",
  "<:slimehug:822833057593294908>",
  "💛",
  "🖤",
  "💚",
  "🤎",
  "🤍",
  "💜",
  "❤️",
  "🧡",
  "💙",
  "✨",
  "😻",
  BINGUS_EMOJI,
];

export const SAD_EMOJIS: EmojiIdentifierResolvable[] = [
  "<:spicypillownom:839133143793139733>",
  "<:nya_gun:957426272030576671>",
  "😭",
  "😢",
  "😓",
  "💀",
  "😈",
  "💥",
];
