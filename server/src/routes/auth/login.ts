import {
  RegisterRoute,
  Methods, Route
} from '../route.js';
import {
  FailedResponse, SuccessfulResponse
} from '../response.js';
import Logger from '../../log.js';
import { ServerError } from '../../error.js';
import AuthService from '../../service/auth.js';

interface LoginBodyParams {
  password: string;
  username: string
}
const prefix = Logger.colorString(Logger.Colors.DarkGreen, '[Login]');
RegisterRoute(new Route<LoginBodyParams>('/api/auth.login',
  (req, res) => {
    try {
      Logger.debugLog(
        '%s: Запрос на регистрацию',
        prefix
      );
      const {
        password, username
      } = req.body;
      const data = AuthService.login(username, password);
      res.sendResponse(new SuccessfulResponse(data));
    } catch (e) {
      if (e instanceof ServerError) {
        Logger.debugLog('%s: %O', prefix, e);
        res.sendResponse(new FailedResponse(e))
      }
    }
  },
  [Methods.post],
  ['username', 'password'])
);