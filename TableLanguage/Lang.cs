using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

#if DEBUG
using T = System.Collections.Generic.List<TableLanguage.Lang.Token>;
using N = System.Collections.Generic.List<TableLanguage.Lang.Node>;
#endif

namespace TableLanguage {
  public static class Lang {
    public class SyntaxError : Exception {
      public int? pos;
      public SyntaxError(string msg, int? pos = null) :
        base($"{msg}{(pos != null ? $" at position {pos}" : "")}") {
        this.pos = pos;
      }
    }
#if DEBUG
    public
#endif
    abstract class Node {
      public string Name => GetType().Name;
    }
#if DEBUG
    public
#endif
    static class Statements {
      public class Block : Node {
        public List<Node> nodes;
        public Block(List<Node> nodes) {
          this.nodes = nodes;
        }
      }

      public class BinaryExpression : Node {
        public Nodes.OperatorNode op;
        public Node left;
        public Node right;
        public BinaryExpression(Nodes.OperatorNode op, Node left, Node right) {
          this.op = op;
          this.left = left;
          this.right = right;
        }
      }
      public class UnaryExpression : Node {
        public Nodes.OperatorNode op;
        public Node operand;
        public UnaryExpression(Nodes.OperatorNode op, Node operand) {
          this.op = op;
          this.operand = operand;
        }
      }

      public class MemberAccessStatement : BinaryExpression {
        public MemberAccessStatement(Nodes.OperatorNode op, Node left, Node right) : base(op, left, right) { }
      }
      public class IfStatement : Node {
        public Node condition;
        public Node body;
        public Node? elseBody;
        public IfStatement(Node condition, Node body, Node? elseBody) {
          this.condition = condition;
          this.body = body;
          this.elseBody = elseBody;
        }
      }

      public class WhileStatement : Node {
        public Node condition;
        public Node? body;
        public WhileStatement(Node condition, Node? body) {
          this.condition = condition;
          this.body = body;
        }
      }

      public class ForStatement : Node {
        public Node? init;
        public Node? condition;
        public Node? afterIteration;
        public Node? body;
        public ForStatement(Node? init, Node? condition, Node? afterIteration, Node? body) {
          this.init = init;
          this.afterIteration = afterIteration;
          this.body = body;
          this.condition = condition;
        }
      }

      public class DeclarationStatement : Node {
        public enum Type {
          variable, constant
        }
        public Type type;
        public string varName;
        public Node? init;
        public DeclarationStatement(Token declToken, Nodes.VariableNode var, Node? init) {
          if (declToken.flags.HasFlag(Flags.VariableDeclaration)) type = Type.variable; else type = Type.constant;
          if (type == Type.constant && init == null) {
            throw new SyntaxError($"Missing initializer in const declaration {var.name}");
          }
          varName = var.name;
          this.init = init;
        }
      }
      public class ReturnStatement : Node {
        public Node? returnValue;
        public ReturnStatement(Node? returnValue) {
          this.returnValue = returnValue;
        }
      }
      public class BreakStatement : Node { }
      public class ContinueStatement : Node { }
      public class DoWhileStatement : Node {
        public Node condition;
        public Node body;
        public DoWhileStatement(Node condition, Node body) {
          this.body = body;
          this.condition = condition;
        }
      }
      public class FunctionDeclarationStatement : Node {
        public List<Node> body;
        public string? name;
        public List<Nodes.VariableNode> args;
        public bool anonymous;
        public FunctionDeclarationStatement(List<Node> body, string? name, List<Nodes.VariableNode> args) {
          this.body = body;
          this.name = name;
          anonymous = name == null;
          this.args = args;
        }
      }
      public class FunctionCall : Node {
        public List<Node> args;
        public Node callee;
        public FunctionCall(List<Node> args, Node callee) {
          this.args = args;
          this.callee = callee;
        }
      }
    }
#if DEBUG
    public
#endif
    static class Nodes {
      public class OperatorNode : Node {
        public Op type;
        public string text;
        public int pos;
        public OperatorNode(Token op, bool isUnary = false) {
          if (!Operators.TryGetValue($"{op.type.name}{(isUnary ? "_unary" : "")}", out var type)) throw new Exception($"Unknown operator{op.text}");
          this.type = type;
          text = op.text;
          pos = op.pos;
        }
        public static OperatorNode? TryConstruct(Token op, bool isUnary = false) {
          if (op.flags.HasFlag(Flags.Operator)) return new(op, isUnary);
          return null;
        }
      }

      public class VariableNode : Node {
        public string name;
        public VariableNode(string name) {
          this.name = name;
        }
      }

      public class NumberNode : Node {
        public readonly double v = 0;
        public NumberNode(string num) {
          var _reverseString = (string str) => {
            var chars = str.ToCharArray();
            Array.Reverse(chars);
            return new string(chars);
          };
          var _parseInt = (string num, int b) => {
            long res = 0;
            int i = 0;
            foreach (char c in _reverseString(num)) {
              res += (c - '0') * (long)Math.Pow(b, i);
              i++;
            }
            return res;
          };
          if (num.StartsWith("0x")) {
            v = _parseInt(num[2..], 16);
          } else if (num.StartsWith("0b")) {
            v = _parseInt(num[2..], 2);
          } else {
            var parts = num.Split('.');
            bool hasIntPart = false;
            bool hasFractionPart = false;
            if (parts.Length == 2)
              hasFractionPart = hasIntPart = true;
            else if (num.StartsWith('.'))
              hasFractionPart = true;
            else hasIntPart = true;
            if (hasIntPart) {
              string rawIntPart = parts[0];
              v += _parseInt(rawIntPart, 10);
            }
            if (hasFractionPart) {
              int i = 1;
              string rawFractionPart = hasIntPart ? parts[1] : parts[0];
              foreach (char c in rawFractionPart) {
                v += (c - '0') * Math.Pow(10, -i);
                i++;
              }
            }
          }
        }
      }

      public class StringNode : Node {
        public string str;
        public StringNode(string str) {
          this.str = str;
        }
      }
      public class ObjectNode : Node {
        public Dictionary<Node, Node> props;
        public ObjectNode(Dictionary<Node, Node> props) {
          this.props = props;
        }
      }
      public class ArrayNode : Node {
        public List<Node?> elements;
        public ArrayNode(List<Node?> elements) {
          this.elements = elements;
        }
      }
      public class BooleanNode : Node {
        public bool value;
        public BooleanNode(Token t) {
          value = t == TokenNames.TrueLiteral;
        }
      }
    }
    private static class Parser {
      private class _Parser {
        private int pos = 0;
        private readonly List<Token> tokens;
        private readonly List<Node> nodes = new();
        private void MoveToNextToken() => pos++;
        private Token? NextToken => tokens.Count - 1 > pos ? tokens[pos + 1] : null;
        private Token? CurrentToken => tokens.Count > pos ? tokens[pos] : null;
        private Token? PreviousToken => tokens.Count + 1 > pos ? tokens[pos - 1] : null;
        private bool CurrentTokenIs(string name, out Token? t) => TokenIs(CurrentToken, name, out t);
        public _Parser(List<Token> tokens) {
          this.tokens = tokens;
        }
        private static void ThrowSyntaxError(string msg, int? pos) => throw new SyntaxError(msg, pos);
        private static void ThrowExpected(
          string msg,
          int? pos,
          bool q = true // use quotes around msg
        ) => ThrowSyntaxError($"Expected {(q ? "\"" : "")}{msg}{(q ? "\"" : "")}", pos);
        private void ThrowExpected(
          string msg,
          bool q = true
        ) => ThrowSyntaxError($"Expected {(q ? "\"" : "")}{msg}{(q ? "\"" : "")}", CurrentToken?.pos);
        private void Expect(string token, bool inv, string throwMsg, int? throwPos, bool q = true) {
          if (inv && CurrentToken == token || !inv && CurrentToken != token) ThrowExpected(throwMsg, throwPos, q);
        }
        private void Expect(string token, bool inv, string throwMsg, bool q = true) => Expect(token, inv, throwMsg, CurrentToken?.pos, q);
        static bool TokenIs(
          Token? token,
          string name,
          out Token? t
        ) => (token != (Token?)null && token == name ? (t = token, true) : (t = null, false)).Item2;
        /*
         * All methods set pos to the next token
         *
         */
        public List<Node> Parse(int count = int.MaxValue, string? WhileToken = null) {
          var localNodes = new List<Node>();
          var Add = (Node n) => {
            if (WhileToken == null && count == int.MaxValue) nodes.Add(n);
            else localNodes.Add(n);
          };
          while (localNodes.Count < count && pos < tokens.Count) {
            var t = CurrentToken!;
            var tokenFlags = t.flags;
            if (WhileToken != null && t == WhileToken) {
              MoveToNextToken();
              break;
            };
            if (tokenFlags.HasFlag(Flags.Declaration)) {
              Add(ParseDeclaration());
              continue;
            }
            if (t == TokenNames.Function) {
              Add(ParseFunctionDeclaration());
              continue;
            }
            if (t == TokenNames.OpCurlyBracket) {
              Add(ParseBlock());
              continue;
            }
            if (t == TokenNames.If) {
              Add(ParseIf());
              continue;
            }
            if (t == TokenNames.Else) {
              ThrowSyntaxError("Unexpected \"else\"", t.pos);
            }
            if (t == TokenNames.While) {
              Add(ParseWhile());
              continue;
            }
            if (t == TokenNames.Do) {
              Add(ParseDoWhile());
              continue;
            }
            if (t == TokenNames.For) {
              Add(ParseFor());
              continue;
            }
            if (t == TokenNames.Break) {
              Add(ParseBreak());
              continue;
            }
            if (t == TokenNames.Continue) {
              Add(ParseContinue());
              continue;
            }
            if (t == TokenNames.Return) {
              Add(ParseReturn());
              continue;
            }
            Add(ParseExpression());
            Expect(TokenNames.Semicolon, false, ";");
            MoveToNextToken();
          }
          if (WhileToken != null || count != int.MaxValue) return localNodes;
          return nodes;
        }
        private Statements.ReturnStatement ParseReturn() {
          Expect(TokenNames.Return, false, "return");
          MoveToNextToken();
          var ret = ParseExpression();
          Expect(TokenNames.Semicolon, false, ";");
          MoveToNextToken();
          return new(ret);
        }
        private Statements.ContinueStatement ParseContinue() {
          Expect(TokenNames.Continue, false, "continue");
          MoveToNextToken();
          Expect(TokenNames.Semicolon, false, ";");
          MoveToNextToken();
          return new();
        }
        private Statements.BreakStatement ParseBreak() {
          Expect(TokenNames.Break, false, "break");
          MoveToNextToken();
          Expect(TokenNames.Semicolon, false, ";");
          MoveToNextToken();
          return new();
        }
        private Statements.ForStatement ParseFor() {
          Expect(TokenNames.For, false, "for");
          MoveToNextToken();
          Expect(TokenNames.OpRndBracket, false, "(");
          MoveToNextToken();
          var init = Parse(1)[0];
          if (init.Name == "DeclarationStatement") pos--;
          Expect(TokenNames.Semicolon, false, ";");
          MoveToNextToken();
          var condition = ParseExpression();
          Expect(TokenNames.Semicolon, false, ";");
          MoveToNextToken();
          var afterIteration = ParseExpression();
          Expect(TokenNames.ClRndBracket, false, ")");
          MoveToNextToken();
          if (CurrentToken == TokenNames.Semicolon) return new(init, condition, afterIteration, null);
          var body = Parse(1)[0];
          return new(init, condition, afterIteration, body);
        }
        private Statements.DoWhileStatement ParseDoWhile() {
          Expect(TokenNames.Do, false, "do");
          MoveToNextToken();
          var body = Parse(1)[0];
          Expect(TokenNames.While, false, "while");
          MoveToNextToken();
          Expect(TokenNames.OpRndBracket, false, "(");
          MoveToNextToken();
          Expect(TokenNames.ClRndBracket, true, "expression", false);
          var condition = ParseExpression();
          Expect(TokenNames.ClRndBracket, false, ")");
          MoveToNextToken();
          Expect(TokenNames.Semicolon, false, ";");
          MoveToNextToken();
          return new(condition, body);
        }
        private Statements.WhileStatement ParseWhile() {
          Expect(TokenNames.While, false, "while");
          MoveToNextToken();
          Expect(TokenNames.OpRndBracket, false, "(");
          MoveToNextToken();
          Expect(TokenNames.ClRndBracket, true, "expression", false);
          var condition = ParseExpression();
          Expect(TokenNames.ClRndBracket, false, ")");
          MoveToNextToken();
          if (CurrentToken == TokenNames.Semicolon) {
            MoveToNextToken();
            return new(condition, null);
          }
          return new(condition, Parse(1)[0]);
        }
        private Statements.IfStatement ParseIf() {
          // if (expr) statement [else statement]
          // statement can be block, so
          // if (expr) { statements; } [else statement]
          Expect(TokenNames.If, false, "if");
          MoveToNextToken();
          Expect(TokenNames.OpRndBracket, false, "(");
          MoveToNextToken();
          Expect(TokenNames.ClRndBracket, true, "expression", PreviousToken?.pos, false);
          var condition = ParseExpression();
          Expect(TokenNames.ClRndBracket, false, ")");
          MoveToNextToken();
          var body = Parse(1)[0];
          if (CurrentToken != TokenNames.Else) return new(condition, body, null);
          MoveToNextToken();
          var ElseBody = Parse(1)[0];
          return new(condition, body, ElseBody);
        }
        private List<Node> ParseCommas(string stopToken) {
          List<int> commaPositions = new();
          int _pos = pos;
          int rnd = 0, sq = 0, cur = 0;
          var c = () => rnd == 0 && sq == 0 && cur == 0;
          while (true) {
            if (CurrentToken == stopToken && c()) break;
            if (CurrentToken == TokenNames.Comma_operator && c()) {
              commaPositions.Add(CurrentToken.pos);
              MoveToNextToken();
              continue;
            }
            if (CurrentToken == TokenNames.OpRndBracket) rnd++;
            else if (CurrentToken == TokenNames.OpSqBracket) sq++;
            else if (CurrentToken == TokenNames.OpCurlyBracket) cur++;
            else if (CurrentToken == TokenNames.ClCurlyBracket) cur--;
            else if (CurrentToken == TokenNames.ClSqBracket) sq--;
            else if (CurrentToken == TokenNames.ClRndBracket) rnd--;
            MoveToNextToken();
          }
          pos = _pos;
          var n = ParseExpression()!;
          MoveToNextToken(); // Current position on stopToken, move it to next
          List<Node> nodes = new();
          if (commaPositions.Count == 0) {
            if (n != null)
              nodes.Add(n);
            return nodes;
          }
          Node expr = n;
          while (true) {
            if (expr.Name != "BinaryExpression") {
              nodes.Add(expr);
              break;
            }
            var b = (Statements.BinaryExpression)expr;
            if (commaPositions.IndexOf(b.op.pos) == -1) {
              nodes.Add(expr);
              break;
            };
            nodes.Add(b.right);
            expr = b.left;
          }
          nodes.Reverse();
          return nodes;
        }
        private Statements.Block ParseBlock() {
          MoveToNextToken();
          return new(Parse(int.MaxValue, TokenNames.ClCurlyBracket));
        }
        private Statements.DeclarationStatement ParseDeclaration() {
          // let <identifier> [= expr];
          // const <identifier> = expr;
          var declToken = CurrentToken;
          if (!declToken!.type.flags.HasFlag(Flags.Declaration)) ThrowExpected("\"let\" or \"const\"", false);
          MoveToNextToken();// var_name
          if (!CurrentTokenIs(TokenNames.Identifier, out var variableName)) ThrowExpected("identifier", false);
          MoveToNextToken(); // semicolon or assignment
          Node? init = null;
          if (CurrentToken == TokenNames.Assignment_operator) {
            // assignment
            MoveToNextToken();
            init = ParseExpression(); // parsing initializer
          }
          Expect(TokenNames.Semicolon, false, ";");
          MoveToNextToken();
          return new(declToken, new Nodes.VariableNode(variableName!.text), init);
        }
        private Statements.FunctionCall ParseFunctionCall(Node callee) {
          Expect(TokenNames.OpRndBracket, false, "(");
          MoveToNextToken();
          var args = ParseCommas(TokenNames.ClRndBracket);
          return new(args, callee);
        }
        private Statements.FunctionDeclarationStatement ParseFunctionDeclaration(bool isExpr = false) {
          // function <function-name>(arg1, arg2, arg_i) {
          //  [statements;]
          // }
          Expect(TokenNames.Function, false, "function");
          MoveToNextToken();
          if (!CurrentTokenIs(TokenNames.Identifier, out var name) && !isExpr) ThrowExpected("function name", false);
          if (name != (Token?)null) MoveToNextToken();
          Expect(TokenNames.OpRndBracket, false, "(");
          MoveToNextToken();
          List<Nodes.VariableNode> argNames = new();
          while (CurrentToken != TokenNames.ClRndBracket) {
            Expect(TokenNames.Identifier, false, "argument name or \")\"", false);
            argNames.Add(new Nodes.VariableNode(CurrentToken!.text));
            MoveToNextToken();
            if (CurrentToken != TokenNames.ClRndBracket && CurrentToken != TokenNames.Comma_operator)
              ThrowExpected("\",\" or \")\"", false);
            if (CurrentToken == TokenNames.Comma_operator) MoveToNextToken();
          }
          MoveToNextToken();
          Expect(TokenNames.OpCurlyBracket, false, "{");
          MoveToNextToken();
          List<Node> body = Parse(int.MaxValue, TokenNames.ClCurlyBracket);
          return new(body, name?.text, argNames);
        }
        private Node ParseExpression() => ParseBinaryOperators(ParsePrimary(), 0);
        private Statements.UnaryExpression ParseUnaryOperator() {
          var op = Nodes.OperatorNode.TryConstruct(CurrentToken!, true);
          if (op == null) ThrowExpected("unary operator (\"+\",  \"-\", \"!\" or \"~\")", false);
          MoveToNextToken();
          var expr = ParseBinaryOperators(ParsePrimary(), op!.type);
          return new(op, expr);
        }
        private Node ParseBinaryOperators(Node lhs, int minPrecedence) {
          start:
          if (CurrentToken?.flags.HasFlag(Flags.Operator) ?? false) {
            Nodes.OperatorNode? lookahead = Nodes.OperatorNode.TryConstruct(CurrentToken);
            while (lookahead != null && lookahead.type >= minPrecedence) {
              if (CurrentToken == TokenNames.OpRndBracket || CurrentToken == TokenNames.OpSqBracket) {
#pragma warning disable IDE0059 // suppress warning because name of variable must be accessed to use recursion
                var a = (ref Node n) => { return; };
#pragma warning restore IDE0059
                a = (ref Node n) => {
                  if (n.Name == "BinaryExpression" && ((Statements.BinaryExpression)n).op.type < Operators[TokenNames.OpRndBracket])
                    a(ref ((Statements.BinaryExpression)n).right);
                  else if (CurrentToken == TokenNames.OpRndBracket) {
                    n = ParseFunctionCall(n);
                  } else {
                    MoveToNextToken();
                    n = new Statements.MemberAccessStatement(
                      new(new(CurrentToken.pos, ".", TokenType.ToDictItem(TokenNames.Dot_operator, "^\\.", Flags.Operator).Value)),
                      n,
                      ParseExpression()
                    );
                    Expect(TokenNames.ClSqBracket, false, "]");
                    MoveToNextToken();
                  }
                };
                a(ref lhs);
                goto start;
              }
              MoveToNextToken();
              var op = lookahead;
              var rhs = ParsePrimary();
              if (rhs == null) break;
              if (pos < tokens.Count && CurrentToken.type.flags.HasFlag(Flags.Operator)) {
                lookahead = new Nodes.OperatorNode(CurrentToken);
                while (
                  lookahead != null &&
                  (lookahead.type > op.type && lookahead.type == OperatorDirection.LeftToRight ||
                  lookahead.type == OperatorDirection.RightToLeft && (int)lookahead.type == op.type)
                  ) {
                  rhs = ParseBinaryOperators(rhs, op.type + ((lookahead.type > op.type) ? 1 : 0));
                  lookahead = Nodes.OperatorNode.TryConstruct(CurrentToken);
                }
              } else lookahead = null;
              if (op.text == ".") lhs = new Statements.MemberAccessStatement(op, lhs, rhs);
              else lhs = new Statements.BinaryExpression(op, lhs, rhs);
            }
          }
          return lhs;
        }
        private Nodes.ObjectNode ParseObject() {
          // Object
          // {
          //   [expr] = expr;
          //   name = expr;
          // }
          Expect(TokenNames.OpCurlyBracket, false, "{");
          MoveToNextToken();
          Dictionary<Node, Node> keysAndValues = new();
          while (CurrentToken != TokenNames.ClCurlyBracket) {
            Node key;
            if (CurrentToken == TokenNames.OpSqBracket) {
              // Dynamic key
              MoveToNextToken();
              key = ParseExpression();
              Expect(TokenNames.ClSqBracket, false, "]");
              MoveToNextToken();
            } else {
              key = ParsePrimary();
              // Prop name must be string with or without quotation marks
              if (key is not Nodes.StringNode && key is not Nodes.VariableNode) ThrowExpected("property name", false);
            }
            Expect(TokenNames.Assignment_operator, false, "=");
            MoveToNextToken();
            var value = ParseExpression();
            if (CurrentToken != TokenNames.Semicolon && CurrentToken != TokenNames.ClCurlyBracket) ThrowExpected("';' or '}'", false);
            if (CurrentToken == TokenNames.Semicolon) MoveToNextToken(); // Move after semicolon
            keysAndValues.Add(key, value);
          }
          MoveToNextToken();
          return new Nodes.ObjectNode(keysAndValues);
        }
        private Nodes.ArrayNode ParseArray() {
          // Array
          // [
          //    expr, expr
          //  ]
          // Comma can be omitted before closing bracket
          List<Node?> elems = new();
          Expect(TokenNames.OpSqBracket, false, "[");
          MoveToNextToken();
          return new(ParseCommas(TokenNames.ClSqBracket!)!);
        }
        private Node ParsePrimary() {
          // primary ::= ( expr ) | object-literal | array-literal | unary-operator + expr
          if (CurrentToken == TokenNames.OpCurlyBracket) return ParseObject();
          if (CurrentToken == TokenNames.OpSqBracket) return ParseArray();
          if (CurrentToken == TokenNames.OpRndBracket) {
            // If current token is left opening par (
            // Parsing expression in Bracket
            MoveToNextToken();
            var expr = ParseExpression();
            Expect(TokenNames.ClRndBracket, false, ")");
            MoveToNextToken();
            return expr;
          }
          if (false
            || CurrentToken == TokenNames.Add_operator
            || CurrentToken == TokenNames.Subtract_operator
            || CurrentToken == TokenNames.BitwiseNot_operator
            || CurrentToken == TokenNames.LogicNot_operator
          ) return ParseUnaryOperator();
          if (CurrentToken == TokenNames.Function) return ParseFunctionDeclaration(true);
          MoveToNextToken();
          if (TokenIs(PreviousToken, TokenNames.NumberLiteral, out var num)) return new Nodes.NumberNode(num!.text);
          if (TokenIs(PreviousToken, TokenNames.Identifier, out var var)) return new Nodes.VariableNode(var!.text);
          if (TokenIs(PreviousToken, TokenNames.StringLiteral, out var str)) return new Nodes.StringNode(str!.text);
          if (PreviousToken != (Token?)null && PreviousToken.flags.HasFlag(Flags.BoolLiteral)) return new Nodes.BooleanNode(PreviousToken!);
          pos--;
          return null;
          //ThrowSyntaxError($"Unexpected token: {PreviousToken!.text}", PreviousToken?.pos);
        }
      }
      public static List<Node> Parse(List<Token> tokens) {
        _Parser p = new(tokens);
        return p.Parse();
      }
    }
#if DEBUG
    public
#endif
    class Token {
      public int pos;
      public string text;
      public Flags flags => type.flags;
      public TokenType type;
      public Token(int pos, string text, TokenType type) {
        this.pos = pos;
        this.text = text;
        this.type = type;
      }
      public static bool operator ==(Token? t, string name) => t?.type.name == name;
      public static bool operator !=(Token? t, string name) => t?.type.name != name;
      public static bool operator ==(string name, Token? t) => t == name;
      public static bool operator !=(string name, Token? t) => t != name;
    }
    [Flags]
#if DEBUG
    public
#else
    private
#endif
    enum Flags {
      Operator = 1,
      Identifier = 2,
      Keyword = 4,
      Whitespace = 8,
      Endline = 16,
      InstructionSeparator = 32,
      Literal = 64,
      StringLiteral = 128,
      NumericLiteral = 256,
      VariableDeclaration = 512,
      ConstantDeclaration = 1024,
      Declaration = 2048,
      Bracket = 4096,
      OpeningBracket = 8192,
      ClosingBracket = 16384,
      RoundBracket = 32768,
      SquareBracket = 65536,
      CurlyBracket = 131072,
      BoolLiteral = 262144,
      Comment = 524288
    }
#if DEBUG
    public
#endif
    class TokenType {
      public string name;
      public Regex regex;
      public Flags flags;
      private TokenType(string name, Regex regex, Flags flags) {
        this.name = name;
        this.regex = regex;
        this.flags = flags;
      }
      public static KeyValuePair<string, TokenType> ToDictItem(
        string name,
        string regex,
        Flags flags
      ) => new(name, new TokenType(name, new Regex(regex, RegexOptions.Compiled), flags));
    }
#if DEBUG
    public
#else
    private
#endif
    enum OperatorDirection {
      LeftToRight = 0,
      RightToLeft = 1,
      NonBinary = 2
    }
#if DEBUG
    public
#endif
    class Op {
      public int priority;
      public OperatorDirection direction;
      public bool isBinary;
      public Op(int priority, OperatorDirection direction = OperatorDirection.NonBinary) {
        this.priority = priority;
        this.direction = direction;
        isBinary = direction != OperatorDirection.NonBinary;
      }
      public static implicit operator int(Op value) => value.priority;
      public static implicit operator Op(int priority) => new(priority, OperatorDirection.LeftToRight);
      public static implicit operator OperatorDirection(Op value) => value.direction;
    }
    private static readonly Dictionary<string, Op> Operators = new() {
      [TokenNames.Comma_operator] = 1, // ,
      [TokenNames.Assignment_operator] = new(2, OperatorDirection.RightToLeft), // =
      [TokenNames.AssignmentAddition_operator] = new(2, OperatorDirection.RightToLeft), // +=
      [TokenNames.AssignmentSubtraction_operator] = new(2, OperatorDirection.RightToLeft), // -=
      [TokenNames.AssignmentMultiplication_operator] = new(2, OperatorDirection.RightToLeft), // *=
      [TokenNames.AssignmentDivision_operator] = 2, // /=
      [TokenNames.AssignmentRemainder_operator] = 2, // %=
      [TokenNames.LogicOr_operator] = 7, // ||
      [TokenNames.LogicAnd_operator] = 8, // &&
      [TokenNames.BitwiseOr_operator] = 9, // |
      [TokenNames.BitwiseXor_operator] = 10, // ^
      [TokenNames.BitwiseAnd_operator] = 11, // &
      [TokenNames.Eq_operator] = 12, // ==
      [TokenNames.StrictEq_operator] = 12, // ===
      [TokenNames.NotEq_operator] = 12, // !=
      [TokenNames.StrictNotEq_operator] = 12, // !==
      [TokenNames.LessOrEquals_operator] = 14, // <=
      [TokenNames.Less_operator] = 14, // <
      [TokenNames.GreaterOrEquals_operator] = 14, // >
      [TokenNames.Greater_operator] = 14, // >
      [TokenNames.Add_operator] = 15, // +
      [TokenNames.Subtract_operator] = 15, // -
      [TokenNames.Mult_operator] = 16, // *
      [TokenNames.Division_operator] = 16, // /
      [TokenNames.Remainder_operator] = 16, // %
      [TokenNames.Power_operator] = 17, // **,
      [$"{TokenNames.Add_operator}_unary"] = new(19, OperatorDirection.NonBinary), // +expr
      [$"{TokenNames.Subtract_operator}_unary"] = new(19, OperatorDirection.NonBinary), // -expr
      [$"{TokenNames.LogicNot_operator}_unary"] = new(19, OperatorDirection.NonBinary), // !expr
      [$"{TokenNames.BitwiseNot_operator}_unary"] = new(19, OperatorDirection.NonBinary), // ~expr
      [TokenNames.OpRndBracket] = 20,
      [TokenNames.OpSqBracket] = 20,
      [TokenNames.Dot_operator] = 21, // variable.member
    };
    private static class TokenNames {
      public static readonly string Space = "space";
      public static readonly string Add_operator = "addition_operator";
      public static readonly string Subtract_operator = "subtraction_operator";
      public static readonly string Mult_operator = "multiplication_operator";
      public static readonly string Division_operator = "division_operator";
      public static readonly string Remainder_operator = "remainder_operator";
      public static readonly string Power_operator = "pow_operator";
      public static readonly string AssignmentAddition_operator = "assign_add_operator";
      public static readonly string AssignmentSubtraction_operator = "assign_subtract_operator";
      public static readonly string AssignmentDivision_operator = "assign_divide_operator";
      public static readonly string AssignmentMultiplication_operator = "assign_multiply_operator";
      public static readonly string AssignmentRemainder_operator = "assign_remainder_operator";
      public static readonly string Semicolon = "semicolon";
      public static readonly string Assignment_operator = "assignment_operator";
      public static readonly string NumberLiteral = "number_literal";
      public static readonly string StringLiteral = "string_literal";
      public static readonly string VariableDeclaration = "variable_declaration_keyword";
      public static readonly string ConstantDeclaration = "constant_declaration_keyword";
      public static readonly string OpRndBracket = "opening_round_bracket";
      public static readonly string ClRndBracket = "closing_round_bracket";
      public static readonly string OpSqBracket = "opening_sq_bracket";
      public static readonly string ClSqBracket = "closing_sq_bracket";
      public static readonly string OpCurlyBracket = "opening_curly_bracket";
      public static readonly string ClCurlyBracket = "closing_curly_bracket";
      public static readonly string Function = "function_keyword";
      public static readonly string If = "if_keyword";
      public static readonly string Else = "else_keyword";
      public static readonly string For = "for_keyword";
      public static readonly string Do = "do_keyword";
      public static readonly string While = "while_keyword";
      public static readonly string Return = "return keyword";
      public static readonly string Dot_operator = "dot_operator";
      public static readonly string Comma_operator = "comma_operator";
      public static readonly string Break = "break_keyword";
      public static readonly string Continue = "continue_keyword";
      public static readonly string StrictEq_operator = "strict_equals_operator";
      public static readonly string Eq_operator = "equals_operator";
      public static readonly string NotEq_operator = "not_equals_operator";
      public static readonly string StrictNotEq_operator = "strict_not_equals_operator";
      public static readonly string Greater_operator = "greater_operator";
      public static readonly string Less_operator = "less_operator";
      public static readonly string GreaterOrEquals_operator = "greater_or_equals_operator";
      public static readonly string LessOrEquals_operator = "less_or_equals_operator";
      public static readonly string Increment_operator = "increment_operator";
      public static readonly string Decrement_operator = "decrement_operator";
      public static readonly string LogicAnd_operator = "logic_and_operator";
      public static readonly string LogicOr_operator = "logic_or_operator";
      public static readonly string LogicNot_operator = "logic_not_operator";
      public static readonly string BitwiseAnd_operator = "bitwise_and_operator";
      public static readonly string BitwiseOr_operator = "bitwise_or_operator";
      public static readonly string BitwiseXor_operator = "bitwise_xor_operator";
      public static readonly string BitwiseNot_operator = "bitwise_not_operator";
      public static readonly string TrueLiteral = "true_literal";
      public static readonly string FalseLiteral = "false_literal";
      public static readonly string NullLiteral = "null_literal";
      public static readonly string Identifier = "identifier";
      public static readonly string Comment = "comment";
      public static readonly string FunctionCall_operator = "function_call_operator";
    }
    private static class Lexer {
      private static readonly Dictionary<string, TokenType> Tokens = new(new List<KeyValuePair<string, TokenType>>() {
        TokenType.ToDictItem(TokenNames.Space, @"^\s+", Flags.Endline | Flags.Whitespace),
        TokenType.ToDictItem(TokenNames.Add_operator, @"^\+(?![\+=])", Flags.Operator),
        TokenType.ToDictItem(TokenNames.Subtract_operator, @"^\-(?![\-=])", Flags.Operator),
        TokenType.ToDictItem(TokenNames.Mult_operator, @"^\*(?![\*\=])", Flags.Operator),
        TokenType.ToDictItem(TokenNames.Division_operator, @"^/(?![=/\*])", Flags.Operator),
        TokenType.ToDictItem(TokenNames.Remainder_operator, "^%(?!=)", Flags.Operator),
        TokenType.ToDictItem(TokenNames.Power_operator, @"^\*\*", Flags.Operator),
        TokenType.ToDictItem(TokenNames.Increment_operator, @"^\+\+", Flags.Operator),
        TokenType.ToDictItem(TokenNames.Decrement_operator, @"^\-\-", Flags.Operator),
        TokenType.ToDictItem(TokenNames.Dot_operator, @"^\.(?!\d)", Flags.Operator),
        TokenType.ToDictItem(TokenNames.Comma_operator, "^,", Flags.Operator),
        TokenType.ToDictItem(TokenNames.Semicolon, "^;", Flags.InstructionSeparator),
        TokenType.ToDictItem(TokenNames.Assignment_operator, @"^=(?!=)", Flags.Operator),
        TokenType.ToDictItem(TokenNames.AssignmentAddition_operator, @"^\+=", Flags.Operator),
        TokenType.ToDictItem(TokenNames.AssignmentSubtraction_operator, @"^\-=", Flags.Operator),
        TokenType.ToDictItem(TokenNames.AssignmentMultiplication_operator, @"^\*=", Flags.Operator),
        TokenType.ToDictItem(TokenNames.AssignmentDivision_operator, @"^\/=", Flags.Operator),
        TokenType.ToDictItem(TokenNames.AssignmentRemainder_operator, @"^%=", Flags.Operator),
        TokenType.ToDictItem(TokenNames.NumberLiteral, @"^0b[01]+|^0x[0-9a-fA-F]+|^\.\d+|^\d+(\.\d*)?", Flags.Literal | Flags.NumericLiteral),
        TokenType.ToDictItem(TokenNames.StringLiteral, "^\".*?\"|^'.*?'", Flags.StringLiteral | Flags.Literal),
        TokenType.ToDictItem(TokenNames.VariableDeclaration, "^let", Flags.Declaration | Flags.VariableDeclaration | Flags.Keyword),
        TokenType.ToDictItem(TokenNames.ConstantDeclaration, "^const", Flags.Declaration | Flags.ConstantDeclaration | Flags.Keyword),
        TokenType.ToDictItem(TokenNames.OpRndBracket, @"^\(", Flags.OpeningBracket | Flags.RoundBracket | Flags.Bracket | Flags.Operator),
        TokenType.ToDictItem(TokenNames.ClRndBracket, @"^\)", Flags.ClosingBracket | Flags.RoundBracket | Flags.Bracket),
        TokenType.ToDictItem(TokenNames.OpSqBracket, @"^\[", Flags.OpeningBracket | Flags.SquareBracket | Flags.Bracket| Flags.Operator),
        TokenType.ToDictItem(TokenNames.ClSqBracket, @"^\]", Flags.ClosingBracket | Flags.SquareBracket | Flags.Bracket),
        TokenType.ToDictItem(TokenNames.OpCurlyBracket, @"^\{", Flags.OpeningBracket | Flags.CurlyBracket | Flags.Bracket),
        TokenType.ToDictItem(TokenNames.ClCurlyBracket, @"^\}", Flags.ClosingBracket | Flags.CurlyBracket | Flags.Bracket),
        TokenType.ToDictItem(TokenNames.Function, "^function", Flags.Keyword),
        TokenType.ToDictItem(TokenNames.If, "^if", Flags.Keyword),
        TokenType.ToDictItem(TokenNames.Else, "^else", Flags.Keyword),
        TokenType.ToDictItem(TokenNames.For, "^for", Flags.Keyword),
        TokenType.ToDictItem(TokenNames.Do, "^do", Flags.Keyword),
        TokenType.ToDictItem(TokenNames.While, "^while", Flags.Keyword),
        TokenType.ToDictItem(TokenNames.Return, "^return", Flags.Keyword),
        TokenType.ToDictItem(TokenNames.Break, "^break", Flags.Keyword),
        TokenType.ToDictItem(TokenNames.Continue, "^continue", Flags.Keyword),
        TokenType.ToDictItem(TokenNames.Eq_operator, "^==(?!=)", Flags.Operator),
        TokenType.ToDictItem(TokenNames.StrictEq_operator, "^===", Flags.Operator),
        TokenType.ToDictItem(TokenNames.Less_operator, "^<", Flags.Operator),
        TokenType.ToDictItem(TokenNames.Greater_operator, "^>", Flags.Operator),
        TokenType.ToDictItem(TokenNames.LessOrEquals_operator, "^<=", Flags.Operator),
        TokenType.ToDictItem(TokenNames.GreaterOrEquals_operator, "^>=", Flags.Operator),
        TokenType.ToDictItem(TokenNames.NotEq_operator, "^!=(?!=)", Flags.Operator),
        TokenType.ToDictItem(TokenNames.StrictNotEq_operator, "^!==", Flags.Operator),
        TokenType.ToDictItem(TokenNames.LogicNot_operator, "^!(?!=)", Flags.Operator),
        TokenType.ToDictItem(TokenNames.LogicAnd_operator, "^&&", Flags.Operator),
        TokenType.ToDictItem(TokenNames.LogicOr_operator, @"^\|\|", Flags.Operator),
        TokenType.ToDictItem(TokenNames.BitwiseAnd_operator, "^&(?!&)", Flags.Operator),
        TokenType.ToDictItem(TokenNames.BitwiseOr_operator, @"^\|(?!\|)", Flags.Operator),
        TokenType.ToDictItem(TokenNames.BitwiseXor_operator, @"^\^", Flags.Operator),
        TokenType.ToDictItem(TokenNames.BitwiseNot_operator, "^~", Flags.Operator),
        TokenType.ToDictItem(TokenNames.TrueLiteral, @"^true(?!\w)", Flags.Literal | Flags.BoolLiteral),
        TokenType.ToDictItem(TokenNames.FalseLiteral, @"^false(?!\w)", Flags.Literal | Flags.BoolLiteral),
        TokenType.ToDictItem(TokenNames.NullLiteral, @"^null(?!\w)", Flags.Literal),
        TokenType.ToDictItem(TokenNames.Comment, @"^//.*(?:\n|$)|^/\*(.|\s)*?\*/", Flags.Comment),
        //TokenType.ToDictItem(TokenNames.Identifier, "^[$a-zA-Zа-яА-ЯёЁ_][\\w$_]*(?=[\\(\\)+\\-\\*\\/=;:\\s\\.,\\?%\\|\\[\\]\\{\\}\"]')", Flags.Identifier),
        TokenType.ToDictItem(TokenNames.Identifier, "^[$a-zA-Zа-яА-ЯёЁ_][$a-zA-Zа-яА-ЯёЁ_0-9]*", Flags.Identifier),
      });
      public static List<Token> Analysis(string code) {
        List<Token> tokens = new();
        int pos = 0;
        while (Next(code, ref tokens, ref pos)) ;
        tokens.RemoveAll(t => t.flags.HasFlag(Flags.Whitespace) || t.flags.HasFlag(Flags.Comment));
        return tokens;

      }
      private static bool Next(string code, ref List<Token> tokens, ref int pos) {
        if (pos >= code.Length) return false;
        foreach (var token in Tokens) {
          var t = token.Value;
          var m = t.regex.Match(code[pos..]);
          if (!m.Success) continue;
          tokens.Add(new(pos, m.Value, t));
          pos += m.Length;
          return true;
        }
        throw new SyntaxError($"Syntax Error at position {pos}:\n{code[(pos < 20 ? pos : pos - 20)..(pos + 10)].Replace('\n', ' ')}\n" +
          $"{string.Join("", Enumerable.Repeat(" ", (pos < 20 ? pos : 20)))}^^", null);
      }
    }
    private static class Runner {
      public static int Run(string code) {
        var t = Lexer.Analysis(code);
        var a = Parser.Parse(t);
        return 0;
      }
    }
    public static int Exec(string code) => Runner.Run(code);
#if DEBUG
    public static N Parse(T tokens) => Parser.Parse(tokens);
    public static T LexicalAnalysis(string input) => Lexer.Analysis(input);
    public static void PrintNode(Node node, int indent = 0, bool indentAtStart = true) {
      var wI = () => Console.Write(string.Join("", Enumerable.Repeat(" ", indent)));
      if (indent != 0 && indentAtStart) wI();
      var w = (object p) => Console.Write(p);
      switch (node.Name) {
        case "NumberNode":
          w(((Nodes.NumberNode)node).v);
          break;
        case "VariableNode":
          w(((Nodes.VariableNode)node).name);
          break;
        case "StringNode":
          w(((Nodes.StringNode)node).str);
          break;
        case "UnaryExpression": {
          Statements.UnaryExpression expr = (Statements.UnaryExpression)node;
          w(expr.op.text);
          w("(");
          PrintNode(expr.operand);
          w(")");
          break;
        }
        case "MemberAccessStatement":
        case "BinaryExpression": {
          Statements.BinaryExpression expr = (Statements.BinaryExpression)node;
          w("(");
          PrintNode(expr.left);
          if (node.Name == "MemberAccessStatement") w(")");
          else w(") ");
          w(expr.op.text);
          if (node.Name == "MemberAccessStatement") w("(");
          else w(" (");
          PrintNode(expr.right);
          w(")");
          break;
        }
        case "DeclarationStatement":
          Statements.DeclarationStatement d = (Statements.DeclarationStatement)node;
          if (d.type == Statements.DeclarationStatement.Type.variable) w("let ");
          else w("const ");
          w(d.varName);
          if (d.init != null) {
            w(" = ");
            PrintNode(d.init);
          }
          w(";");
          break;

        case "FunctionCall":
          Statements.FunctionCall c = (Statements.FunctionCall)node;
          w("(");
          PrintNode(c.callee);
          w(")");
          w("(");
          for (int i = 0; i < c.args.Count; i++) {
            var arg = c.args[i];
            w("(");
            PrintNode(arg);
            w(")");
            if (i + 1 != c.args.Count) w(", ");
          }
          w(")");
          break;
        case "BooleanNode":
          w(((Nodes.BooleanNode)node).value);
          break;
        case "ArrayNode": {
          Nodes.ArrayNode expr = (Nodes.ArrayNode)node;
          w("[");
          for (var i = 0; i < expr.elements.Count; i++) {
            var el = expr.elements[i];
            PrintNode(el!);
            if (i != expr.elements.Count - 1) w(", ");
          }
          w("]");
          break;
        }
        case "FunctionDeclarationStatement":
          Statements.FunctionDeclarationStatement function = (Statements.FunctionDeclarationStatement)node;
          w("function");
          if (function.name != null) {
            w(" ");
            w(function.name);
          }
          w("(");
          for (var i = 0; i < function.args.Count; i++) {
            var arg = function.args[i];
            w(arg.name);
            if (i != function.args.Count - 1) w(", ");
          }
          w(")");
          PrintNode(new Statements.Block(function.body), indent);
          break;
        case "Block": {
          w("{\n");
          N nodes = ((Statements.Block)node).nodes;
          foreach (var n in nodes) {
            PrintNode(n, indent + 2);
            w("\n");
          }
          wI();
          w("}");
          break;
        }
        case "IfStatement": {
          var ifStm = (Statements.IfStatement)node;
          w("if (");
          PrintNode(ifStm.condition);
          w(") ");
          var body = ifStm.body;
          var elseBody = ifStm.elseBody;
          var isBodyBlock = body.Name == "Block";
          if (!isBodyBlock) {
            w("\n");
            PrintNode(body, indent + 2);
          } else PrintNode(body, indent, false);
          if (elseBody == null) break;
          if (isBodyBlock) {
            w(" else ");
            PrintNode(elseBody, indent, false);
          } else {
            w("\nelse\n");
            PrintNode(elseBody, indent + 2);
          }
          break;
        }
        case "WhileStatement": {
          var wh = (Statements.WhileStatement)node;
          w("while (");
          PrintNode(wh.condition);
          w(") ");
          if (wh.body == null) {
            w(";");
            break;
          }
          var body = wh.body;
          if (body.Name == "Block") {
            PrintNode(body, indent, false);
          } else {
            w("\n");
            PrintNode(body, indent + 2);
          }
          break;
        }
        case "DoWhileStatement": {
          var st = (Statements.DoWhileStatement)node;
          w("do ");
          var body = st.body;
          if (body.Name == "Block") {
            PrintNode(body, indent + 2, false);
            w(" ");
          } else {
            w("\n");
            PrintNode(body, indent + 2);
            w("\n");
          }
          w("while (");
          PrintNode(st.condition);
          w(");");
          break;
        }
        case "ForStatement": {
          var st = (Statements.ForStatement)node;
          var init = st.init;
          var cond = st.condition;
          var afterInter = st.afterIteration;
          var body = st.body;
          w("for (");
          if (init != null) PrintNode(init);
          if (init?.Name != "DeclarationStatement") w(";");
          if (cond != null) {
            w(" ");
            PrintNode(cond);
          }
          w(";");
          if (afterInter != null) {
            w(" ");
            PrintNode(afterInter);
          }
          w(") ");
          if (body == null) {
            w(";"); break;
          }
          if (body.Name == "Block") PrintNode(body, indent, false);
          else {
            w("\n");
            PrintNode(body, indent + 2);
          }
          break;
        }
        case "BreakStatement":
          w("break;");
          break;
        case "ContinueStatement":
          w("continue;");
          break;
        case "ReturnStatement": {
          var ret = (Statements.ReturnStatement)node;
          w("return");
          if (ret.returnValue != null) {
            w(" ");
            PrintNode(ret.returnValue);
          }
          w(";");
          break;
        }
      }
    }
#endif
  }
}