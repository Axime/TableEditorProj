import { ServerError } from '../../error.js';
import Logger from '../../log.js';
import AuthService from '../../service/auth.js';
import { FailedResponse, ResponseBase, SuccessfulResponse } from '../response.js';
import {
  Methods,
  RegisterRoute, Route
} from '../route.js';

interface RegistrationRequiredProps {
  username: string
  password: string
  passwordRepeat: string
  keyword: string
}
const prefix = Logger.colorString(Logger.Colors.DarkGreen, '[Registration]', true);
RegisterRoute(new Route<RegistrationRequiredProps>(
  '/api/auth.registration',
  (req, res) => {
    try {
      Logger.debugLog(
        '%s: Запрос на регистрацию',
        prefix,
        );
      const {
        keyword, username,
        password, passwordRepeat
      } = req.body;
      AuthService.register({
        keyword, username,
        password, passwordRepeat,
      });
      res.sendResponse(new SuccessfulResponse({
        success: true,
      }));
    } catch (e) {
      Logger.debugLog('%s: %O', prefix, e);
      res.sendResponse(e instanceof ResponseBase ? e : new FailedResponse(e as ServerError));
    }
  },
  [Methods.post],
  ['username', 'password', 'passwordRepeat', 'keyword'])
);