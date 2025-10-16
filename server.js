const express = require('express');
const compression = require('compression');
const path = require('path');

const app = express();
const PORT = process.env.PORT || 8080;
const BUILD_PATH = path.join(__dirname, 'webgl');

app.use(compression({
    filter: (req, res) => {
        if (req.headers['x-no-compression']) {
            return false;
        }
        return compression.filter(req, res);
    },
    level: 9,
}));

app.use(express.static(BUILD_PATH, {
    setHeaders: (res, path, stat) => {
        if (path.endsWith('.unityweb') || path.endsWith('.br') || path.endsWith('.gz') || path.endsWith('.js') || path.endsWith('.wasm')) {
            res.set('Cache-Control', 'public, max-age=31536000, immutable');
        } else if (path.endsWith('.json')) {
            res.set('Cache-Control', 'public, max-age=86400');
        } else if (path.endsWith('index.html')) {
            res.set('Cache-Control', 'public, max-age=60');
        }
    },
}));

app.listen(PORT, () => {
    console.log(`Unity WebGL Server running on port ${PORT}`);
    console.log(`Serving files from: ${BUILD_PATH}`);
});