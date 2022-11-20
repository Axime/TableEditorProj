import Schema from '../lib/schema.js';

enum CellType {
  int, float, string
}

interface Cell {
  type: CellType;
  value: string
}

export interface ITable {
  username: string;
  name: string;
  cells: Cell[];
}
const TableModel = new Schema<ITable, ['username', 'name']>('table', ['username', 'name']);

export default TableModel;