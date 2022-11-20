import ErrorCodes from '../error.js';

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
  constructor(error: ErrorCodes, errorDescription: string) {
    super(false, error, errorDescription, null);
  }
}