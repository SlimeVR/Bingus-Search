import {
  APIEmbed,
  ActionRowBuilder,
  ButtonBuilder,
  ButtonStyle,
  ChatInputCommandInteraction,
  ComponentType,
  EmbedBuilder,
  ReplyOptions,
  TextBasedChannel,
} from "discord.js";

import { readFile } from "fs/promises";
import path from "path";
import { fileURLToPath } from "url";

export async function fetchBingus(query: string) {
  const url = `https://bingus.bscotch.ca/api/faq/search?question=${encodeURIComponent(
    query,
  )}&responseCount=30`;

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
    return new EmbedBuilder(this.embeds[this.index]).setFooter({
      text: `${this.index + 1}/${this.embeds.length}`,
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

export async function fetchBingusData(): Promise<FaqConfig> {
  return JSON.parse(
    await readFile(
      path.join(
        fileURLToPath(import.meta.url),
        "../../../BingusApi/faq_config.json",
      ),
      { encoding: "utf-8" },
    ),
  ) as FaqConfig;
}
