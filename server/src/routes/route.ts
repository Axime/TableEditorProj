import {
  type IncomingMessage,
  type ServerResponse,
  STATUS_CODES,
} from 'http';
import parseBody from '../body-parser.js';
import ErrorCodes from '../error.js';
import { FailedResponse, ResponseBase, SuccessfulResponse } from './response.js';

type RequestListenerFn<T = null> = (req: IncomingMessage & {
  body: T
}, res: ServerResponse & {
  sendResponse: (response: ResponseBase<any>, statusCode?: number) => void;
}) => void

export const enum Methods {
  get = 'GET',
  post = 'POST',
  put = 'PUT',
  delete = 'DELETE'
}

export class Route<T extends Record<string, any> | null = null> {
  constructor(
    public path: string | ((url: string) => boolean),
    private listener: RequestListenerFn<T>,
    public methods: Methods[],
    public requiredParams: string[]
  ) { }

  public checkPath(path: string): boolean {
    return typeof this.path === 'string' ? this.path === path : this.path(path);
  }

  public handleRequest: RequestListenerFn<T> = (req, res) => {
    // Check for method is valid

    const method = req.method! as Methods;
    if (!this.methods.includes(method)) return res.sendResponse(
      new FailedResponse(
        ErrorCodes.HttpMethodNotAllowed,
        `HTTP method "${method}". Please, use one of this methods: ${this.methods.join(', ')}`
      ),
      405
    );
    // check body for required props
    const body = req.body;
    if (method === Methods.get) return this.listener(req, res); // call listener because 'body' doesn't exist in "GET"
    const missingParams = body ? this.requiredParams.filter(prop => !(prop in (body as object))) : this.requiredParams;
    if (!missingParams.length) return this.listener(req, res);
    const paramsDescription = `\
Missing params: ${missingParams.join(', ')}\
${body ? '\nCurrent params:\n' + [...Object.entries(body)].map(([k, v]) => `${k} = ${v.toString()}`).join('\n') : ''}`;
    res.sendResponse(new FailedResponse(ErrorCodes.MissingParams, paramsDescription), 400);
  }
}

const routes: Route[] = [];

export const RegisterRoute = (route: Route<any>): void => void routes.push(route);
export const findRoute = (path: string) => routes.find(route => route.checkPath(path)) || null;


export const requestListener: RequestListenerFn<any> = async (req, res) => {
  console.log(`[Главный обработчик]: входящий запрос, url${req.url}`)
  res.sendResponse = function (response, statusCode = response instanceof SuccessfulResponse ? 200 : 400) {
    this.writeHead(statusCode, STATUS_CODES[statusCode]);
    this.write(response.toString());
    this.end();
  };
  try {
    res
      .setHeader('Access-Control-Allow-Methods', 'GET, POST')
      .setHeader('Access-Control-Allow-Origin', '*')
      .setHeader('Connection', 'close')
      .setHeader('Content-Type', 'application/json');
    const route = findRoute(req.url!);
    if (!route) return void res.writeHead(404, STATUS_CODES[404]) as void;
    if (req.method === Methods.post) req.body = await parseBody(req);
    else req.body = null;
    route.handleRequest(req, res);

  } catch (e) {
    console.error(e);
    res.sendResponse(new FailedResponse(ErrorCodes.InternalServerError, 'Internal server error'), 500);
  } finally {
    if (!res.closed) res.end();
  }
}
