const express = require('express');
const compression = require('compression');
const path = require('path');

const app = express();
const PORT = process.env.PORT || 8080; // Azure usa la variable de entorno PORT

// Directorio donde Unity WebGL deposita los archivos (wwwroot)
const BUILD_PATH = path.join(__dirname, 'site/wwwroot');

// --- Middleware para servir archivos comprimidos (Brotli/Gzip) ---
// La librería 'compression' automáticamente revisa si existe un archivo .br o .gz
// al lado del archivo original, y si existe, lo sirve con el header Content-Encoding correcto.
// Ojo: Unity genera archivos Brotli (.br) si lo configuras así en las Build Settings.
app.use(compression({
    // Filtro para incluir los tipos de archivos de Unity WebGL si es necesario,
    // aunque por defecto 'compression' maneja bien los mime-types comunes.
    filter: (req, res) => {
        // Habilita la compresión solo si el cliente lo acepta (Header Accept-Encoding)
        if (req.headers['x-no-compression']) {
            return false;
        }
        return compression.filter(req, res);
    },
    level: 9, // Nivel de compresión (no afecta a los archivos pre-comprimidos de Unity)
    // Azure App Service de Node.js ya maneja Content-Encoding.
    // Usamos compression para que sirva los archivos pre-comprimidos.
}));

// --- Servir archivos estáticos ---
// Es crucial que esto esté DESPUÉS del middleware de 'compression'.
app.use(express.static(BUILD_PATH, {
    // Configuraciones de cache
    setHeaders: (res, path, stat) => {
        // Establecer headers de cache para archivos del build
        if (path.endsWith('.unityweb') || path.endsWith('.br') || path.endsWith('.gz') || path.endsWith('.js') || path.endsWith('.wasm')) {
            res.set('Cache-Control', 'public, max-age=31536000, immutable'); // Cache por 1 año para archivos inmutables
        } else if (path.endsWith('.json')) {
            res.set('Cache-Control', 'public, max-age=86400'); // Cache por 1 día para archivos de configuración
        } else if (path.endsWith('index.html')) {
            res.set('Cache-Control', 'public, max-age=60'); // Poco cache para el index.html
        }
    },
}));

// --- Iniciar el Servidor ---
app.listen(PORT, () => {
    console.log(`Unity WebGL Server running on port ${PORT}`);
    console.log(`Serving files from: ${BUILD_PATH}`);
});