import {
  createServer,
  type RequestListener
} from 'http';
import getEnv, {
  isDebug
} from './env.js';
import {
  requestListener,
} from './routes/route.js';
import './routes/index.js';

const server = createServer(requestListener as RequestListener);

server.listen(isDebug ? {
  port: 3000
} : {
  port: 8100,
  ipv6Only: true,
});
