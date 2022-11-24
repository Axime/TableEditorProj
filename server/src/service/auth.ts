import UserModel from '../db/model/user.js';
import UserService from './user.js';
import { createHash as sha256 } from 'crypto';
import ErrorCodes, { ServerError } from '../error.js';
class AuthError extends ServerError {
  constructor(msg: string, errorCode: ErrorCodes) {
    super(msg, errorCode);
    this.name = 'Auth Error';
  }
}
class RegistrationError extends AuthError {
  constructor(msg: string, code: ErrorCodes) {
    super(msg, code);
  }
}

namespace AuthService {

  function createHash(data: string) {
    return sha256('sha256')
      .update(data
        .split('')
        .reduce(
          (str, cur, ind) =>
            ind % 6
              ? `${str}${cur}`
              : `${str}${cur}${((ind + 5) % 10)}`,
          ''
        ), 'utf-8')
      .digest('hex');
  }

  interface RegistrationParams {
    username: string;
    password: string;
    passwordRepeat: string;
    keyword: string;
  }
  export function register(params: RegistrationParams) {
    const {
      keyword, username,
      password, passwordRepeat,
    } = params;

    const possibleUser = UserService.findByUsername(username);
    if (possibleUser) throw new RegistrationError('User with such username already exists', ErrorCodes.AuthRegistration__UserWithSuchUsernameAlreadyExist);
    if (password !== passwordRepeat) throw new RegistrationError('Passwords don\'t match', ErrorCodes.AuthRegistration__PasswordsDoNotMatch);

    const hashedPassword = createHash(password);
    const newUser = UserModel.create({
      keyword,
      password: hashedPassword,
      username
    });
    newUser.save();
    return newUser;
  }
}

export default AuthService;
