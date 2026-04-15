import { EmojiIdentifierResolvable } from "discord.js";
import winkNLP from "wink-nlp";
import model from "wink-eng-lite-web-model";

export const BINGUS_EMOJI: EmojiIdentifierResolvable =
  "<:bingus:1157717351861596200>";
export const NYAGUN_EMOJI: EmojiIdentifierResolvable =
  "<:nya_gun:957426272030576671>";
export const GUNNYA_EMOJI: EmojiIdentifierResolvable =
  "<:gun_nya:962115210670383184>";
export const BINGUSGUN_EMOJI: EmojiIdentifierResolvable =
  "<:bingus_gun:1404234276630958080>";
export const NIGHTYGUN_EMOJI: EmojiIdentifierResolvable =
  "<:nighty_gun:1314209484440338474>";
export const LANGUAGE_EMOJI: EmojiIdentifierResolvable =
  "<:langwidjnom:1055961639842750534>";
export const QUESTION_EMOJI: EmojiIdentifierResolvable =
  "<:nighty_question:1314209482133209088>";

export const MISC_EMOJIS: EmojiIdentifierResolvable[] = [
  "<:nya_a:847203539352551544>",
  "<:nya_umu:850498715617198080>",
  "<:pcbnom:833469925980110908>",
  "<:slime_wink:1341418353801166953>",
  BINGUS_EMOJI,
];

export function getRandomMiscEmoji(): EmojiIdentifierResolvable {
  return MISC_EMOJIS[Math.floor(Math.random() * MISC_EMOJIS.length)];
}

const POSITIVE_EMOJIS: EmojiIdentifierResolvable[] = [
  "<:Heart:1117832451620868136>",
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
  "<:nighty_hug:1314209493747241011>",
  "<:nighty_heart:1314209486390427659>",
  "<:nighty_nom:1314209503276699708>",
  "<:nighty_yay:1319261631217143910>",
  "<:slime_wow:1341418344544211045>",
];

export function getRandomPositiveEmoji(): EmojiIdentifierResolvable {
  return POSITIVE_EMOJIS[Math.floor(Math.random() * POSITIVE_EMOJIS.length)];
}

const MISC_AND_POSITIVE_EMOJIS = [...POSITIVE_EMOJIS, ...MISC_EMOJIS];

export function getRandomPositiveOrMiscEmoji(): EmojiIdentifierResolvable {
  return MISC_AND_POSITIVE_EMOJIS[
    Math.floor(Math.random() * MISC_AND_POSITIVE_EMOJIS.length)
  ];
}

export const NEGATIVE_EMOJIS: EmojiIdentifierResolvable[] = [
  QUESTION_EMOJI,
  NYAGUN_EMOJI,
  GUNNYA_EMOJI,
  BINGUSGUN_EMOJI,
  NIGHTYGUN_EMOJI,
  "<:spicypillownom:839133143793139733>",
  "<:nya_flop:847202537425731604>",
  "<:nighty_cry:1314209498554175578>",
  "<:nighty_flop:1314209488429121556>",
  "<:slime_angy:1341418349472518175>",
  "<:nighty_a:1314209496029204572>",
  "<:bingus_sad:1398416913222336582>",
  "😭",
  "😢",
  "😓",
  "💀",
  "😈",
  "💥",
];

export function getRandomNegativeEmoji(): EmojiIdentifierResolvable {
  return NEGATIVE_EMOJIS[Math.floor(Math.random() * NEGATIVE_EMOJIS.length)];
}

export const ALWAYS_REACT_CHANNELS = new Set([
  // #updates
  "818062236492759050",
  // #small-updates
  "907246949005148170",
]);

export const MEDIA_REACT_CHANNELS = new Set([
  // #slimevr-media
  "903962635161174076",
  // #diy-gallery
  "855164207615705118",
  // #art
  "880402436387397652",
]);

const nlp = winkNLP(model, ["negation", "sentiment"]);
const { its } = nlp;

export function getSentimentEmoji(
  msg: string,
  positiveThreshold: number = 0.35,
  negativeThreshold: number = -0.4,
): EmojiIdentifierResolvable | undefined {
  const sentiment = nlp.readDoc(msg).out(its.sentiment);
  if (typeof sentiment === "string") return;

  if (sentiment >= positiveThreshold) {
    return getRandomPositiveEmoji();
  } else if (sentiment <= negativeThreshold) {
    return getRandomNegativeEmoji();
  }
}

export const MAGIC_8_BALL_ANSWERS: string[] = [
  "It is certain",
  "It is decidedly so",
  "Without a doubt",
  "Yes definitely",
  "You may rely on it",
  "As I see it, yes",
  "Most likely",
  "Outlook good",
  "Yes",
  "Signs point to yes",
  "Reply hazy, try again",
  "Ask again later",
  "Better not tell you now",
  "Cannot predict now",
  "Concentrate and ask again",
  "Don't count on it",
  "My reply is no",
  "My sources say no",
  "Outlook not so good",
  "Very doubtful",
];

export function getMagic8BallAnswer(): string {
  return MAGIC_8_BALL_ANSWERS[
    Math.floor(Math.random() * MAGIC_8_BALL_ANSWERS.length)
  ];
}
