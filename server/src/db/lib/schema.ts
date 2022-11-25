import Document from './document.js';

import {
  join
} from 'path';
import {
  brotliCompressSync,
  brotliDecompressSync,
} from 'zlib';
import {
  writeFileSync,
  readFileSync,
  unlink,
  existsSync,
  mkdirSync,
} from 'fs';
import { DocumentWithSuchPrimaryValuesAlreadyExist } from './error.js';
import Logger from '../../log.js';

interface MapElement<T, P extends (keyof T)[]> {
  file: string;
  props: {
    [K in keyof Pick<T, P[number]>]: T[K];
  }
}

type Doc<T extends Record<string, any>, S extends Schema<T, (keyof T)[]>> = Document<T, S> & T;


class FileMap<T extends Record<string, any>, P extends (keyof T)[]> {
  constructor(private $pathToDir: string, public mapPropName: P) {
    this.#load();
  }

  public update(doc: Document<T>) {
    if (doc.$deleted && doc.$new) return;
    if (doc.$deleted) this.#delete(doc);
    if (doc.$new) this.#add(doc);
    this.#write();
  }

  get #mapPath() { return join(this.$pathToDir, 'map.json'); }
  #map: MapElement<T, P>[] = [];
  #load() {
    (path => {
      if (!existsSync(path)) mkdirSync(path);
    })(join(process.cwd(), 'server', 'data'));
    if (!existsSync(this.$pathToDir)) mkdirSync(this.$pathToDir);
    if (!existsSync(this.#mapPath)) writeFileSync(this.#mapPath, '[]', {
      encoding: 'utf-8',
    })
    this.#map = JSON.parse(readFileSync(this.#mapPath, 'utf-8') || '[]');
  }
  #write() {
    try {
      writeFileSync(this.#mapPath, JSON.stringify(this.#map), {
        encoding: 'utf-8',
      });
    } catch (e) {
      if (e) console.error(e);
    }
  }
  #add(doc: Document<T>) {
    this.#map.push({
      file: doc.$name,
      props: Object.fromEntries(
        Object
          .entries(doc.$data)
          .filter(([k]) => this.mapPropName.includes(k))
      ) as MapElement<T, P>['props']
    });
  }
  #delete(doc: Document<T>) {
    this.#map.splice(
      this.#map.findIndex(el => el.file === doc.$name),
      1
    );
  }
  public find(filter: Partial<T>) {
    return this.#map.filter(({ props }) => {
      for (const key in filter) {
        if (key in props && filter[key] !== props[key]) return false;
      }
      return true;
    }).map(el => el.file);
  }
  public checkIfDocumentWithSamePrimaryValuesExists(doc: Document<T>): boolean {
    const data = doc.$data;
    return !!this.#map.find(mapDoc => this.mapPropName.every(prop => mapDoc.props[prop] === data[prop]));
  }
}

export default class Schema<T extends Record<string, any>, P extends (keyof T)[]> {
  public constructor(name: string, mapPropName: P) {
    this.name = name;
    this.#map = new FileMap<T, P>(this.$pathToDir, mapPropName);
  }
  public name: string
  public static get DataDirPath() {
    return join(process.cwd(), 'server', 'data');
  }
  private get $pathToDir() {
    return join(Schema.DataDirPath, `${this.name}s`);
  }
  // Used for searching of documents
  readonly #map: FileMap<T, P>;
  readonly #cache = new Map<string, T>;
  #readDoc(name: string): Doc<T, this> {
    return new Document(
      this.#cache.has(name)
        ? this.#cache.get(name)!
        : (data => {
          this.#cache.set(name, data);
          return data;
        })(JSON.parse(
          brotliDecompressSync(
            readFileSync(join(this.$pathToDir, name))
          ).toString('utf-8')
        ) as T),
      false,
      this, name
    ) as Doc<T, this>;
  }

  #writeDoc(doc: Document<T>) {
    try {
      writeFileSync(join(this.$pathToDir, doc.$name), brotliCompressSync(JSON.stringify(doc.$data)));
      this.#cache.set(doc.$name, doc.$data);
    } catch (err) {
      if (err) Logger.error(err as Error);
    }
  }
  #deleteDoc(doc: Document<T>) {
    unlink(join(this.$pathToDir, doc.$name), (err) => {
      if (err) Logger.error(err)
    });
  }


  public save(doc: Document<T>) {
    if (!doc.$edited && !doc.$new || doc.$deleted) return false;
    if (this.#map.checkIfDocumentWithSamePrimaryValuesExists(doc)) throw new DocumentWithSuchPrimaryValuesAlreadyExist();
    this.#map.update(doc);
    this.#writeDoc(doc);
    return true;
  }

  public create(data: T): Doc<T, this> {
    return new Document(data, true, this) as Doc<T, this>;
  }

  public delete(doc: Document<T>) {
    this.#map.update(doc);
    this.#deleteDoc(doc);
  }

  public find(filter: Partial<T>): Doc<T, this>[] {
    const docs = this.#map.find(filter);
    return docs.map(doc => this.#readDoc(doc))
  }
}
