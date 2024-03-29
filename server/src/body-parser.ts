import {
  type IncomingMessage
} from 'http';

export default async function parseBody(req: IncomingMessage) {
  return new Promise<string | null>(res => {
    let data = '';
    req
      .on('data', chunk => data += chunk)
      .on('end', () => res(data ? JSON.parse(data) : null));
  });
}