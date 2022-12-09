import React from 'react';
import { ThemeProvider } from '@emotion/react';
import { useMediaQuery, AppBar, createTheme, CssBaseline, Link, Toolbar, Typography } from '@mui/material';

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
  
  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <AppBar
        position="absolute"
        color="default"
        elevation={0}
        sx={{
          position: 'relative',
          borderBottom: (t) => `1px solid ${t.palette.divider}`,
        }}
      >
        <Toolbar>
          <Typography variant="h6" color="inherit" noWrap>
            <Link href='/' underline="none" color="inherit">Bingus Search</Link>
          </Typography>
        </Toolbar>
      </AppBar>
    </ThemeProvider>
  );
}

export default App;
