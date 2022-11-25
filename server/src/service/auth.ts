import UserModel from '../db/model/user.js';
import UserService from './user.js';
import { createHash as sha256 } from 'crypto';
import ErrorCodes, { ServerError } from '../error.js';
import jwt from 'jsonwebtoken';

class AuthError extends ServerError {
  constructor(msg: string, errorCode: ErrorCodes) {
    super(msg, errorCode);
    this.name = 'Auth Error';
  }
}

const enum ErrorMessages {
  UserAlreadyExist = 'User with such username already exists',
  PasswordsDontMatch = 'Passwords don\'t match',
  InvalidCredentials = 'Invalid credentials'
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
    if (possibleUser) throw new AuthError(ErrorMessages.UserAlreadyExist, ErrorCodes.AuthRegistration__UserWithSuchUsernameAlreadyExist);
    if (password !== passwordRepeat) throw new AuthError(ErrorMessages.PasswordsDontMatch, ErrorCodes.AuthRegistration__PasswordsDoNotMatch);

    const hashedPassword = createHash(password);
    const newUser = UserModel.create({
      keyword,
      password: hashedPassword,
      username
    });
    newUser.save();
    return newUser;
  }
  export enum AccessType {
    User, Admin, Dev
  }
  export function login(username: string, password: string) {
    const user = UserService.findByUsername(username)?.toObject();
    if (!user || user.password !== createHash(password)) throw new AuthError(ErrorMessages.InvalidCredentials, ErrorCodes.AuthLogin__InvalidCredentials);
    return {
      token: jwt.sign({
        username: user.username,
        keyword: user.keyword
      }, createHash('Hello world'), {
        algorithm: 'HS384',
      }),
      accessType: AccessType.User
    }
  }
}

export default AuthService;
