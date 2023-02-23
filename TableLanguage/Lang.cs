using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using N = System.Collections.Generic.List<TableLanguage.Lang.Node>;
using T = System.Collections.Generic.List<TableLanguage.Lang.Token>;
using R = TableLanguage.Lang.Runtime.IRuntimeEntity;
using NameRefDict = System.Collections.Generic.Dictionary<string, TableLanguage.Lang.Runtime.Reference>;

namespace TableLanguage {
  public static class Lang {
    private static class StandardLibrary {
      public interface IObject {
        public Runtime.Object obj { get; init; }
        public IObject? Prototype { get; init; }
      }
      private record ObjProto : IObject {
        public Runtime.Object obj { get; init; } = null!;
        public IObject? Prototype { get; init; }
      }
      interface IPrimitiveWrapper<T> {
        public T Value { get; set; }
        public Runtime.Reference ValueOf();
      }
      internal static class Prototypes {
        public static readonly IObject ObjectPrototype = new ObjProto {
          obj = new Runtime.Object(new() {
            ["toString"] = new(new Runtime.NativeFunction((f, @this, n) => "[object Object]", "toString"), true, Runtime.Reference.RefType.lvalue),
            ["__proto__"] = null!,
          }, null),
          Prototype = (IObject?)null,
        };
        public static readonly IObject NumberPrototype = new ObjProto {
          obj = new(new() {
            ["toString"] = new(new Runtime.NativeFunction((env, @this, args) => {
              var value = Runtime.GetObjectProperty(@this, "valueOf").Val as Runtime.ICallable;
              return (string)value?.Call(env, @this, new())!;

            }, "toString"), true, Runtime.Reference.RefType.lvalue, null, true),
            ["__proto__"] = new(ObjectPrototype.obj, true, Runtime.Reference.RefType.lvalue, "__proto__", true),
          }, null),
          Prototype = ObjectPrototype,
        };

      }
      public static readonly Runtime.Module Global = new(new() {
        ["Math"] = new(new Runtime.Object(new() {
          ["sin"] = new(new Runtime.NativeFunction((env, @this, args) => {
            var arg = (double)args[0];
            return Math.Sin(arg);
          }, "sin"), true, Runtime.Reference.RefType.lvalue, null, true),
          ["cos"] = new(new Runtime.NativeFunction((env, @this, args) => {
            var arg = (double)args[0];
            return Math.Cos(arg);
          }, "cos"), true, Runtime.Reference.RefType.lvalue, null, true),
          ["tg"] = new(new Runtime.NativeFunction((env, @this, args) => {
            var arg = (double)args[0];
            return Math.Tan(arg);
          }, "tg"), true, Runtime.Reference.RefType.lvalue, null, true)
        }), true, Runtime.Reference.RefType.lvalue, null, true),
      });

      public class Object : IObject {
        public IObject? Prototype { get; init; } = Prototypes.ObjectPrototype;
        public Runtime.Object obj { get; init; } = new(new() { });
        public Object(Runtime.Object obj) {
          this.obj = obj;
        }
      }
      public class Number : IPrimitiveWrapper<double>, IObject {
        public IObject? Prototype { get; init; } = Prototypes.NumberPrototype;
        public Runtime.Object obj { get; init; } = new(new() {
          ["MAX"] = new(new Runtime.NumberConstant(double.MaxValue), false, Runtime.Reference.RefType.rvalue, null, true),
          ["__proto__"] = new(Prototypes.NumberPrototype.obj, true, Runtime.Reference.RefType.lvalue, null, true),
        });
        public double Value { get; set; }
        public Number(double v = 0) {
          Value = v;
          obj.props.Add("valueOf", new(new Runtime.NativeFunction((env, @this, args) => ValueOf(), "valueOf"), true, Runtime.Reference.RefType.lvalue, null, true));
        }
        public Runtime.Reference ValueOf() => Value;
      }
    }
    public class Runtime {
      public enum RuntimeEntityType {
        Number,
        Boolean,
        String,
        Object,
        Function,
        Null
      }
      public class Reference {
        public enum RefType {
          rvalue,
          lvalue
        }
        public R? Val { get; set; }
        public bool isConst;
        public bool isRef;
        public string? propName;
        public RefType type;
        public Reference? owner = null;
        public Reference(R? value, bool isRef, RefType type, in Reference? owner = null, bool isConst = false, string? propName = null) {
          Val = value;
          this.isRef = isRef;
          this.owner = owner;
          this.type = type;
          this.propName = propName;
          if (!isRef) {
            this.isConst = true;
            this.type = RefType.rvalue;
          } else {
            this.isConst = isConst;
            this.type = RefType.lvalue;
          }
          addOwnerAndPropNames(this);
        }
        public static void addOwnerAndPropNames(in Reference obj) {
          if (obj.Val is not Object o) return;
          foreach (var (key, value) in o.props) {
            if (value == null) continue;
            value.propName = key;
            value.owner = obj;
            if (value.Val is Object) addOwnerAndPropNames(value);
          }
        }
        public static explicit operator bool(Reference r) => (TypeCast(r, RuntimeEntityType.Boolean).Val as BooleanConstant)!.value!;
        public static explicit operator double(Reference r) => (TypeCast(r, RuntimeEntityType.Number).Val as NumberConstant)!.value!;
        public static explicit operator string(Reference r) => (TypeCast(r, RuntimeEntityType.String)?.Val as StringConstant)!.str!;
        public static implicit operator Reference(bool v) => new(new BooleanConstant(v), false, RefType.rvalue);
        public static implicit operator Reference(string v) => new(new StringConstant(v), false, RefType.rvalue);
        public static implicit operator Reference(double v) => new(new NumberConstant(v), false, RefType.rvalue);
      }

      public interface IRuntimeEntity {
        public RuntimeEntityType Type { get; }
      }
      public interface ICallable : R {
        public Reference? Call(Runner env, Reference? owner, List<Reference> args);
      }

      internal class Function : ICallable {
        Statements.FunctionDeclarationStatement func;
        public RuntimeEntityType Type => RuntimeEntityType.Function;
        public string? Name => func.name;
        public Function(Statements.FunctionDeclarationStatement func) {
          this.func = func;
        }
        public Reference? Call(Runner env, Reference? owner, List<Reference> args) {
          for (int i = 0; i < func.args.Count; i++) {
            var arg = args[i];
            env.scopes.AddVarToCurrentScope(func.args[i].name, arg);
          }
          env.scopes.AddVarToCurrentScope("this", owner!);
          foreach (var node in func.body) {
            env.ExecNode(node, out var s);
            if (s?.T == Runner.ISpecSignal.Type.Return) return s.ReturnValue;
          }
          return null;
        }
      }
      public class NativeFunction : ICallable {
        public RuntimeEntityType Type => RuntimeEntityType.Function;
        public string? Name;
        public Func<Runner, Reference?, List<Reference>, Reference> func;
        public NativeFunction(Func<Runner, Reference?, List<Reference>, Reference> func, string? name) {
          this.func = func;
          this.Name = name;
        }
        public Reference? Call(Runner env, Reference? owner, List<Reference> args) => func(env, owner, args);
      }
      internal class Object : R {
        public RuntimeEntityType Type => RuntimeEntityType.Object;
        public NameRefDict props = new();
        public Object(NameRefDict props) : this(props, StandardLibrary.Prototypes.ObjectPrototype.obj) { }
        public Object(NameRefDict props, Object? proto) {
          this.props = props;
          if (proto != null) this.props.Add(
              "__proto__",
              new(proto, true, Reference.RefType.lvalue, null, true, "__proto__")
            );
        }

      }
      internal class NumberConstant : R {
        public double value = 0;
        public RuntimeEntityType Type => RuntimeEntityType.Number;
        public NumberConstant(double v) {
          this.value = v;
        }
      }
      internal class StringConstant : R {
        public string str;
        public RuntimeEntityType Type => RuntimeEntityType.String;
        public StringConstant(string str) {
          this.str = str;
        }
      }
      internal class BooleanConstant : R {
        public bool value;
        public bool Value => value;
        public RuntimeEntityType Type => RuntimeEntityType.Boolean;
        public BooleanConstant(bool value) { this.value = value; }
      }
      public class Module {
        internal NameRefDict entities;
        public Module(NameRefDict entities) {
          this.entities = entities;
          foreach (var (_, value) in entities)
            if (value.Val is Object) Reference.addOwnerAndPropNames(value);
        }
      }

      internal static Reference SetObjectProperty(in Reference prop, in Reference value) {
        if (prop?.owner?.Val is not Object o) throw new TypeError($"Cannot set properties of non-object");
        value.type = Reference.RefType.lvalue;
        o.props[prop.propName!] = value;
        return value;
      }
      internal static Reference GetObjectProperty(in Reference obj, in Reference name) {
        // get property name as string
        string propName = (string)name;
        if (propName == null) throw new ReferenceError("Cannot cast property name to \"string\"");
        if (obj.Val == null) throw new ReferenceError("Cannot read property of null");
        if (obj.Val is Object o) {
          Object tmp = o;
          // if current object doesn't have property check it's prototype
          do if (tmp.props.TryGetValue(propName, out var value)) return new Reference(value.Val, true, Reference.RefType.lvalue, obj, false, propName);
          while ((tmp.props.TryGetValue("__proto__", out var proto) && (tmp = (proto.Val as Object)!) != null));
          return new Reference(null, true, Reference.RefType.rvalue, null, true, propName);
        } else if (obj.Val is StringConstant oStr) {
          throw new NotImplementedException();
        } else if (obj.Val is NumberConstant oNum) {
          var wrapper = new StandardLibrary.Number(oNum.value);
          return GetObjectProperty(
           new(wrapper.obj, true, Reference.RefType.lvalue, null, true),
            propName
            );
        } else if (obj.Val is Function oFun) {
          throw new NotImplementedException();
        } else if (obj.Val is BooleanConstant oBool) {
          throw new NotImplementedException();
        }
        throw new NotImplementedException();
      }

      internal static Reference TypeCast(in Reference obj, RuntimeEntityType type) {
        if (obj == null) throw new ReferenceError();
        if (obj.Val?.Type == type) return obj;
        return type switch {
          RuntimeEntityType.Boolean => obj.Val switch {
            NumberConstant num when num.value != 0 => true,
            NumberConstant => false,
            StringConstant str when string.IsNullOrWhiteSpace(str.str) => false,
            StringConstant => true,
            Object or NativeFunction or Function => true,
            null => false,
            _ => false
          },
          RuntimeEntityType.Number => obj.Val switch {
            BooleanConstant b when b.value => 1,
            BooleanConstant => 0,
            Object or Function or NativeFunction => double.NaN,
            StringConstant { str: var str } => double.Parse(str),
            _ => throw new NotImplementedException()
          },
          RuntimeEntityType.String => obj.Val switch {
            BooleanConstant b => b.value.ToString()!,
            NumberConstant n => n.value.ToString()!,
            NativeFunction { Name: var n } => $"function {(n ?? "")}() {{ [native code] }}",
            Function { Name: var n } => $"function {n}() {{}}",
            Object => "[object Object]",
            _ => throw new NotImplementedException(),

          },
          _ => throw new NotImplementedException()
        };
      }

      public class Runner {
        internal class Scopes {
          internal class Scope {
            public bool IsGlobal { init; get; } = false;
            private readonly NameRefDict vars = new();
            public void AddVar(in string name, in Reference var) => SetVar(name, var);
            public Reference GetVar(in string name) => vars[name];
            public void SetVar(in string name, in Reference var) => vars[name] = var;
            public bool HasVar(in string name) => vars.ContainsKey(name);
          }
          private readonly List<Scope> scopes = new() { new() { IsGlobal = true } };
          public bool HasCurrentScopeVar(in string name) => scopes[^1].HasVar(name);
          public void AddVarToCurrentScope(in string name, in Reference var) => scopes[^1].AddVar(name, var);
          public void CreateScope() => scopes.Add(new());
          public void DeleteScope() {
            if (scopes.Count == 0) return;
            scopes.RemoveAt(scopes.Count - 1);
          }
          public Scope GlobalScope => scopes[0];
          public Reference GetVar(in string name) {
            for (int i = scopes.Count - 1; i >= 0; i--) {
              var scope = scopes[i];
              if (scope.HasVar(name)) return scope.GetVar(name);
            }
            throw new ReferenceError($"Variable or constant \"{name}\" is not defined");
          }
        }

        internal readonly Scopes scopes = new();
        internal Runner(in N? nodes, in List<Module>? modules = null) {
          //this.nodes = nodes;
          var _m = modules ?? new();
          _m.Add(StandardLibrary.Global);
          foreach (var module in _m) {
            foreach (var (name, value) in module!.entities) {
              scopes.AddVarToCurrentScope(name, value);
            }
          }
          if (nodes != null) Exec(nodes);
        }
        internal void Exec(in N? nodes) {
          if (nodes == null) return;
          foreach (var node in nodes) {
            ExecNode(node);
          }
        }
        internal Reference? ExecOneNode(in Node n) => ExecNode(n);

        internal class ISpecSignal {
          public enum Type {
            Break,
            Continue,
            Return
          }
          public Type T { get; init; }
          public Reference? ReturnValue { get; init; } = null;
        }

        private Reference? ExecNode(in Node? n) => ExecNode(n, out var _);

        internal Reference? ExecNode(in Node? n, out ISpecSignal? signal) {
          signal = null;
          if (n == null) return null;
          return n switch {
            Nodes.ObjectNode obj => ExecObject(obj),
            Nodes.ArrayNode arr => ExecArray(arr),
            Nodes.BooleanNode b => ExecBool(b),
            Nodes.NumberNode num => ExecNumber(num),
            Nodes.StringNode str => ExecString(str),
            Nodes.NullNode => ExecNull(),
            Nodes.VariableNode var => scopes.GetVar(var.name),
            Statements.DeclarationStatement stm => ExecDeclaration(stm),
            Statements.FunctionDeclarationStatement stm => ExecFunctionDeclarationStatement(stm),
            Statements.FunctionCall fc => ExecFunctionCall(fc),
            Statements.MemberAccessStatement {
              left: var left,
              right: var right,
              dotAndIdentifier: var notExecRight
            } => GetObjectProperty(ExecExpression(left), notExecRight ? new(new StringConstant((right as Nodes.StringNode)!.str), false, Reference.RefType.rvalue) : ExecExpression(right)),
            Statements.UnaryExpression {
              operand: var operand,
              op: var op
            } => ExecUnaryOperator(ExecNode(operand), op),
            Statements.BinaryExpression {
              left: var left,
              op: var op,
              right: var right
            } => ExecBinaryOperator(ExecExpression(left), ExecExpression(right), op),
            Statements.IfStatement ifStmnt => (ExecIfStatement(ifStmnt, out var s), signal = s).Item1,
            Statements.ForStatement forStm => (ExecForStatement(forStm, out var s), signal = s).Item1,
            Statements.Block block => (ExecBlock(block, out var s), signal = s).Item1,
            Statements.BreakStatement => ((Reference?)null, signal = new() { T = ISpecSignal.Type.Break }).Item1,
            Statements.ContinueStatement => ((Reference?)null, signal = new() { T = ISpecSignal.Type.Continue }).Item1,
            Statements.ReturnStatement {
              returnValue: var rv
            } => ((Reference?)null, signal = new() { ReturnValue = ExecNode(rv), T = ISpecSignal.Type.Return }).Item1,
            _ => throw new ArgumentException("Parameter n doesn't match any pattern")
          };
        }
        private Reference ExecNumber(in Nodes.NumberNode num) => new(new NumberConstant(num.v), false, Reference.RefType.rvalue);
        private Reference ExecString(in Nodes.StringNode str) => new(new StringConstant(str.str), false, Reference.RefType.rvalue);
        private Reference ExecBool(in Nodes.BooleanNode boolean) => new(new BooleanConstant(boolean.value), false, Reference.RefType.rvalue);
        private Reference ExecNull() => new(null, true, Reference.RefType.rvalue);
        private Reference ExecArray(in Nodes.ArrayNode array) {
          throw new NotImplementedException();
        }
        private Reference ExecObject(in Nodes.ObjectNode obj) {
          NameRefDict props = new();
          foreach (var (key, value) in obj.props) {
            props.Add((string)ExecExpression(key), ExecExpression(value));
          }
          var o = new Object(props);
          return new Reference(o, true, Reference.RefType.rvalue);
        }

        private Reference? ExecFunctionDeclarationStatement(Statements.FunctionDeclarationStatement func) {
          Reference f = new(new Function(func), true, Reference.RefType.lvalue, null, true);
          if (func.isDeclaration) scopes.AddVarToCurrentScope(func.name!, f);
          return f;
        }

        private Reference? ExecFunctionCall(in Statements.FunctionCall fc) {
          var funcRef = ExecNode(fc.callee);
          var func = funcRef.Val;
          if (func is ICallable f) {
            var args = new List<Reference>();
            foreach (var arg in fc.args) {
              args.Add(ExecExpression(arg));
            }
            scopes.CreateScope();
            var res = f.Call(this, funcRef.owner, args);
            scopes.DeleteScope();
            return res;
          }
          throw new ReferenceError("Expression cannot be called");
        }

        private Reference ExecDeclaration(Statements.DeclarationStatement declaration) {
          if (scopes.HasCurrentScopeVar(declaration.varName)) throw new ReferenceError("Variable with such name already declared in this scope");
          var init = declaration.init == null ? new Reference(null, true, Reference.RefType.lvalue) : ExecExpression(declaration.init);
          init.isConst = declaration.type == Statements.DeclarationStatement.Type.constant;
          init.type = Reference.RefType.lvalue;
          var varValue = ExecBinaryOperator(new(null, true, Reference.RefType.lvalue), init, new(new(0, "=", Tokens[TokenNames.Assignment_operator])));
          varValue.isConst = init.isConst;
          scopes.AddVarToCurrentScope(declaration.varName, varValue);
          return new(null, true, Reference.RefType.rvalue, null, true);
        }
        private Reference ExecIfStatement(in Statements.IfStatement ifStm, out ISpecSignal? signal) {
          if (TypeCast(ExecNode(ifStm.condition), RuntimeEntityType.Boolean).Val is not BooleanConstant condition)
            throw new ArgumentException("Can not cast to boolean");
          if (condition.value) {
            ExecNode(ifStm.body, out var s);
            signal = s;
          } else if (ifStm.elseBody != null) {
            ExecNode(ifStm.elseBody, out var s);
            signal = s;
          } else signal = null;
          return null;
        }

        private Reference ExecForStatement(in Statements.ForStatement forStm, out ISpecSignal? signal) {
          signal = null;
          scopes.CreateScope();
          if (forStm.init != null) ExecNode(forStm.init);
          while (forStm.condition != null && (bool)ExecNode(forStm.condition) == true) {
            if (forStm.body != null) {
              ExecNode(forStm.body, out var s);
              if (s != null) {
                if (s.T == ISpecSignal.Type.Break) break;
                if (s.T == ISpecSignal.Type.Return) {
                  signal = s;
                  break;
                }
              }
            };
            if (forStm.afterIteration != null) ExecNode(forStm.afterIteration);
          }
          scopes.DeleteScope();
          return null;
        }

        private Reference ExecBlock(in Statements.Block block, out ISpecSignal? signal) {
          signal = null;
          scopes.CreateScope();
          foreach (var node in block.nodes) {
            ExecNode(node, out var s);
            if (s != null) {
              signal = s;
              break;
            }
          };
          scopes.DeleteScope();
          return null;
        }

        private Reference ExecExpression(in Node node) {
          return ExecNode(node);
        }
        private Reference ExecUnaryOperator(in Reference operand, in Nodes.OperatorNode op) {
          return op.text switch {
            "!" => !(bool)operand,
            "~" => ~(long)operand,
            "+" => (long)operand,
            "-" => -(long)operand,
            _ => throw new ArgumentException(),
          };
        }
        internal Reference ExecBinaryOperator(in Reference left, in Reference right, in Nodes.OperatorNode op) {
          var opText = op.text;
          //if (left.Val == null && right.Val != null) return ExecBinaryOperator(right, left, op);
          switch (opText) {
            case "&&": {
              if ((bool)left) return right;
              return left;
            }
            case "||": {
              if ((bool)left) return left;
              return right;
            }
            case "===": {
              if (left.Val?.Type != right.Val?.Type) return false;
              if (!left.isRef && !right.isRef) {
                return left.Val switch {
                  NumberConstant { value: var v1 } => (right.Val as NumberConstant)!.value == v1,
                  StringConstant { str: var str } => (right.Val as StringConstant)!.str == str,
                  BooleanConstant { value: var v1 } => (right.Val as BooleanConstant)!.value == v1,
                  _ => throw new ArgumentException()
                };
              };
              return left.Val == right.Val;
            }
            case "!==": return !(ExecBinaryOperator(left, right, new(new(0, "!==", new("", new(""), Flags.Operator)))).Val as BooleanConstant)!.value;
            case "=": {
              if (left.owner != null) return SetObjectProperty(left, right);
              if (left.type == Reference.RefType.rvalue) throw new ReferenceError("Invalid lefthand reference assignment");
              if (left.isConst) throw new ReferenceError("Cannot assign to const variable");
              left.Val = right.Val switch {
                NumberConstant num => new NumberConstant(num.value),
                BooleanConstant b => new BooleanConstant(b.value),
                _ => right.Val
              };
              left.isRef = right.isRef;
              return left;

            }
            case ",": return right;
            default:
              var _ = opText switch {
                "-" or "*" or "/" or "**" or "%" or "&" or "|" or "^"
                when left.Val is not NumberConstant || right.Val is not NumberConstant => ExecBinaryOperator(
                TypeCast(left, RuntimeEntityType.Number),
                TypeCast(right, RuntimeEntityType.Number),
                op
                ),
                "+=" or "-=" or "/=" or "*=" or "**=" or "%=" or "&&=" or "||="
                or "|=" or "&=" or "^=" => ExecBinaryOperator(
                    left,
                    ExecBinaryOperator(left, right, new(new(0, opText[..^1], Tokens[TokenNames.Add_operator]))),
                    new(new(0, "=", Tokens[TokenNames.Assignment_operator]))
                  ),
                _ => null,
              };
              if (_ != null) return _;
              break;

          }

          if (left.Val is StringConstant leftStr && right.Val is StringConstant rightStr) {
            var lv = leftStr.str;
            var rv = rightStr.str;
            return op.text switch {
              "+" => lv + rv,
              _ => throw new ArgumentException(),
            };
          }

          if (left.Val is NumberConstant leftNum && right.Val is NumberConstant rightNum) {
            var lv = leftNum.value;
            var rv = rightNum.value;
            // both are numbers
            return opText switch {
              "+" => (Reference)(lv + rv),
              "-" => lv - rv,
              "*" => lv * rv,
              "/" => lv / rv,
              "**" => Math.Pow(lv, rv),
              "%" => lv % rv,
              "&" => (long)lv & (long)rv,
              "|" => (long)lv | (long)rv,
              "^" => (long)lv ^ (long)rv,
              ">" => lv > rv,
              "<" => lv < rv,
              "<=" => lv <= rv,
              ">=" => lv > rv,
              "==" => lv.CompareTo(rv) == 0,
              "!=" => lv.CompareTo(rv) != 0,
              _ => throw new ArgumentException("Unknown operator")
            };
          }
          if (left.Val is BooleanConstant bool1 && right.Val is BooleanConstant bool2) {
            var lv = bool1.value;
            var rv = bool2.value;
            return opText switch {
              //"+" or "-" or "*" or "/" or "**" or "%" or "&" or "|" or "^" or ">" or ">=" or "<=" or "<" => ExecBinaryOperator(
              //  TypeCast(left, RuntimeEntityType.Number),
              //  TypeCast(right, RuntimeEntityType.Number),
              //  op
              //  ),
              "==" => lv == rv,
              "!=" => lv != rv
            };
          }
          if ((left.Val is StringConstant || right.Val is StringConstant) && op.text == "+")
            return (string)left + (string)right;
          if (left.Val?.Type != right.Val?.Type) {
            throw new NotImplementedException();
          }
          throw new Error($"Cannot use operator {op.text} with types {left.Val.Type} and {left.Val.Type}");
        }
      }
    }
    public class Error : Exception {
      public Error(in string msg, in string errName) : base($"{errName}: {msg}") { }
      public Error(in string msg) : this(msg, "Error") { }
    }
    public class SyntaxError : Error {
      public int? pos;
      public SyntaxError(in string msg, in int? pos = null) :
        base($"{msg}{(pos != null ? $" at position {pos}" : "")}", "SyntaxError") {
        this.pos = pos;
      }
    }
    public class ReferenceError : Error {
      public ReferenceError(in string msg = "") : base(msg, "ReferenceError") { }
    }
    public class TypeError : Error {
      public TypeError(string msg = "") : base(msg, "TypeError") { }
    }
    protected internal abstract class Node {
      public string Name => GetType().Name;
    }
    internal static class Statements {
      public class Block : Node {
        public N nodes;
        public Block(N nodes) {
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
        public bool dotAndIdentifier;
        public MemberAccessStatement(
          Nodes.OperatorNode op,
          Node left,
          Node right,
          bool dotAndIdentifier
          ) : base(
            op, left,
            dotAndIdentifier ? new Nodes.StringNode($"\"{(right as Nodes.VariableNode)!.name}\"") : right
            ) {
          this.dotAndIdentifier = dotAndIdentifier;
        }
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
        public N body;
        public string? name;
        public List<Nodes.VariableNode> args;
        public bool anonymous;
        public bool isDeclaration;
        public FunctionDeclarationStatement(N body, string? name, List<Nodes.VariableNode> args, bool isDeclaration) {
          this.body = body;
          this.name = name;
          anonymous = name == null;
          this.args = args;
          this.isDeclaration = isDeclaration;
        }
      }
      public class FunctionCall : Node {
        public N args;
        public Node callee;
        public FunctionCall(N args, Node callee) {
          this.args = args;
          this.callee = callee;
        }
      }
    }
    internal static class Nodes {
      public class OperatorNode : Node {
        public Op type;
        public string text;
        public int pos;
        public OperatorNode(in Token op, bool isUnary = false) {
          if (!Operators.TryGetValue($"{op.type.name}{(isUnary ? "_unary" : "")}", out var type)) throw new Exception($"Unknown operator{op.text}");
          this.type = type;
          text = op.text;
          pos = op.pos;
        }
        public static OperatorNode? TryConstruct(in Token op, bool isUnary = false) {
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

      public class NullNode : Node { }

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
        public string str = "";
        public StringNode(in string str) {
          bool waitNextCharAfterBackslash = false;
          var isSingleQuoted = str[0] == '\'';
          foreach (var c in str[1..^1]) {
            if (c == '\\') {
              waitNextCharAfterBackslash = true;
              continue;
            }
            if (waitNextCharAfterBackslash) {
              this.str += c switch {
                '\\' => "\\",
                'n' => "\n",
                't' => "\t",
                'r' => "\r",
                'f' => "\f",
                '0' => "\0",
                'a' => "\a",
                'v' => "\v",
                '"' => "\"",
                _ => throw new SyntaxError($"Unknown escape sequence: \\{c}"),
              };
              waitNextCharAfterBackslash = false;
              continue;
            }
            if (isSingleQuoted && c == '\'') throw new SyntaxError("Unescaped ' in string");
            if (!isSingleQuoted && c == '"') throw new SyntaxError("Unescaped \" in string");
            this.str += c;
          }
        }
      }
      public class ObjectNode : Node {
        public Dictionary<Node, Node> props;
        public ObjectNode(Dictionary<Node, Node> props) {
          this.props = props;
        }
      }
      public class ArrayNode : Node {
        public N elements;
        public ArrayNode(N elements) {
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
        private readonly T tokens;
        private readonly N nodes = new();
        private void MoveToNextToken() => pos++;
        private Token? CurrentToken => tokens.Count > pos ? tokens[pos] : null;
        private Token? PreviousToken => tokens.Count + 1 > pos ? tokens[pos - 1] : null;
        private bool CurrentTokenIs(string name, out Token? t) => TokenIs(CurrentToken, name, out t);
        public _Parser(T tokens) {
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
        public N Parse(int count = int.MaxValue, string? WhileToken = null) {
          var localNodes = new N();
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
        private N ParseCommas(string stopToken) {
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
          N nodes = new();
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
          N body = Parse(int.MaxValue, TokenNames.ClCurlyBracket);
          return new(body, name?.text, argNames, !isExpr);
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
                      ParseExpression(),
                      false
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
              if (op.text == ".") lhs = new Statements.MemberAccessStatement(op, lhs, rhs, rhs.Name == "VariableNode");
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
              if (key is Nodes.VariableNode var) key = new Nodes.StringNode($"\"{var.name}\"");
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
          N elems = new();
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
          if (PreviousToken == TokenNames.NullLiteral) return new Nodes.NullNode();
          if (PreviousToken != (Token?)null && PreviousToken.flags.HasFlag(Flags.BoolLiteral)) return new Nodes.BooleanNode(PreviousToken!);
          pos--;
          return null;
          //ThrowSyntaxError($"Unexpected token: {PreviousToken!.text}", PreviousToken?.pos);
        }
      }
      public static N Parse(T tokens) {
        _Parser p = new(tokens);
        return p.Parse();
      }
    }
    internal class Token {
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
    protected internal enum Flags {
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
    protected internal class TokenType {
      public string name;
      public Regex regex;
      public Flags flags;
      public TokenType(string name, Regex regex, Flags flags) {
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
    protected internal enum OperatorDirection {
      LeftToRight = 0,
      RightToLeft = 1,
      NonBinary = 2
    }
    protected internal class Op {
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
    private static class Lexer {
      public static T Analysis(string code) {
        T tokens = new();
        int pos = 0;
        while (Next(code, ref tokens, ref pos)) ;
        tokens.RemoveAll(t => t.flags.HasFlag(Flags.Whitespace) || t.flags.HasFlag(Flags.Comment));
        return tokens;

      }
      private static bool Next(string code, ref T tokens, ref int pos) {
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
      public static int Run(string code, out Exception? exception) {
        var t = Lexer.Analysis(code);
        var a = Parser.Parse(t);
        var res = new Runtime.Runner(a, new() {
          new(new() {
            ["AddOne"] = new(new Runtime.NativeFunction((env, @this, args) => {
              var val = args[0];
              if (val.Val is Runtime.NumberConstant num) {
                var console = env.scopes.GlobalScope.GetVar("console");
                var logRef = Runtime.GetObjectProperty(console, "log");
                if (logRef.Val is Runtime.ICallable log) {
                  log.Call(env, logRef.owner, new() { "before: ", num.value, "\n after: ", num.value + 1 }) ;
                }
                num.value += 1;
                return num.value + 1;
              }
              throw new TypeError("Illegal type: expected number");
            }, "AddOne"), true, Runtime.Reference.RefType.lvalue),
            ["console"] = new(new Runtime.Object(new() {
              ["log"] = new(new Runtime.NativeFunction((env, @this, args) => {
                foreach (var arg in args) {
                  var str = arg.Val switch {
                    Runtime.StringConstant s => s.str,
                    Runtime.NumberConstant n => n.value.ToString(),
                    Runtime.BooleanConstant b => b.value.ToString(),
                    null => "null",
                    Runtime.NativeFunction {Name: var n} => $"function {n ?? ""}() {{ [native code] }}",
                    Runtime.Function {Name: var n} => $"function {n ?? ""}() {{ /* function realization /* }}",
                    _ => "Unknown value"
                  };
                  Console.Write(str);
                }
                Console.WriteLine();
                return new(null, true, Runtime.Reference.RefType.rvalue);
              }, "log"), true, Runtime.Reference.RefType.lvalue)
            }), true, Runtime.Reference.RefType.lvalue)
          })
        });
        exception = null;
        return 0;

      }

    }
    public class Engine {
      private readonly List<Runtime.Module>? modules;
      private Runtime.Runner? runner = null;
      private static N CodeToAST(string code) => Parser.Parse(Lexer.Analysis(code));
      public Engine(List<Runtime.Module>? modules = null) {
        this.modules = modules;
      }
      public void Exec(
        string code,
        bool usePreviousContext = false
      ) {
        if (runner == null || !usePreviousContext) runner = new Runtime.Runner(null, modules);
        runner.Exec(CodeToAST(code));
      }
      public Runtime.Reference ExecOneOperation(
        string code,
        bool usePrevioudContext = false
      ) {
        if (runner == null || !usePrevioudContext) runner = new Runtime.Runner(null, modules);
        return runner.ExecOneNode(CodeToAST(code)[0]);
      }
    }
    public static int Exec(string code) => Runner.Run(code, out var _);
#if DEBUG
    internal static N Parse(T tokens) => Parser.Parse(tokens);
    internal static T LexicalAnalysis(string input) => Lexer.Analysis(input);
    internal static void PrintNode(Node node, int indent = 0, bool indentAtStart = true) {
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