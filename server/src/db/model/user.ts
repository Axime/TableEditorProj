import Schema from '../lib/schema.js';

export interface IUser {
  username: string;
  password: string;
  keyword: string;
}

const UserModel = new Schema<IUser, ['username']>('user', ['username']);

export default UserModel;
