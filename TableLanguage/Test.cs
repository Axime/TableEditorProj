#if DEBUG
using System;

namespace TableLanguage {
  internal class Test {
    public static void Main() {
      string c =
      "let i = (!false) + (Array.isArray)(a()(5); 4);" +
      ""
      ;
      var a = Lang.LexicalAnalysis(c);
      Console.WriteLine(c);
      //foreach (var t in a) {
      //  Console.WriteLine($" Text: {t.text}\tpos: {t.pos}\ttype: {t.type.name}");
      //}
      var b = Lang.Parse(a);
      //Console.WriteLine($"\t\t{((Lang.Nodes.NumberNode)((Lang.Statements.DeclarationStatement)b[0]).init!).v}");
      Lang.PrintNode(b[0]);
      //Console.WriteLine(b);

    }
  }
}
#endif