import Document from './document.js';

import {
  join
} from 'path';
import {
  brotliCompress,
  type CompressCallback,
  brotliDecompressSync,
} from 'zlib';
import {
  writeFile,
  readFileSync,
  unlink
} from 'fs';

interface MapElement<T, P extends (keyof T)[]> {
  file: string;
  props: {
    [K in keyof Pick<T, P[number]>]: T[K];
  }
}


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
    this.#map = JSON.parse(readFileSync(this.#mapPath, 'binary') || '[]');
  }
  #write() {
    writeFile(this.#mapPath, JSON.stringify(this.#map), {
      encoding: 'binary',
    }, err => {
      if (err) console.error(err);
    });
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
  #map: FileMap<T, P>;

  #readDoc(name: string): Document<T> {
    return new Document(
      JSON.parse(
        brotliDecompressSync(
          readFileSync(join(this.$pathToDir, name))
        ).toString('utf-8')
      ),
      false,
      this, name
    );
  }

  #writeDoc(doc: Document<T>) {
    brotliCompress(JSON.stringify(doc.$data), (err, res) => {
      if (err) return console.error(err);
      writeFile(join(this.$pathToDir, doc.$name), res, () => void 0);
    });
  }
  #deleteDoc(doc: Document<T>) {
    unlink(join(this.$pathToDir, doc.$name), (err) => {
      if (err) console.error(err)
    });
  }


  public save(doc: Document<T>) {
    if (!doc.$edited && !doc.$new || doc.$deleted) return false;
    this.#map.update(doc);
    this.#writeDoc(doc)
    return true;
  }

  public create(data: T): Document<T, this> {
    return new Document(data, true, this);
  }

  public delete(doc: Document<T>) {
    this.#map.update(doc);
    this.#deleteDoc(doc);
  }

  public find(filter: Partial<T>) {
    const docs = this.#map.find(filter);
    return docs.map(doc => this.#readDoc(doc))
  }
}
