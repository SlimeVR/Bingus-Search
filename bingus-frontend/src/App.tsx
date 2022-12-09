import { ThemeProvider } from "@emotion/react";
import {
  Button,
  Container,
  createTheme,
  CssBaseline,
  Stack,
  TextField,
  Typography,
  useMediaQuery,
} from "@mui/material";
import { useMemo, useState } from "react";
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

  const resultCard = function (text: string, relevance: number | null = null) {
    return (
      <Container
        disableGutters
        sx={{
          bgcolor: "primary.dark",
          borderRadius: 2,
        }}
      >
        <Stack padding={1} spacing={1.5} direction="row">
          {relevance ? (
            <Typography
              variant="caption"
              color="secondary.contrastText"
              sx={{
                width: "fit-content",
                height: "fit-content",
                bgcolor: "primary.main",
                boxShadow: 3,
                borderRadius: 2,
                padding: 1,
              }}
            >
              {relevance.toFixed()}%
            </Typography>
          ) : (
            <></>
          )}
          <Typography paragraph variant="body1" color="primary.contrastText">
            {text}
          </Typography>
        </Stack>
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
            ? lastResults
                ?.sort((a, b) => (a.relevance <= b.relevance ? 1 : -1))
                .map((result) => resultCard(result.text, result.relevance))
            : resultCard("No results...")}
        </Stack>
      </Container>
    </ThemeProvider>
  );
}

export default App;
