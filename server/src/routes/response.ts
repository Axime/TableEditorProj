import ErrorCodes, { ServerError } from '../error.js';
import Logger from '../log.js';

export abstract class ResponseBase<T> {
  constructor(
    public ok: boolean,
    public error: ErrorCodes | null,
    public errorDescription: string | null,
    public response: T,
  ) { }
  public toString() {
    return JSON.stringify(this);
  }
}

export class SuccessfulResponse<T> extends ResponseBase<T> {
  constructor(response: T) {
    super(true, null, null, response);
  }
}

export class FailedResponse extends ResponseBase<null> {
  constructor(...args: [error: ErrorCodes, errorDescription: string] | [error: ServerError]) {
    const [error, errorDescription] = args.length === 2 ? args : [args[0].code, args[0].message];
    super(false, error, errorDescription, null);
  }
}