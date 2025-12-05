/**
 * Unity WebGL Express Server
 * This server hosts Unity WebGL builds with proper handling of Brotli (.br) and Gzip (.gz) compressed files.
 * It sets appropriate Content-Type and Content-Encoding headers, and applies caching strategies for static assets.
 * Additionally, it implements Content Security Policy (CSP) with SHA-256 hashes for WebGL files.
 */
const express = require('express');
const path = require('node:path');
const fs = require('node:fs');
const app = express();

const PORT = process.env.PORT || 8080;
const BUILD_PATH = path.join(__dirname, 'webgl');

const ALLOWED_ORIGINS = [
  'http://localhost:3000',
  'https://anquilosaurios-development-frontend-bwcbgzf6byefdthz.eastus-01.azurewebsites.net',
  'http://20.168.245.216'
];

// Load the WebGL manifest with file hashes
let webglManifest = {};
const manifestPath = path.join(__dirname, 'webgl-manifest.json');

try {
  if (fs.existsSync(manifestPath)) {
    const manifestData = fs.readFileSync(manifestPath, 'utf8');
    webglManifest = JSON.parse(manifestData);
    console.log('WebGL manifest loaded successfully');
    console.log(`Found ${Object.keys(webglManifest.files || {}).length} files with hashes`);
  } else {
    console.warn('webgl-manifest.json not found. CSP hashes will not be applied.');
  }
} catch (error) {
  console.error('Error loading webgl-manifest.json:', error.message);
}

/**
 * Helper function to get the file hash from manifest
 * @param {string} requestPath - The request path
 * @returns {string|null} - The hash if found, null otherwise
 */
function getFileHash(requestPath) {
  let filePath = requestPath.startsWith('/') ? requestPath.substring(1) : requestPath;
  
  // Remove /webgl/ prefix if present
  if (filePath.startsWith('webgl/')) {
    filePath = filePath.substring(6);
  }
  
  if (filePath.endsWith('.br') || filePath.endsWith('.gz')) {
    filePath = filePath.slice(0, -3);
  }
  
  return webglManifest.files?.[filePath] || null;
}

/**
 * Middleware to configure security and cross-origin headers for Unity WebGL hosting.
 * Also applies CSP with file-specific SHA-256 hashes.
 *
 * Sets headers to:
 * - Allow embedding only from the specified frontend origin via Content-Security-Policy.
 * - Remove X-Frame-Options to avoid iframe restrictions.
 * - Enforce secure cross-origin isolation for WebAssembly and SharedArrayBuffer usage.
 * - Enable CORS for the specified frontend origin with allowed methods and headers.
 * - Apply CSP with SHA-256 hashes for WebGL script files.
 *
 * @function
 * @name securityHeadersMiddleware
 * @param {express.Request} req - Incoming HTTP request object.
 * @param {express.Response} res - HTTP response object used to set headers.
 * @param {express.NextFunction} next - Callback to pass control to the next middleware.
 */
app.use((req, res, next) => {
  // Get file hash if available
  const fileHash = getFileHash(req.path);
  
  // Build CSP header
  let cspParts = [];
  
  // Frame ancestors
  if (ALLOWED_ORIGINS.length > 0) {
    cspParts.push(`frame-ancestors ${ALLOWED_ORIGINS.join(' ')}`);
  } else {
    cspParts.push("frame-ancestors 'none'");
  }
  
  // Add script-src with hash if this is a JS or WASM file
  if (fileHash && (req.path.endsWith('.js') || req.path.endsWith('.js.br') || req.path.endsWith('.js.gz'))) {
    cspParts.push(`script-src 'self' '${fileHash}' 'unsafe-eval'`);
    console.log(`Applied CSP hash to: ${req.path} -> ${fileHash}`);
  } else if (fileHash && (req.path.endsWith('.wasm') || req.path.endsWith('.wasm.br') || req.path.endsWith('.wasm.gz'))) {
    cspParts.push(`script-src 'self' '${fileHash}' 'unsafe-eval'`);
    cspParts.push(`script-src-elem 'self' '${fileHash}'`);
    console.log(`Applied CSP hash to WASM: ${req.path} -> ${fileHash}`);
  }
  
  res.setHeader('Content-Security-Policy', cspParts.join('; '));
  res.removeHeader('X-Frame-Options');
  res.setHeader('Cross-Origin-Resource-Policy', 'cross-origin');

  const origin = req.headers.origin;
  if (origin && ALLOWED_ORIGINS.includes(origin)) {
    res.setHeader('Access-Control-Allow-Origin', origin);
  } else if (ALLOWED_ORIGINS.length === 1) {
    res.setHeader('Access-Control-Allow-Origin', ALLOWED_ORIGINS[0]);
  }
  
  res.setHeader('Access-Control-Allow-Methods', 'GET, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type');
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

app.get('/webgl-manifest.json', (req, res) => {
  if (Object.keys(webglManifest.files || {}).length === 0) {
    return res.status(503).json({
      error: 'Manifest not available',
      message: 'The integrity manifest has not been loaded or is empty'
    });
  };
  
  res.json(webglManifest);
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
 * Health check endpoint to verify server status and manifest loading
 */
app.get('/health', (req, res) => {
  res.json({
    status: 'healthy',
    timestamp: new Date().toISOString(),
    manifestLoaded: Object.keys(webglManifest.files || {}).length > 0,
    filesWithHashes: Object.keys(webglManifest.files || {}).length,
    allowedOrigins: ALLOWED_ORIGINS
  });
});

/** 
 * Starts the Express server and logs the active port and build path.
 */
app.listen(PORT, () => {
    console.log(`
    Unity WebGL Server
    Port: ${PORT}
    Build Path: ${BUILD_PATH.split('/').pop()}
    CSP Hashes: ${(Object.keys(webglManifest.files || {}).length > 0 ? 'ENABLED' : 'DISABLED')}
    Allowed Origins: ${ALLOWED_ORIGINS.length}
    `);

    if (ALLOWED_ORIGINS.length > 0) {
        console.log('Allowed origins:');
        ALLOWED_ORIGINS.forEach(origin => console.log(`- ${origin}`));
    }
});