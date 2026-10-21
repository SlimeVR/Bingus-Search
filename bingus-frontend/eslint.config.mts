import globals from "globals";
import css from "@eslint/css";
import base from "../eslint.config.mts";
import { defineConfig } from "eslint/config";

export default defineConfig([
  base,
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

    rules: {
      "react/react-in-jsx-scope": "off",
    },
  }
]);
