namespace API {
  public static class APITable {
    public enum Types {
      @int,
      @float,
      @duble,
      @byte,
      @string,
      @bool,

    }

    public class Cell {
      public Cell(int collumn, int row, object value = null) {
        this.collumn = collumn; this.row = row; this.value = value;
      }

      public int collumn; public int row;
      public object value;

      public static Cell GetCell(int collumn, int row) {
        Cell cell = new(collumn, row);
        return cell;
      }
    }

    public static class Methods {
      public static bool SetValue<T, A>(A adress, T value) => true;
    }
  }
}
