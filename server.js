const express = require('express');
const path = require('path');
const app = express();

const PORT = process.env.PORT || 8080;
const BUILD_PATH = path.join(__dirname, 'webgl');

app.get('*.br', (req, res, next) => {
    if (req.url.endsWith('.data.br')) {
        res.set('Content-Type', 'application/octet-stream');
    } else if (req.url.endsWith('.wasm.br')) {
        res.set('Content-Type', 'application/wasm');
    } else if (req.url.endsWith('.js.br')) {
        res.set('Content-Type', 'application/javascript');
    }
    
    res.set('Content-Encoding', 'br');
    next();
});

app.get('*.gz', (req, res, next) => {
    if (req.url.endsWith('.data.gz')) {
        res.set('Content-Type', 'application/octet-stream');
    } else if (req.url.endsWith('.js.gz')) {
        res.set('Content-Type', 'application/javascript');
    }
    
    res.set('Content-Encoding', 'gzip');
    next();
});

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