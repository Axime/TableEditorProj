export class DbError extends Error {
  constructor(msg: string) {
    super(msg);
    this.name = 'DbError'
  }
}

export class DocumentWithSuchPrimaryKeyAlreadyExist extends DbError {
  constructor() {
    super('Document with such values of primary keys already exists');
  }
}