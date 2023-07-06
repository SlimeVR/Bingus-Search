import { APIEmbed, BaseInteraction, ChatInputCommandInteraction } from "discord.js";

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
  embeds: APIEmbed[] = []
  index = 0
  
  push(embed: APIEmbed): number {
    return this.embeds.push(embed);
  }

  sendChatInput(interaction: ChatInputCommandInteraction) {
    return new Promise((res, rej) => {
      interaction
    });
  }
}
