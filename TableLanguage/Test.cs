#if DEBUG
using System;

namespace TableLanguage {
  internal class Test {
    public static void Main() {
      string c =
      "const numbers = [[1, 2, 3], 2, 3, 4][1];\n" +
      //"let a = 5 + !!5;" +
      "let i = !!!number.slice((1,1), 3, 5 + 5) + a['filter'](isEven) + !!!!5;\n" +
      //"function n(arg,arg2) {}" +
      "";
      var a = Lang.LexicalAnalysis(c);
      Console.WriteLine(c);
      //foreach (var t in a) {
      //  Console.WriteLine($" Text: {t.text}\tpos: {t.pos}\ttype: {t.type.name}");
      //}
      var b = Lang.Parse(a);
      //Console.WriteLine($"\t\t{((Lang.Nodes.NumberNode)((Lang.Statements.DeclarationStatement)b[0]).init!).v}");
      foreach (var node in b) {
        Console.WriteLine();
        Lang.PrintNode(node);
      }
    }
  }
}
#endif