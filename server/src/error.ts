enum ErrorCodes {
  InternalServerError = 0,
  MethodDoesNotExist = 1,
  MissingParams = 2,
  HttpMethodNotAllowed = 3,
  // auth.registration
  AuthRegistration__UserWithSuchUsernameAlreadyExist = 100,
  AuthRegistration__PasswordsDoNotMatch = 101,
  // auth.login
  AuthLogin__InvalidCredentials = 150
}
export class ServerError extends Error {
  constructor(msg: string, public code: ErrorCodes) {
    super(msg);
  }
}
export default ErrorCodes;
