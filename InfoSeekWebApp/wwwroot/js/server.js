// express server setup
const express = require('express');
const app = express();
const path = require('path');

// Configurar las cabeceras HTTP para Cross-Origin-Opener-Policy y Cross-Origin-Embedder-Policy
app.use((req, res, next) => {
    res.setHeader('Cross-Origin-Opener-Policy', 'same-origin');
    res.setHeader('Cross-Origin-Embedder-Policy', 'require-corp');
    next();
});

// Servir archivos estáticos desde la carpeta 'wwwroot'
app.use(express.static(path.join(__dirname, '../wwwroot')));

// Configuración del puerto y arranque del servidor
const PORT = process.env.PORT || 5023;
app.listen(PORT, () => {
    console.log(`Servidor escuchando en el puerto ${PORT}`);
});
