const { env } = require('process');

const target = env.ASPNETCORE_HTTPS_PORT ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}` :
  env.ASPNETCORE_URLS ? env.ASPNETCORE_URLS.split(';')[0] : 'http://localhost:7036';


/*
const socketTarget = env.ASPNETCORE_HTTPS_PORT ? `wss://localhost:${env.ASPNETCORE_HTTPS_PORT}` :
  env.ASPNETCORE_URLS ? env.ASPNETCORE_URLS.split(';')[0] : 'wss://localhost:7036';
console.log([env.ASPNETCORE_HTTPS_PORT, env.ASPNETCORE_URLS]);
console.log([target, socketTarget]);
*/
const PROXY_CONFIG = [
  {
    context: [
      "/Core/**",
      "/Administration/**"
    ],
    target: target,
    secure: false,
    headers: {
      Connection: 'Keep-Alive'
    }
  }/*,
  {
    context: [
      "/notifications"
    ],
    target: socketTarget,
    secure: false,
    headers: {
      Connection: 'Keep-Alive'
    },
    ws: true
  }*/
];

module.exports = PROXY_CONFIG;
