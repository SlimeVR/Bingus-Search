import { useMemo, useState } from "react";
import { ThemeProvider } from "@emotion/react";
import {
  useMediaQuery,
  createTheme,
  CssBaseline,
  Typography,
  Container,
  TextField,
  Button,
  Stack,
} from "@mui/material";
import { bake_cookie, read_cookie } from "sfcookies";

function App() {
  const cookieTheme = read_cookie("user-theme");
  const systemDarkMode = useMediaQuery("(prefers-color-scheme: dark)");

  const [prefersDarkMode, setPrefersDarkMode] = useState(
    cookieTheme.length ? cookieTheme === "dark" : systemDarkMode
  );

  const theme = useMemo(
    () =>
      createTheme({
        palette: {
          mode: prefersDarkMode ? "dark" : "light",
        },
      }),
    [prefersDarkMode]
  );

  const [input, setInput] = useState("");
  const [lastResults, setLastResults] = useState<
    [{ relevance: number; title: string; text: string }] | null
  >(null);

  const queryBingus = async (query: string, responseCount: number = 5) => {
    const url = new URL("https://bingus.bscotch.ca/api/faq/search");

    url.search = new URLSearchParams({
      question: query,
      responseCount: responseCount.toFixed().toString(),
    }).toString();

    return fetch(url).then((response) => response.json());
  };

  const search = async () => {
    const results = await queryBingus(input);
    setLastResults(results);

    console.log(results);
  };

  const toggleTheme = async () => {
    setPrefersDarkMode((value) => {
      const newValue = !value;
      bake_cookie("user-theme", newValue ? "dark" : "light");
      return newValue;
    });
  };

  const resultCard = function (text: string) {
    return (
      <Container
        sx={{
          bgcolor: "primary.dark",
          padding: 1.5,
          borderRadius: 1,
          "&:hover": { bgcolor: "primary.main" },
        }}
      >
        <Typography>{text}</Typography>
      </Container>
    );
  };

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />

      <Stack alignItems="end" direction="column-reverse" sx={{ padding: 2 }}>
        <Button variant="contained" onClick={toggleTheme}>
          {prefersDarkMode ? "Dark" : "Light"}
        </Button>
      </Stack>

      <Container maxWidth="md" sx={{ my: 3 }}>
        <Typography variant="h4" align="center">
          Bingus Search
        </Typography>

        <Stack spacing={1} direction="row" sx={{ my: 3 }}>
          <TextField
            fullWidth
            label="Ask a question..."
            variant="filled"
            onChange={(e) => setInput(e.target.value)}
            onKeyPress={(e) => {
              if (e.key === "Enter") search();
            }}
          />
          <Button onClick={search} variant="contained">
            Search
          </Button>
        </Stack>

        <Stack
          spacing={2}
          alignItems="center"
          direction="column"
          sx={{ my: 3 }}
        >
          {lastResults?.length
            ? lastResults?.map((result) => resultCard(result.text))
            : resultCard("No results...")}
        </Stack>
      </Container>
    </ThemeProvider>
  );
}

export default App;
