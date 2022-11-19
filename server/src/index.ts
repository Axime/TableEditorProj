import {
  createServer,
  type RequestListener
} from 'http';
import env, {
  isDebug
} from './env.js';
import {
  requestListener,
} from './route.js';
import './routes/index.js';

const server = createServer(requestListener as RequestListener);

server.listen(...[(
  isDebug
    ? [3000, 'localhost'] as const
    : [8100, '::'] as const
)] as const);
