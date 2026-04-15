import globals from "globals";
import base from "../eslint.config.mts";
import { defineConfig } from "eslint/config";

export default defineConfig([
  base,
  {
    languageOptions: {
      globals: {
        ...globals.node,
      },
      ecmaVersion: "latest",
      sourceType: "module",
    },
  },
]);
