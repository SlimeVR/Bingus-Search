import React from 'react';
import { ThemeProvider } from '@emotion/react';
import { useMediaQuery, createTheme, CssBaseline, Typography, Container, TextField, Button, Stack } from '@mui/material';

function App() {
  const prefersDarkMode = useMediaQuery('(prefers-color-scheme: dark)');

  const theme = React.useMemo(
    () =>
      createTheme({
        palette: {
          mode: prefersDarkMode ? 'dark' : 'light',
        },
      }),
    [prefersDarkMode],
  );

  const queryBingus = async () => {
    const results = fetch("https://bingus.bscotch.ca/faq/search?question=test");
    console.log(results);
  }

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <Container maxWidth="md" sx={{ my: 8 }}>
        <Typography variant="h4" align="center">
          Bingus Search
        </Typography>

        <Stack spacing={1} direction="row" sx={{ my: 3 }}>
          <TextField fullWidth label="Ask a question..." variant="outlined" />
          <Button onClick={queryBingus} variant="outlined">Search</Button>
        </Stack>

        <Stack spacing={2} direction="column" sx={{ my: 3 }}>
          Test
        </Stack>
      </Container>
    </ThemeProvider>
  );
}

export default App;
