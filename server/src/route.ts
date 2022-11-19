import {
  type IncomingMessage,
  type ServerResponse,
  STATUS_CODES,
} from 'http';
import parseBody from './body-parser.js';


export class Route<T = null> {
  constructor(
    public path: string | ((url: string) => boolean),
    public listener: (req: IncomingMessage & {
      body: T
    }, res: ServerResponse) => void
  ) { }

  public checkPath(path: string): boolean {
    return typeof this.path === 'string' ? this.path === path : this.path(path);
  }
}

const routes: Route[] = [];

export const addRoute = (route: Route): void => void routes.push(route);
export const findRoute = (path: string) => routes.find(route => route.checkPath(path)) || null;


export const requestListener = (req: IncomingMessage & {
  body: any,
}, res: ServerResponse
) => {
  try {
    res
      .setHeader('Access-Control-Allow-Methods', 'GET, POST')
      .setHeader('Connection', 'close');
    (async route => {
      if (!route) return void res.writeHead(404, STATUS_CODES[404]) as void;
      if (req.method === 'POST') req.body = await parseBody(req);
      else req.body = null;
      route.listener(req, res);
    })(findRoute(`${req.headers.host}${req.url}`));
  } catch (e) {
    console.error(e);
    res.writeHead(
      500,
      STATUS_CODES[500], [
      'Content-Type', 'application/json',
    ]).write(JSON.stringify({
      ok: false,
      error: 0,
      errorDescription: 'Внутренняя ошибка сервера'
    }))
  } finally {
    res.end();
  }
}
