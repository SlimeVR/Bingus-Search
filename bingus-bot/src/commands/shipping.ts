import { SlashCommandBuilder, SlashCommandIntegerOption } from "discord.js";
import { Command } from "../index.js";
import { readFile } from "node:fs/promises";

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

const STRING_SET_MAP: Record<string, SlimeSet> = {
  "SLIMEVR-FBT-LBS-P": SlimeSet.LOWER_BODY_PURPLE,
  "SLIMEVR-FBT-LBS-B": SlimeSet.LOWER_BODY_BLACK,
  "SLIMEVR-FBT-LBS-W": SlimeSet.LOWER_BODY_WHITE,
  "SLIMEVR-FBT-CS-P": SlimeSet.CORE_PURPLE,
  "SLIMEVR-FBT-CS-B": SlimeSet.CORE_BLACK,
  "SLIMEVR-FBT-CS-W": SlimeSet.CORE_WHITE,
  "SLIMEVR-FBT-ECS-P": SlimeSet.ENHANCED_CORE_PURPLE,
  "SLIMEVR-FBT-ECS-B": SlimeSet.ENHANCED_CORE_BLACK,
  "SLIMEVR-FBT-ECS-W": SlimeSet.ENHANCED_CORE_WHITE,
  "SLIMEVR-FBT-FBS-P": SlimeSet.FULLBODY_PURPLE,
  "SLIMEVR-FBT-FBS-B": SlimeSet.FULLBODY_BLACK,
  "SLIMEVR-FBT-FBS-W": SlimeSet.FULLBODY_WHITE,
  "SLIMEVR-FBT-DTS-P": SlimeSet.DELUXE_TRACKER_PURPLE,
  "SLIMEVR-FBT-DTS-B": SlimeSet.DELUXE_TRACKER_BLACK,
  "SLIMEVR-FBT-DTS-W": SlimeSet.DELUXE_TRACKER_WHITE,
};

const SHIP_WHEN_CHANNEL =
  "https://discord.com/channels/817184208525983775/1129107343058153623";

let MAX_ORDER = 0;
const ORDER_SET_MAP = new Map(
  (await readFile("./assets/orders.csv", { encoding: "utf8" }))
    .split("\n")
    .slice(1)
    .map((order) => {
      const cols = order.split(",");
      const orderNo = parseInt(cols[0]);
      MAX_ORDER = Math.max(MAX_ORDER, orderNo);
      return [orderNo, STRING_SET_MAP[cols[1]]];
    }),
);

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
    ),

  async run(interaction) {
    const order = interaction.options.getInteger("order")!;


    if (order > MAX_ORDER) {
      await interaction.reply({
        content: `I can't seem to find your order yet, it might be a pretty new one.
You can check on ${SHIP_WHEN_CHANNEL} on the progress of orders.`,
        ephemeral: true,
      });
      return;
    }

    const set = ORDER_SET_MAP.get(order);

    if (set === undefined) {
      await interaction.reply({
        content: `I can't seem to find your order, are you sure you put it correctly?
You can check on ${SHIP_WHEN_CHANNEL} on the progress of orders.`,
        ephemeral: true,
      });
      return;
    }

    console.log(
      `User @${interaction.user.id} asked about order #${order} in set ${
        Object.entries(SlimeSet).find(([, value]) => value === set)?.[0] ?? set
      }`,
    );

    const shipment = SHIPMENTS.findIndex((x) => x[set] >= order);
    const shipped = SHIPPED_SHIPMENTS.has(shipment);

    if (shipment === -1) {
      await interaction.reply({
        content: `Your order hasn't been manufactured yet.
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
You can check on ${
          SHIPMENT_MESSAGE[shipment]
        } on the progress of the shipment.`,
        ephemeral: true,
      });
      return;
    }

    await interaction.reply({
      content: `Your order is being made currently, it's shipment ${
        shipment + 2
      }!
You can check on ${
        SHIPMENT_MESSAGE[shipment]
      } to see when it's going to get shipped.`,
      ephemeral: true,
    });
  },
};

// Index of shipped shipment
const SHIPPED_SHIPMENTS = new Set([0, 1]);

// Link to shipment message
const SHIPMENT_MESSAGE = [
  "https://discord.com/channels/817184208525983775/1129107343058153623/1129110457953812651",
  "https://discord.com/channels/817184208525983775/1129107343058153623/1129117575721267290",
  "https://discord.com/channels/817184208525983775/1129107343058153623/1130164280537391154",
];

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
  {
    [SlimeSet.LOWER_BODY_PURPLE]: 130241,
    [SlimeSet.LOWER_BODY_BLACK]: 130241,
    [SlimeSet.LOWER_BODY_WHITE]: 130241,
    [SlimeSet.CORE_PURPLE]: 130241,
    [SlimeSet.CORE_BLACK]: 130241,
    [SlimeSet.CORE_WHITE]: 130241,
    [SlimeSet.ENHANCED_CORE_PURPLE]: 130241,
    [SlimeSet.ENHANCED_CORE_BLACK]: 130241,
    [SlimeSet.ENHANCED_CORE_WHITE]: 130241,
    [SlimeSet.FULLBODY_PURPLE]: 130241,
    [SlimeSet.FULLBODY_BLACK]: 130241,
    [SlimeSet.FULLBODY_WHITE]: 130241,
    [SlimeSet.DELUXE_TRACKER_PURPLE]: 130241,
    [SlimeSet.DELUXE_TRACKER_BLACK]: 130241,
    [SlimeSet.DELUXE_TRACKER_WHITE]: 130241,
  },
];
