import { ThemeProvider } from "@emotion/react";
import {
  Card,
  CardContent,
  CardHeader,
  CircularProgress,
  Container,
  createTheme,
  CssBaseline,
  IconButton,
  Link,
  Skeleton,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { useMemo, useRef, useState } from "react";
import MuiMarkdown from "mui-markdown";
import SearchIcon from "@mui/icons-material/Search";
import LightModeIcon from "@mui/icons-material/LightMode";
import DarkModeIcon from "@mui/icons-material/DarkMode";

export type Result = {
  relevance: number;
  matchedQuestion: string;
  title: string;
  text: string;
};

function relevanceToElevation(relevance: number, scale = 24): number {
  return Math.round((relevance / 100) * scale);
}

interface ResultCardProps {
  readonly relevance: number;
  readonly title: string;
  readonly text: string;
}

function ResultCard(props: ResultCardProps) {
  return (
    <Card
      key={props.title}
      variant="elevation"
      elevation={1 + relevanceToElevation(props.relevance, 5)}
      sx={{ width: "100%" }}
    >
      <CardHeader
        title={props.title}
        subheader={`${props.relevance.toFixed()}% relevant`}
        sx={{ pb: 1 }}
        titleTypographyProps={{ sx: { typography: { sm: "h5", xs: "h6" } } }}
      />
      <CardContent sx={{ pt: 0 }}>
        <MuiMarkdown>{props.text}</MuiMarkdown>
      </CardContent>
    </Card>
  );
}

function App() {
  const localTheme = localStorage.getItem("user-theme");
  const systemDarkMode = matchMedia("(prefers-color-scheme: dark)");

  const [prefersDarkMode, setPrefersDarkMode] = useState(
    localTheme ? localTheme === "dark" : systemDarkMode.matches,
  );
  if (!localTheme) {
    systemDarkMode.onchange = (ev) => {
      !localStorage.getItem("user-theme") &&
        setPrefersDarkMode(() => ev.matches);
    };
  }

  const theme = useMemo(
    () =>
      createTheme({
        components: {
          MuiTypography: {
            styleOverrides: {
              root: {
                wordBreak: "break-word",
              },
            },
          },
        },
        palette: {
          mode: prefersDarkMode ? "dark" : "light",
        },
      }),
    [prefersDarkMode],
  );

  const url = new URL(window.location.href);
  const urlInput = url.searchParams.get("q") ?? "";

  const urlInputSearched = useRef(false);
  const [input, setInput] = useState(urlInput);
  const [lastSearch, setLastSearch] = useState("");

  const [loadingResults, setLoadingResults] = useState(false);
  const [searchResults, setSearchResults] = useState<Result[] | undefined>(
    undefined,
  );

  const updateUrl = function () {
    window.history.replaceState({}, window.name, url.toString());
  };

  const queryBingus = async (query: string, responseCount = 30) => {
    const url = new URL("https://bingus.slimevr.io/faq/search");

    setLoadingResults(true);

    url.search = new URLSearchParams({
      question: query,
      responseCount: responseCount.toFixed().toString(),
    }).toString();

    return fetch(url)
      .then((response) => response.json())
      .finally(() => setLoadingResults(false));
  };

  const search = async () => {
    if (input === lastSearch) {
      return;
    }

    // Clear last search results
    setSearchResults(undefined);

    if (!input || !/\S/.test(input)) {
      setSearchResults(undefined);
      setLastSearch(input);

      url.searchParams.delete("q");
      updateUrl();

      return;
    }

    url.searchParams.set("q", input);
    updateUrl();

    setSearchResults(await queryBingus(input));
    setLastSearch(input);
  };

  if (!urlInputSearched.current) {
    urlInputSearched.current = true;
    search();
  }

  const toggleTheme = async () => {
    setPrefersDarkMode((value) => {
      const newValue = !value;
      localStorage.setItem("user-theme", newValue ? "dark" : "light");
      return newValue;
    });
  };

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />

      <Container>
        <Stack useFlexGap direction="column" py={2} spacing={2}>
          <IconButton
            id="themeToggleButton"
            aria-label="Theme Toggle Button"
            onClick={toggleTheme}
            sx={{
              width: "fit-content",
              height: "fit-content",
              alignSelf: "end",
            }}
          >
            {prefersDarkMode ? (
              <DarkModeIcon fontSize="inherit" />
            ) : (
              <LightModeIcon fontSize="inherit" />
            )}
          </IconButton>

          <Typography
            noWrap
            align="center"
            variant="h1"
            fontSize={{ md: 64, sm: 56, xs: 48 }}
            fontWeight={700}
          >
            Bingus
          </Typography>
          <Typography
            align="center"
            variant="subtitle1"
            fontSize={14}
            fontWeight={500}
            color="textSecondary"
          >
            Information may not be up-to-date. If you need further help, join
            the <Link href="https://discord.gg/SlimeVR">SlimeVR Discord</Link>.
          </Typography>

          <TextField
            fullWidth
            id="searchBar"
            aria-label="Search Bar"
            label="Ask a question..."
            autoFocus
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyUp={(e) => {
              if (e.key === "Enter") search();
            }}
            slotProps={{
              input: {
                endAdornment: loadingResults ? (
                  <CircularProgress />
                ) : (
                  <IconButton
                    id="searchButton"
                    aria-label="Search Button"
                    onClick={search}
                  >
                    <SearchIcon fontSize="inherit" />
                  </IconButton>
                ),
              },
            }}
          />

          <Stack spacing={2} alignItems="center" direction="column">
            {loadingResults
              ? [...Array(30)].map((_, i) => (
                  <Skeleton
                    key={i}
                    variant="rounded"
                    width="100%"
                    height={128}
                  />
                ))
              : searchResults
                  ?.sort((a, b) => (a.relevance <= b.relevance ? 1 : -1))
                  ?.map((result) => (
                    <ResultCard
                      key={result.title}
                      relevance={result.relevance}
                      title={result.title}
                      text={result.text}
                    />
                  )) || (
                  <Typography color="text.secondary" padding={1}>
                    No results...
                  </Typography>
                )}
          </Stack>
        </Stack>
      </Container>
    </ThemeProvider>
  );
}

export default App;
