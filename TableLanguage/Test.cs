#if DEBUG
using System;

namespace TableLanguage {
  internal class Test {
    public static void Main() {
      string c =
      //"const numbers = [[1, 2, 3], 2, 3, 4][1];\n" +
      "do let a = (function(arg1, arg2){})(1, 2);\n" +
      "while ((function a(arg1){})(1)); {\n" +
      "if (a == 4) {\n" +
      "  let a = 5;\n" +
      "} else if (some.prop !== 2) {\n" +
      "  let a = 6;\n" +
      "} else if (otherCondition()) {\n" +
      "  const a = 4;" +
      "} else {\n" +
      "  const void = console.log(\"Nothing\");\n" +
      "}}\n" +
      "for (let i = 0; i < 5; i += 1) {\n" +
      "  console.log(i);\n" +
      "}" +
      "//let i = !!!number.slice((1,1), 3, 5 + 5) + a['filter'](isEven) + !!!!5;\n" +
      "function sum(x, y) {\n" +
      "  const s = x + y;\n" +
      "  return s;\n" +
      "}" +
      "";
      Console.WriteLine("Raw code:\n=======================================");
      Console.WriteLine(c);
      Console.WriteLine("=======================================");
      var a = Lang.LexicalAnalysis(c);
      //foreach (var t in a) {
      //  Console.WriteLine($" Text: {t.text}\tpos: {t.pos}\ttype: {t.type.name}");
      //}
      var b = Lang.Parse(a);
      Console.WriteLine("\nCode from parser:\n" +
      "=======================================");
      //Console.WriteLine($"\t\t{((Lang.Nodes.NumberNode)((Lang.Statements.DeclarationStatement)b[0]).init!).v}");
      foreach (var node in b) {
        Lang.PrintNode(node);
        Console.WriteLine();
      }
      Console.WriteLine("=======================================");
    }
  }
}
#endif