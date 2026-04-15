import globals from "globals";
import pluginReact from "eslint-plugin-react";
import css from "@eslint/css";
import base from "../eslint.config.mts";
import { defineConfig } from "eslint/config";

export default defineConfig([
  base,
  pluginReact.configs.flat.recommended,
  { files: ["**/*.css"], plugins: { css }, language: "css/css", extends: ["css/recommended"] },
  {
    languageOptions: {
      globals: {
        ...globals.browser,
      },
      ecmaVersion: "latest",
      sourceType: "module",
    },

    settings: {
      react: {
        version: "detect",
      },
    },
  }
]);
