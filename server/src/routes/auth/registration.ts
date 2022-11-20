import { SuccessfulResponse } from '../response.js';
import {
  Methods,
  RegisterRoute, Route
} from '../route.js';

interface RegistrationRequiredProps {
  username: string
  password: string
  passwordRepeat: string
}

RegisterRoute(new Route<RegistrationRequiredProps>(
  '/api/auth.registration',
  (req, res) => {
    // TODO: Add registration route
    console.log("[Registration]: Запрос на регистрацию");
    res.sendResponse(new SuccessfulResponse({
      123: 'Hello',
      Hello: 'world!'
    }));
  },
  [Methods.post],
  ['username', 'password', 'passwordRepeat'])
);
