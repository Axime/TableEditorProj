import {
  v5 as uid,
  v4
} from 'uuid'; import Schema from './schema.js';
export default class Document<T extends Record<string | symbol, any> = {}, S extends Schema<T, (keyof T)[]> = Schema<T, (keyof T)[]>> {
/*   private get $path() {
    return join(process.cwd(), 'server', 'data', `${this.$schema.name}s`, this.$name);
  } */
  private $schema: S;
  public $data: T;
  public $new: boolean;
  public $edited: boolean = false;
  public $name: string;
  public $deleted: boolean = false;

  constructor(data: T, usingCreate: boolean, schema: S, name?: string) {
    this.$data = data;
    this.$schema = schema;
    this.$new = usingCreate;
    this.$name = name ?? uid(v4(), schema.name);
    return new Proxy(this, {
      get(doc, prop) {
        if (Object.hasOwn(doc, prop)) return doc[prop as keyof typeof doc];
        return doc.$data[prop];
      },
      set(doc, prop, value) {
        if (Object.hasOwn(doc, prop)) doc[prop as keyof typeof doc] = value;
        else if (prop in doc.$data) {
          doc.$data = value;
          doc.$edited = true;
        }
        return true;
      }
    });
  }

  public save() {
    this.$schema.save(this);
  }

  public forceSave() {
    this.$edited = true;
    this.save();
  }

  public delete() {
    this.$deleted = true;
    this.$schema.delete(this);
  }

  public toObject() {
    return this.$data;
  }
}

