import UserModel, { IUser } from '../db/model/user.js';

namespace UserService {

  export function findByUsername(username: string) {
    return UserModel.find({
      username
    })[0] ?? null;
  }
}

export default UserService;
