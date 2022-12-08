#if DEBUG
using System;

namespace TableLanguage {
  internal class Test {
    public static void Main(string[] args) {
      string c =
      "let i = .45; const a = {" +
        "[key + 1 * 2] = {" +
          "innerKey=1;" +
        "};" +
      "};";
      var a = Lang.LexicalAnalysis(c);
      Console.WriteLine(c);
      foreach (var t in a) {
        Console.WriteLine($" Text: {t.text}\tpos: {t.pos}\ttype: {t.type.name}");
      }
      var b = Lang.Parse(a);
      //Console.WriteLine($"\t\t{((Lang.Nodes.NumberNode)((Lang.Statements.DeclarationStatement)b[0]).init!).v}");
      Console.WriteLine(b);

    }
  }
}
#endif