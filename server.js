const express = require('express');
const expressStaticGzip = require('express-static-gzip');
const path = require('path');

const app = express();

const assetsPath = path.join(__dirname, 'build', 'Build');

app.use('/Build', expressStaticGzip(assetsPath, {
  enableBrotli: true,
  orderPreference: ['br', 'gz'],
  serveStatic: {
    setHeaders: (res, filePath) => {
      if (filePath.endsWith('.wasm')) {
        res.setHeader('Content-Type', 'application/wasm');
      }
      res.setHeader('Cache-Control', 'public, max-age=31536000, immutable');
    }
  }
}));

app.get('*', (req, res) => {
  res.sendFile(path.join(__dirname, 'build', 'index.html'));
});

const port = process.env.PORT || 8080;
app.listen(port, () => console.log(`Server listening on port ${port}`));
