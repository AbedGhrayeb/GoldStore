const http = require('node:http');
const { readFile } = require('node:fs');
const { join, extname, normalize } = require('node:path');

const ROOT = join(__dirname, '..', 'dist', 'client', 'browser');
const MIME = {
  '.html': 'text/html',
  '.js': 'text/javascript',
  '.css': 'text/css',
  '.json': 'application/json',
  '.woff2': 'font/woff2',
  '.png': 'image/png',
  '.svg': 'image/svg+xml',
};

const server = http.createServer((req, res) => {
  let path = normalize(decodeURIComponent(req.url.split('?')[0]));
  if (path === '/' || path === '\\') path = '/index.html';
  const file = join(ROOT, path);
  if (!file.startsWith(ROOT)) {
    res.writeHead(403);
    res.end();
    return;
  }
  readFile(file, (err, data) => {
    if (err) {
      res.writeHead(404);
      res.end('not found');
      return;
    }
    res.writeHead(200, {
      'Content-Type': MIME[extname(file)] ?? 'application/octet-stream',
      'Cache-Control': 'no-cache',
    });
    res.end(data);
  });
});

server.listen(4300, () => console.log('static server on 4300'));
