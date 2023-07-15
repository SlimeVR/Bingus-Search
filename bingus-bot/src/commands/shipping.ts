import {
  SlashCommandBuilder,
  SlashCommandIntegerOption,
  SlashCommandStringOption,
} from "discord.js";
import { Command } from "../index.js";

enum SlimeSet {
  LOWER_BODY_PURPLE = "Purple Lower Body Set (5+0)",
  LOWER_BODY_BLACK = "Black Lower Body Set (5+0)",
  LOWER_BODY_WHITE = "White Lower Body Set (5+0)",
  CORE_PURPLE = "Purple Core Set (5+1)",
  CORE_BLACK = "Black Core Set (5+1)",
  CORE_WHITE = "White Core Set (5+1)",
  ENHANCED_CORE_PURPLE = "Purple Enhanced Core Set (5+3)",
  ENHANCED_CORE_BLACK = "Black Enhanced Core Set (5+3)",
  ENHANCED_CORE_WHITE = "White Enhanced Core Set (5+3)",
  FULLBODY_PURPLE = "Purple Full-Body Set (7+3)",
  FULLBODY_BLACK = "Black Full-Body Set (7+3)",
  FULLBODY_WHITE = "White Full-Body Set (7+3)",
  DELUXE_TRACKER_PURPLE = "Purple Deluxe Tracker Set (10+6)",
  DELUXE_TRACKER_BLACK = "Black Deluxe Tracker Set (10+6)",
  DELUXE_TRACKER_WHITE = "White Deluxe Tracker Set (10+6)",
}

const SHIP_WHEN_CHANNEL =
  "https://discord.com/channels/817184208525983775/1129107343058153623";

export const shippingCommand: Command = {
  builder: new SlashCommandBuilder()
    .setName("shipping")
    .setDescription(
      "Tries to look up if your batch has been shipped to Crowdsupply",
    )
    .addIntegerOption(
      new SlashCommandIntegerOption()
        .setName("order")
        .setRequired(true)
        .setDescription("Your order number on Crowdsupply")
        .setMinValue(0),
    )
    .addStringOption(
      new SlashCommandStringOption()
        .setName("set")
        .setDescription("What set did you buy")
        .setRequired(true)
        .addChoices(
          ...Object.entries(SlimeSet).map(([, value]) => ({
            name: value,
            value,
          })),
        ),
    ),
  async run(interaction) {
    const order = interaction.options.getInteger("order")!;
    const set = interaction.options.getString("set")! as SlimeSet;

    console.log(
      `User @${interaction.user.id} asked about order #${order} in set ${
        Object.entries(SlimeSet).find(([, value]) => value === set)?.[0] ?? set
      }`,
    );

    const shipment = SHIPMENTS.findIndex((x) => x[set] >= order);
    const shipped = SHIPPED_SHIPMENTS.has(shipment);

    if (shipment === -1) {
      await interaction.reply({
        content: `Your order hasn't been made yet.
You can check on ${SHIP_WHEN_CHANNEL} on the progress of orders.`,
        ephemeral: true,
      });
      return;
    }

    if (shipped) {
      await interaction.reply({
        content: `Your order has been shipped to Crowdsupply, it's shipment ${
          shipment + 2
        }!
You can check on ${SHIP_WHEN_CHANNEL} on the progress of the shipment.`,
        ephemeral: true,
      });
      return;
    }

    await interaction.reply({
      content: `Your order is being made currently, it's shipment ${
        shipment + 2
      }!
You can check on ${SHIP_WHEN_CHANNEL} to see when it's going to get shipped.`,
      ephemeral: true,
    });
  },
};

// Index of shipped shipment
const SHIPPED_SHIPMENTS = new Set([0]);

// Shipments being prepared or shipped
const SHIPMENTS = [
  {
    [SlimeSet.LOWER_BODY_PURPLE]: 145034,
    [SlimeSet.LOWER_BODY_BLACK]: 129822,
    [SlimeSet.LOWER_BODY_WHITE]: 143933,
    [SlimeSet.CORE_PURPLE]: 148676,
    [SlimeSet.CORE_BLACK]: 125190,
    [SlimeSet.CORE_WHITE]: 144063,
    [SlimeSet.ENHANCED_CORE_PURPLE]: 124382,
    [SlimeSet.ENHANCED_CORE_BLACK]: 124538,
    [SlimeSet.ENHANCED_CORE_WHITE]: 126021,
    [SlimeSet.FULLBODY_PURPLE]: 124693,
    [SlimeSet.FULLBODY_BLACK]: 124676,
    [SlimeSet.FULLBODY_WHITE]: 124997,
    [SlimeSet.DELUXE_TRACKER_PURPLE]: 135451,
    [SlimeSet.DELUXE_TRACKER_BLACK]: 127939,
    [SlimeSet.DELUXE_TRACKER_WHITE]: 150693,
  },
  {
    [SlimeSet.LOWER_BODY_PURPLE]: 145034,
    [SlimeSet.LOWER_BODY_BLACK]: 144797,
    [SlimeSet.LOWER_BODY_WHITE]: 143933,
    [SlimeSet.CORE_PURPLE]: 148676,
    [SlimeSet.CORE_BLACK]: 143948,
    [SlimeSet.CORE_WHITE]: 147485,
    [SlimeSet.ENHANCED_CORE_PURPLE]: 129711,
    [SlimeSet.ENHANCED_CORE_BLACK]: 124703,
    [SlimeSet.ENHANCED_CORE_WHITE]: 129346,
    [SlimeSet.FULLBODY_PURPLE]: 124951,
    [SlimeSet.FULLBODY_BLACK]: 125420,
    [SlimeSet.FULLBODY_WHITE]: 126036,
    [SlimeSet.DELUXE_TRACKER_PURPLE]: 141554,
    [SlimeSet.DELUXE_TRACKER_BLACK]: 130253,
    [SlimeSet.DELUXE_TRACKER_WHITE]: 150693,
  },
];
