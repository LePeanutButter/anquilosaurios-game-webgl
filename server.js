/**
 * Unity WebGL Express Server
 * This server hosts Unity WebGL builds with proper handling of Brotli (.br) and Gzip (.gz) compressed files.
 * It sets appropriate Content-Type and Content-Encoding headers, and applies caching strategies for static assets.
 */
require('dotenv').config();

const express = require('express');
const path = require('path');
const app = express();
const FRONT_ORIGIN = process.env.FRONT_ORIGIN;

/**
 * @constant {number} PORT - The port number the server listens on.
 * Defaults to 8080 if no environment variable is set.
 */
const PORT = process.env.PORT || 8080;

/**
 * @constant {string} BUILD_PATH - Absolute path to the Unity WebGL build directory.
 */
const BUILD_PATH = path.join(__dirname, 'webgl');

/**
 * Middleware to configure security and cross-origin headers for Unity WebGL hosting.
 *
 * Sets headers to:
 * - Allow embedding only from the specified frontend origin via Content-Security-Policy.
 * - Remove X-Frame-Options to avoid iframe restrictions.
 * - Enforce secure cross-origin isolation for WebAssembly and SharedArrayBuffer usage.
 * - Enable CORS for the specified frontend origin with allowed methods and headers.
 *
 * @function
 * @name securityHeadersMiddleware
 * @param {express.Request} req - Incoming HTTP request object.
 * @param {express.Response} res - HTTP response object used to set headers.
 * @param {express.NextFunction} next - Callback to pass control to the next middleware.
 */
app.use((req, res, next) => {
  res.setHeader(
    "Content-Security-Policy",
    `frame-ancestors ${FRONT_ORIGIN};`
  );

  res.removeHeader("X-Frame-Options");

  res.setHeader("Cross-Origin-Opener-Policy", "same-origin");
  res.setHeader("Cross-Origin-Embedder-Policy", "require-corp");

  res.setHeader("Access-Control-Allow-Origin", FRONT_ORIGIN);
  res.setHeader("Access-Control-Allow-Methods", "GET, OPTIONS");
  res.setHeader("Access-Control-Allow-Headers", "Content-Type");

  next();
});

/**
 * Middleware for handling Brotli-compressed files (.br).
 * Sets appropriate Content-Type based on file extension and applies Brotli encoding.
 *
 * @param {express.Request} req - The incoming request object.
 * @param {express.Response} res - The response object used to set headers.
 * @param {express.NextFunction} next - Callback to pass control to the next middleware.
 */
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

/**
 * Middleware for handling Gzip-compressed files (.gz).
 * Sets appropriate Content-Type based on file extension and applies Gzip encoding.
 *
 * @param {express.Request} req - The incoming request object.
 * @param {express.Response} res - The response object used to set headers.
 * @param {express.NextFunction} next - Callback to pass control to the next middleware.
 */
app.get('*.gz', (req, res, next) => {
    if (req.url.endsWith('.data.gz')) {
        res.set('Content-Type', 'application/octet-stream');
    } else if (req.url.endsWith('.js.gz')) {
        res.set('Content-Type', 'application/javascript');
    }
    
    res.set('Content-Encoding', 'gzip');
    next();
});

/**
 * Serves static files from the Unity WebGL build directory.
 * Applies caching strategies based on file type:
 * - Long-term caching for immutable assets (.unityweb, .br, .gz, .js, .wasm)
 * - 1-day caching for JSON files
 * - 1-minute caching for index.html
 *
 * @param {express.Response} res - The response object used to set headers.
 * @param {string} path - The file path being served.
 * @param {object} stat - File statistics object.
 */
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

/**
 * Starts the Express server and logs the active port and build path.
 */
app.listen(PORT, () => {
    console.log(`Unity WebGL Server running on port ${PORT}`);
    console.log(`Serving files from: ${BUILD_PATH}`);
});