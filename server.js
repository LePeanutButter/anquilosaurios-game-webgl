const express = require('express');
const path = require('path');
const app = express();

app.use(express.static(path.join(__dirname, 'build'), {
    setHeaders: (res, filePath) => {
        if (filePath.endsWith('.wasm')) {
            res.setHeader('Content-Type', 'application/wasm');
        }
        if (filePath.endsWith('.unityweb')) {
            res.setHeader('Content-Encoding', 'gzip');
        }
    }
}));

app.get('*', (req, res) => {
    res.sendFile(path.join(__dirname, 'build', 'index.html'));
});

app.listen(process.env.PORT || 3000);