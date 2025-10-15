const express = require('express');
const expressStaticGzip = require('express-static-gzip');
const path = require('path');

const app = express();
const root = path.join(__dirname, 'build');

app.use('/', expressStaticGzip(root, {
  enableBrotli: true,
  orderPreference: ['br', 'gz'],
  setHeaders: (res, path) => {
    if (path.endsWith('.wasm')) {
      res.setHeader('Content-Type', 'application/wasm');
    }
    res.setHeader('Cache-Control', 'public, max-age=31536000, immutable');
  }
}));

app.get('*', (req, res) => {
  res.sendFile(path.join(root, 'index.html'));
});

const port = process.env.PORT || 8080;
app.listen(port, () => console.log(`Server listening on ${port}`));
