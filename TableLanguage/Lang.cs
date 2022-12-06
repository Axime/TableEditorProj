using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
#if DEBUG
using T = System.Collections.Generic.List<TableLanguage.Lang.Token>;
using N = System.Collections.Generic.List<TableLanguage.Lang.Node>;
#endif

namespace TableLanguage {
  public static class Lang {
    public class SyntaxError : Exception {
      public int? pos;
      public SyntaxError(string msg, int? pos = null) : base(msg) {
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
        public Node lOp;
        public Node rOp;
        public BinaryExpression(Nodes.OperatorNode op, Node lOp, Node rOp) {
          this.op = op;
          this.lOp = lOp;
          this.rOp = rOp;
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

      public class IfStatement : Node {
        public Node condition;
        public Node then;
        public Node? @else;
        public IfStatement(Node condition, Node then, Node? @else) {
          this.condition = condition;
          this.then = then;
          this.@else = @else;
        }
      }

      public class WhileStatement : Node {
        public Node condition;
        public Node then;
        public WhileStatement(Node condition, Node then) {
          this.condition = condition;
          this.then = then;
        }
      }

      public class ForStatement : Node {
        public Node? init;
        public Node? condition;
        public Node? interation;
        public Node then;
        public ForStatement(Node? init, Node? condition, Node? interation, Node then) {
          this.init = init;
          this.interation = interation;
          this.then = then;
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
            throw new SyntaxError($"Missing initialer in const declaration {var.name}");
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
    }
#if DEBUG
    public
#endif
    static class Nodes {
      public class OperatorNode : Node {
        public Op type;
        public string text;
        public OperatorNode(Token op, bool isUnary = false) {
          if (!Operators.TryGetValue($"{op.type.name}{(isUnary ? "_unary" : "")}", out var type)) throw new Exception($"Unknown operator{op.text}");
          this.type = type;
          text = op.text;
        }
        public static OperatorNode? TryContruct(Token op, bool isUnary = false) {
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
          var _parseInt = (string num, int @base) => {
            long res = 0;
            int i = 0;
            foreach (char c in _reverseString(num)) {
              res += (c - '0') * (long)Math.Pow(@base, i);
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
            bool hasFractPart = false;
            if (parts.Length == 2)
              hasFractPart = hasIntPart = true;
            else if (num.StartsWith('.'))
              hasFractPart = true;
            else hasIntPart = true;
            if (hasIntPart) {
              string rawIntPart = parts[0];
              v += _parseInt(rawIntPart, 10);
            }
            if (hasFractPart) {
              int i = 1;
              string rawFractPart = hasIntPart ? parts[1] : parts[0];
              foreach (char c in rawFractPart) {
                v += (c - '0') * Math.Pow(10, -i);
                i++;
              }
            }
          }
        }
      }
    }
    private static class Parser {
      private class _Parser {
        private int pos = 0;
        private List<Token> tokens;
        private readonly List<Node> nodes = new();
        public _Parser(List<Token> tokens) {
          this.tokens = tokens;
        }
        static bool TokenIs(
          Token? token,
          string name,
          out Token? t
        ) => (token != null && token.type.name == name ? (t = token, true) : (t = null, false)).Item2;
        /*
         * All methods set pos to the next token
         * 
         */
        public List<Node> Parse() {
          while (pos < tokens.Count) {
            var t = tokens[pos];
            var tokenType = t.type;
            var tokenFlags = tokenType.flags;
            var tokenName = tokenType.name;
            if (tokenFlags.HasFlag(Flags.Declaration)) {
              nodes.Add(ParseDeclaration());
              continue;
            }
          }
          return nodes;
        }
        private void MoveToNextToken() => pos++;
        private Token? NextToken => tokens.Count - 1 > pos ? tokens[pos + 1] : null;
        private Token? CurrentToken => tokens.Count > pos ? tokens[pos] : null;
        private Token? PreviousToken => tokens.Count + 1 > pos ? tokens[pos - 1] : null;
        private Statements.DeclarationStatement ParseDeclaration() {
          // let <var_name> [= expr];
          // const <const_name> [= expr];
          var declToken = CurrentToken;
          if (!declToken!.type.flags.HasFlag(Flags.Declaration)) throw new Exception("Invalid DeclarationStatement");
          MoveToNextToken();// var_name
          if (!TokenIs(CurrentToken, TokenNames.Identifier, out var variableName)) throw new SyntaxError("Expectied indentifier", CurrentToken?.pos);
          MoveToNextToken(); // semicolon or assignment
          Node? init = null;
          if (TokenIs(CurrentToken, TokenNames.Assignment_operator, out var _)) {
            // assignment
            MoveToNextToken();
            init = ParseExpression(); // parsing initializer
          }
          if (!TokenIs(CurrentToken, TokenNames.Semicolon, out var _)) throw new SyntaxError("Expected semicolon", CurrentToken?.pos);
          MoveToNextToken();
          return new(declToken, new Nodes.VariableNode(variableName!.text), init);
        }
        private Node ParseExpression() => __parseExpr(ParsePrimary(), 0);
        private Node __parseExpr(Node lhs, int min_predence) {
          if (CurrentToken!.flags.HasFlag(Flags.Operator)) {
            Nodes.OperatorNode? lookahead = Nodes.OperatorNode.TryContruct(CurrentToken);
            while (lookahead != null && lookahead.type >= min_predence) {
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
                  rhs = __parseExpr(rhs, op.type + ((lookahead.type > op.type) ? 1 : 0));
                  lookahead = Nodes.OperatorNode.TryContruct(CurrentToken);
                }
              } else lookahead = null;
              lhs = new Statements.BinaryExpression(op, lhs, rhs);
            }
          }
          return lhs;
        }
        private Node ParsePrimary() {
          if (TokenIs(CurrentToken, TokenNames.OpRndBracket, out var _)) {
            // If current token is left opening par (
            // Parsing expression in parenthesis
            MoveToNextToken();
            var expr = ParseExpression();
            if (!TokenIs(CurrentToken, TokenNames.ClRndBracket, out var _)) throw new SyntaxError("Expected )", CurrentToken?.pos);
            MoveToNextToken();
            return expr;
          }
          var returnUnary = (
            Token UnaryOperatorToken
          ) => new Statements.UnaryExpression(new Nodes.OperatorNode(UnaryOperatorToken), ParsePrimary());
          MoveToNextToken();
          if (TokenIs(PreviousToken, TokenNames.Add_operator, out var unaryPlus)) return returnUnary(unaryPlus!);
          if (TokenIs(PreviousToken, TokenNames.Subtract_operator, out var unaryMinus)) return returnUnary(unaryMinus!);
          if (TokenIs(PreviousToken, TokenNames.NumberLiteral, out var num)) return new Nodes.NumberNode(num!.text);
          if (TokenIs(PreviousToken, TokenNames.Identifier, out var var)) return new Nodes.VariableNode(var!.text);
          throw new SyntaxError($"Unexpected token: {PreviousToken!.text}", PreviousToken?.pos);
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
      public Flags flags;
      public TokenType type;
      public Token(int pos, string text, Flags flags, TokenType type) {
        this.pos = pos;
        this.text = text;
        this.flags = flags;
        this.type = type;
      }
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
      Parenthesis = 4096,
      OpeningParenthesis = 8192,
      ClosingParenthesis = 16384,
      CirlceParenthesis = 32768,
      SquareParenthesis = 65536,
      CurlyureParenthesis = 131072,
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
      public static KeyValuePair<string, TokenType> ToDictItem(string name, string regex, Flags flags) => new(name, new TokenType(name, new Regex(regex, RegexOptions.Compiled), flags));
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
      [TokenNames.Eq_operator] = 6, // ==
      [TokenNames.StrictEq_operator] = 6, // ===
      [TokenNames.NotEq_operator] = 6, // !=
      [TokenNames.StrictNotEq_operator] = 6, // !==
      [TokenNames.LessOrEquals_operator] = 7, // <=
      [TokenNames.Less_operator] = 7, // <
      [TokenNames.GreaterOrEquals_operator] = 7, // >
      [TokenNames.Greater_operator] = 7, // >
      [TokenNames.Add_operator] = 9, // +
      [TokenNames.Subtract_operator] = 9, // -
      [TokenNames.Mult_operator] = 10, // *
      [TokenNames.Division_operator] = 10, // /
      [TokenNames.Remainder_operator] = 10, // %
      [TokenNames.Power_operator] = 11, // **,
      [$"{TokenNames.Add_operator}_unary"] = new(13, OperatorDirection.NonBinary), // +expr
      [$"{TokenNames.Subtract_operator}_unary"] = new(13, OperatorDirection.NonBinary), // -expr
      [TokenNames.Dot_operator] = 14, // variable.member
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
      public static readonly string ConstantDeclaration = "constant_declaratopn_keyword";
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
      public static readonly string While = "while_keyword";
      public static readonly string Return = "return keyword";
      public static readonly string Dot_operator = "dot_operator";
      public static readonly string Comma_operator = "comma_operator";
      public static readonly string Break = "break_keyword";
      public static readonly string Continue = "continue_keyword";
      public static readonly string Eq_operator = "equals_operator";
      public static readonly string StrictEq_operator = "strict_equals_operator";
      public static readonly string NotEq_operator = "not_equals_operator";
      public static readonly string StrictNotEq_operator = "strict_not_equals_operator";
      public static readonly string Greater_operator = "greater_operator";
      public static readonly string Less_operator = "less_operator";
      public static readonly string GreaterOrEquals_operator = "greater_or_equals_operator";
      public static readonly string LessOrEquals_operator = "less_or_equals_operator";
      public static readonly string Increment_operator = "increment_operator";
      public static readonly string Decrement_operator = "decrement_operator";

      public static readonly string Identifier = "identifier";
    }
    private static class Lexer {
      private static readonly Dictionary<string, TokenType> Tokens = new(new List<KeyValuePair<string, TokenType>>() {
        TokenType.ToDictItem(TokenNames.Space, @"^\s+", Flags.Endline | Flags.Whitespace),
        TokenType.ToDictItem(TokenNames.Add_operator, @"^\+(?![\+=])", Flags.Operator),
        TokenType.ToDictItem(TokenNames.Subtract_operator, @"^\-(?![\-=])", Flags.Operator),
        TokenType.ToDictItem(TokenNames.Mult_operator, @"^\*(?![\*\=])", Flags.Operator),
        TokenType.ToDictItem(TokenNames.Division_operator, @"^/(?!=)", Flags.Operator),
        TokenType.ToDictItem(TokenNames.Remainder_operator, "^%(?!=)", Flags.Operator),
        TokenType.ToDictItem(TokenNames.Power_operator, @"^\*\*", Flags.Operator),
        TokenType.ToDictItem(TokenNames.Increment_operator, @"^\+\+", Flags.Operator),
        TokenType.ToDictItem(TokenNames.Decrement_operator, @"^\-\-", Flags.Operator),
        TokenType.ToDictItem(TokenNames.Dot_operator, @"^\.(?!\d)", Flags.Operator),
        TokenType.ToDictItem(TokenNames.Comma_operator, "^,", Flags.Operator),
        TokenType.ToDictItem(TokenNames.Semicolon, "^;", Flags.InstructionSeparator),
        TokenType.ToDictItem(TokenNames.Assignment_operator, @"^=(?!=)", Flags.Operator),
        TokenType.ToDictItem(TokenNames.AssignmentAddition_operator, @"\+=", Flags.Operator),
        TokenType.ToDictItem(TokenNames.AssignmentSubtraction_operator, @"\-=", Flags.Operator),
        TokenType.ToDictItem(TokenNames.AssignmentMultiplication_operator, @"^\*=", Flags.Operator),
        TokenType.ToDictItem(TokenNames.AssignmentDivision_operator, @"\/=", Flags.Operator),
        TokenType.ToDictItem(TokenNames.AssignmentRemainder_operator, @"%=", Flags.Operator),
        TokenType.ToDictItem(TokenNames.NumberLiteral, @"^0b[01]+|^0x[0-9a-fA-F]+|^\.\d+|^\d+(\.\d*)?", Flags.Literal | Flags.NumericLiteral),
        TokenType.ToDictItem(TokenNames.StringLiteral, "^\".*?\"|^'.*?'", Flags.StringLiteral | Flags.Literal),
        TokenType.ToDictItem(TokenNames.VariableDeclaration, "^let", Flags.Declaration | Flags.VariableDeclaration | Flags.Keyword),
        TokenType.ToDictItem(TokenNames.ConstantDeclaration, "^const", Flags.Declaration | Flags.ConstantDeclaration | Flags.Keyword),
        TokenType.ToDictItem(TokenNames.OpRndBracket, @"^\(", Flags.OpeningParenthesis | Flags.CirlceParenthesis | Flags.Parenthesis),
        TokenType.ToDictItem(TokenNames.ClRndBracket, @"^\)", Flags.ClosingParenthesis | Flags.CirlceParenthesis | Flags.Parenthesis),
        TokenType.ToDictItem(TokenNames.OpSqBracket, @"^\[", Flags.OpeningParenthesis | Flags.SquareParenthesis | Flags.Parenthesis),
        TokenType.ToDictItem(TokenNames.ClSqBracket, @"^\]", Flags.ClosingParenthesis | Flags.SquareParenthesis | Flags.Parenthesis),
        TokenType.ToDictItem(TokenNames.OpCurlyBracket, @"^\{", Flags.OpeningParenthesis | Flags.CurlyureParenthesis | Flags.Parenthesis),
        TokenType.ToDictItem(TokenNames.ClCurlyBracket, @"^\}", Flags.ClosingParenthesis | Flags.CurlyureParenthesis | Flags.Parenthesis),
        TokenType.ToDictItem(TokenNames.Function, "^function", Flags.Keyword),
        TokenType.ToDictItem(TokenNames.If, "^if", Flags.Keyword),
        TokenType.ToDictItem(TokenNames.Else, "else", Flags.Keyword),
        TokenType.ToDictItem(TokenNames.For, "^for", Flags.Keyword),
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

        //TokenType.ToDictItem(TokenNames.Identifier, "^[$a-zA-Zа-яА-ЯёЁ_][\\w$_]*(?=[\\(\\)+\\-\\*\\/=;:\\s\\.,\\?%\\|\\[\\]\\{\\}\"]')", Flags.Identifier),
        TokenType.ToDictItem(TokenNames.Identifier, "^[$a-zA-Zа-яА-ЯёЁ_][$a-zA-Zа-яА-ЯёЁ_0-9]*", Flags.Identifier),
      });
      public static List<Token> Analysis(string code) {
        List<Token> tokens = new();
        int pos = 0;
        while (Next(code, ref tokens, ref pos)) ;
        tokens.RemoveAll(t => t.flags.HasFlag(Flags.Whitespace));
        return tokens;

      }
      private static bool Next(string code, ref List<Token> tokens, ref int pos) {
        if (pos >= code.Length) return false;
        foreach (var token in Tokens) {
          var t = token.Value;
          var m = t.regex.Match(code[pos..]);
          if (!m.Success) continue;
          tokens.Add(new(pos, m.Value, t.flags, t));
          pos += m.Length;
          return true;
        }
        throw new SyntaxError($"Syntax Error at position {pos}:\n{code[pos..]}", pos);
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
#endif
  }
}