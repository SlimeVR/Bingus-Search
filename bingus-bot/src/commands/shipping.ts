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
      return [
        orderNo,
        { set: STRING_SET_MAP[cols[1]], date: new Date(`${cols[8]} UTC`) },
      ];
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

    const orderInfo = ORDER_SET_MAP.get(order);

    if (orderInfo === undefined) {
      await interaction.reply({
        content: `I can't seem to find your order, are you sure you put it correctly?
You can check on ${SHIP_WHEN_CHANNEL} on the progress of orders.`,
        ephemeral: true,
      });
      return;
    }

    console.log(
      `User ${interaction.user} asked about order #${order} in set ${
        Object.entries(SlimeSet).find(
          ([, value]) => value === orderInfo.set,
        )?.[0] ?? orderInfo
      }`,
    );

    const shipment = SHIPMENTS.findIndex((shipment) => {
      const maxOrder = shipment[orderInfo.set];
      return maxOrder !== undefined && maxOrder >= order;
    });

    if (shipment === -1) {
      await interaction.reply({
        content: `There is no news about your order yet.
  You can check on ${SHIP_WHEN_CHANNEL} on the progress of orders.`,
        ephemeral: true,
      });
      return;
    }

    if (SHIPPED_SHIPMENTS.has(shipment)) {
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

    if (MANUFACUTRED_SHIPMENTS.has(shipment)) {
      await interaction.reply({
        content: `Your order is being made currently, it's shipment ${
          shipment + 2
        }!
You can check on ${
          SHIPMENT_MESSAGE[shipment]
        } to see when it's going to get shipped.`,
        ephemeral: true,
      });
      return;
    }

    await interaction.reply({
      content: `Your order hasn't been manufactured yet, but it's scheduled to be in shipment ${
        shipment + 2
      }!
You can check on ${SHIP_WHEN_CHANNEL} to see when it's going to get shipped.`,
      ephemeral: true,
    });
  },
};

// Shipment number is offset by 2 (ex 7 = 9)
const MANUFACUTRED_SHIPMENTS = new Set([0, 1, 2, 3, 4, 5, 6, 7, 8]);

// Index of shipped shipment
const SHIPPED_SHIPMENTS = new Set([0, 1, 2, 3, 4, 5, 6, 7]);

// Link to shipment message
const SHIPMENT_MESSAGE = [
  "https://discord.com/channels/817184208525983775/1129107343058153623/1129110457953812651", // 2
  "https://discord.com/channels/817184208525983775/1129107343058153623/1129117575721267290", // 3
  "https://discord.com/channels/817184208525983775/1129107343058153623/1130164280537391154", // 4
  "https://discord.com/channels/817184208525983775/1129107343058153623/1183816649950888066", // 5
  "https://discord.com/channels/817184208525983775/1129107343058153623/1195340407962349651", // 6
  "https://discord.com/channels/817184208525983775/1129107343058153623/1204216527402836042", // 7
  "https://discord.com/channels/817184208525983775/1129107343058153623/1242966024886812722", // 8
  "https://discord.com/channels/817184208525983775/1129107343058153623/1253778746838876191", // 9
  "https://discord.com/channels/817184208525983775/1129107343058153623/1286353083282231356", // 10
];

// Shipments being prepared or shipped
const SHIPMENTS = [
  { // 2
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
  { // 3
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
  { // 4
    [SlimeSet.LOWER_BODY_PURPLE]: 145034,
    [SlimeSet.LOWER_BODY_BLACK]: 146417,
    [SlimeSet.LOWER_BODY_WHITE]: 143933,
    [SlimeSet.CORE_PURPLE]: 148676,
    [SlimeSet.CORE_BLACK]: 143948,
    [SlimeSet.CORE_WHITE]: 147485,
    [SlimeSet.ENHANCED_CORE_PURPLE]: 131278,
    [SlimeSet.ENHANCED_CORE_BLACK]: 142973,
    [SlimeSet.ENHANCED_CORE_WHITE]: 132923,
    [SlimeSet.FULLBODY_PURPLE]: 136423,
    [SlimeSet.FULLBODY_BLACK]: 148429,
    [SlimeSet.FULLBODY_WHITE]: 131751,
    [SlimeSet.DELUXE_TRACKER_PURPLE]: 145844,
    [SlimeSet.DELUXE_TRACKER_BLACK]: 143353,
    [SlimeSet.DELUXE_TRACKER_WHITE]: 150693,
  },
  { // 5
    [SlimeSet.LOWER_BODY_PURPLE]: 145755,
    [SlimeSet.LOWER_BODY_BLACK]: 147499,
    [SlimeSet.LOWER_BODY_WHITE]: 144215,
    [SlimeSet.CORE_PURPLE]: 149260,
    [SlimeSet.CORE_BLACK]: 147021,
    [SlimeSet.CORE_WHITE]: 149361,
    [SlimeSet.ENHANCED_CORE_PURPLE]: 135028,
    [SlimeSet.ENHANCED_CORE_BLACK]: 144888,
    [SlimeSet.ENHANCED_CORE_WHITE]: 132976,
    [SlimeSet.FULLBODY_PURPLE]: 156056,
    [SlimeSet.FULLBODY_BLACK]: 149324,
    [SlimeSet.FULLBODY_WHITE]: 152199,
    [SlimeSet.DELUXE_TRACKER_PURPLE]: 145844,
    [SlimeSet.DELUXE_TRACKER_BLACK]: 143353,
    [SlimeSet.DELUXE_TRACKER_WHITE]: 150693,
  },
  { // 6
    [SlimeSet.LOWER_BODY_PURPLE]: 151381,
    [SlimeSet.LOWER_BODY_WHITE]: 156589,
    [SlimeSet.LOWER_BODY_BLACK]: 150573,
    [SlimeSet.CORE_PURPLE]: 150209,
    [SlimeSet.CORE_WHITE]: 154975,
    [SlimeSet.CORE_BLACK]: 149945,
    [SlimeSet.ENHANCED_CORE_PURPLE]: 162865,
    [SlimeSet.ENHANCED_CORE_WHITE]: 141349,
    [SlimeSet.ENHANCED_CORE_BLACK]: 154233,
    [SlimeSet.FULLBODY_PURPLE]: 156432,
    [SlimeSet.FULLBODY_WHITE]: 167790,
    [SlimeSet.FULLBODY_BLACK]: 149681,
    [SlimeSet.DELUXE_TRACKER_PURPLE]: 147390,
    [SlimeSet.DELUXE_TRACKER_WHITE]: 150693,
    [SlimeSet.DELUXE_TRACKER_BLACK]: 147109,
  },
  { // 7
    [SlimeSet.LOWER_BODY_PURPLE]: 151381,
    [SlimeSet.LOWER_BODY_WHITE]: 163329,
    [SlimeSet.LOWER_BODY_BLACK]: 150573,
    [SlimeSet.CORE_PURPLE]: 158595,
    [SlimeSet.CORE_WHITE]: 163583,
    [SlimeSet.CORE_BLACK]: 152037,
    [SlimeSet.ENHANCED_CORE_PURPLE]: 162865,
    [SlimeSet.ENHANCED_CORE_WHITE]: 161215,
    [SlimeSet.ENHANCED_CORE_BLACK]: 154233,
    [SlimeSet.FULLBODY_PURPLE]: 166695,
    [SlimeSet.FULLBODY_WHITE]: 169072,
    [SlimeSet.FULLBODY_BLACK]: 151247,
    [SlimeSet.DELUXE_TRACKER_PURPLE]: 158906,
    [SlimeSet.DELUXE_TRACKER_WHITE]: 167795,
    [SlimeSet.DELUXE_TRACKER_BLACK]: 166425,
  },
  { // 8
    [SlimeSet.LOWER_BODY_PURPLE]: 179075,
    [SlimeSet.LOWER_BODY_WHITE]: 163329,
    [SlimeSet.LOWER_BODY_BLACK]: 162280,
    [SlimeSet.CORE_PURPLE]: 163405,
    [SlimeSet.CORE_WHITE]: 163583,
    [SlimeSet.CORE_BLACK]: 165195,
    [SlimeSet.ENHANCED_CORE_PURPLE]: 189897,
    [SlimeSet.ENHANCED_CORE_WHITE]: 163725,
    [SlimeSet.ENHANCED_CORE_BLACK]: 163765,
    [SlimeSet.FULLBODY_PURPLE]: 177650,
    [SlimeSet.FULLBODY_WHITE]: 169072,
    [SlimeSet.FULLBODY_BLACK]: 151247,
    [SlimeSet.DELUXE_TRACKER_PURPLE]: 162039,
    [SlimeSet.DELUXE_TRACKER_WHITE]: 167795,
    [SlimeSet.DELUXE_TRACKER_BLACK]: 166425,
  },
  { // 9
    [SlimeSet.LOWER_BODY_PURPLE]: 197569,
    [SlimeSet.LOWER_BODY_WHITE]: 170895,
    [SlimeSet.LOWER_BODY_BLACK]: 172511,
    [SlimeSet.CORE_PURPLE]: 196811,
    [SlimeSet.CORE_WHITE]: 169108,
    [SlimeSet.CORE_BLACK]: 169286,
    [SlimeSet.ENHANCED_CORE_PURPLE]: 197224,
    [SlimeSet.ENHANCED_CORE_WHITE]: 169961,
    [SlimeSet.ENHANCED_CORE_BLACK]: 168770,
    [SlimeSet.FULLBODY_PURPLE]: 198922,
    [SlimeSet.FULLBODY_WHITE]: 169072,
    [SlimeSet.FULLBODY_BLACK]: 175321,
    [SlimeSet.DELUXE_TRACKER_PURPLE]: 174567,
    [SlimeSet.DELUXE_TRACKER_WHITE]: 167795,
    [SlimeSet.DELUXE_TRACKER_BLACK]: 168457,
  },
  { // 10
    [SlimeSet.LOWER_BODY_PURPLE]: 228278,
    [SlimeSet.CORE_PURPLE]: 221041,
    [SlimeSet.ENHANCED_CORE_PURPLE]: 221323,
    [SlimeSet.FULLBODY_PURPLE]: 221642,
    [SlimeSet.DELUXE_TRACKER_PURPLE]: 224055,
  },
];
