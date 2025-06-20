using System;
using System.Collections.Generic;

public static class FunctionTreeStringifier
{
    public static string ToReadableString(FunctionTree tree)
    {
        if (tree == null || tree.function == null)
            return "";

        return Visit(StringCompressor(tree));
    }

    private static string Visit(FunctionTree node)
    {
        string name = node.function.name;
        var children = node.children;

        if (name.StartsWith("const"))
        {
            string val = name.Substring(5);
            if (val == "e") return "e";
            if (val == "pi") return "π";
            return val;
        }

        if (name.StartsWith("neg_") && children.Count == 0)
        {
            // e.g. neg_e means -e
            string symbol = name.Substring(4);
            return "-" + symbol;
        }

        if (name == "var")
        {
            return "x";
        }

        if (name == "neg" && children.Count == 1)
        {
            string inner = Visit(children[0]);
            if (IsOperatorNode(children[0]))
                return "-(" + inner + ")";
            else
                return "-" + inner;
        }

        if (IsBinaryOperator(name) && children.Count == 2)
        {
            // Special case for pow with negative exponent: print as division
            if (name == "pow" && children[1].function.name == "neg" && children[1].children.Count == 1)
            {
                string baseStr = Visit(children[0]);
                string posExp = Visit(children[1].children[0]);
                return "1 / (" + baseStr + " ^ " + posExp + ")";
            }

            string left = Visit(children[0]);
            string right = Visit(children[1]);
            string op = OperatorSymbol(name);

            if (IsOperatorNode(children[0]) && Precedence(children[0].function.name) < Precedence(name))
                left = "(" + left + ")";
            if (IsOperatorNode(children[1]) && Precedence(children[1].function.name) <= Precedence(name))
                right = "(" + right + ")";

            return left + " " + op + " " + right;
        }

        if (IsUnaryFunction(name) && children.Count == 1)
        {
            return name + "(" + Visit(children[0]) + ")";
        }

        string args = string.Join(", ", children.ConvertAll(Visit));
        return name + "(" + args + ")";
    }

    private static bool IsUnaryFunction(string name)
    {
        switch (name)
        {
            case "sin":
            case "cos":
            case "tan":
            case "exp":
            case "ln":
            case "sec":
            case "csc":
            case "cot":
                return true;
            default:
                return false;
        }
    }

    private static bool IsBinaryOperator(string name)
    {
        switch (name)
        {
            case "add":
            case "sub":
            case "mul":
            case "div":
            case "pow":
                return true;
            default:
                return false;
        }
    }

    private static string OperatorSymbol(string name)
    {
        switch (name)
        {
            case "add": return "+";
            case "sub": return "-";
            case "mul": return "*";
            case "div": return "/";
            case "pow": return "^";
            default: return name;
        }
    }

    private static bool IsOperatorNode(FunctionTree node)
    {
        return IsBinaryOperator(node.function.name) || node.function.name == "neg";
    }

    private static int Precedence(string op)
    {
        switch (op)
        {
            case "add":
            case "sub":
                return 1;
            case "mul":
            case "div":
                return 2;
            case "pow":
                return 3;
            case "neg":
                return 4;
            default:
                return 5;
        }
    }

    public static FunctionTree StringCompressor(FunctionTree tree)
    {
        if (tree == null) return null;

        // Recursively simplify children first
        for (int i = 0; i < tree.children.Count; i++)
        {
            tree.children[i] = StringCompressor(tree.children[i]);
        }

        string func = tree.function.name;
        var children = tree.children;

        // Handle ln(e) = 1 simplification
        if (func == "ln" && children.Count == 1)
        {
            if (IsSpecialConstant(children[0], out string specialConst) && specialConst == "e")
            {
                return new FunctionTree(new Function("const1"));
            }
        }

        // Simplify subtraction like 0 - x to neg(x)
        if (func == "sub" && children.Count == 2)
        {
            if (IsConst(children[0], out double val) && val == 0)
            {
                return new FunctionTree(new Function("neg")) { children = { children[1] } };
            }
        }

        // Simplify (a^b)^c => a^(b*c)
        if (func == "pow" && children.Count == 2)
        {
            if (children[0].function.name == "pow" && children[0].children.Count == 2)
            {
                var a = children[0].children[0];
                var b = children[0].children[1];
                var c = children[1];

                var newExponent = new FunctionTree(new Function("mul"));
                newExponent.AddChild(b);
                newExponent.AddChild(c);

                return new FunctionTree(new Function("pow")) { children = { a, StringCompressor(newExponent) } };
            }
        }

        // Helper: check if node is constant and get numeric value
        bool IsConst(FunctionTree node, out double val)
        {
            val = 0;
            if (node.function.name.StartsWith("const"))
            {
                string numStr = node.function.name.Substring(5);
                if (numStr == "e" || numStr == "pi")
                    return false; // special constants no numeric val
                return double.TryParse(numStr, out val);
            }
            return false;
        }

        // Helper: check if node is special constant like "conste" or "constpi"
        bool IsSpecialConstant(FunctionTree node, out string symbol)
        {
            symbol = null;
            if (node.function.name == "conste")
            {
                symbol = "e";
                return true;
            }
            else if (node.function.name == "constpi")
            {
                symbol = "π";
                return true;
            }
            return false;
        }

        // Simplify binary operators
        if ((func == "mul" || func == "add" || func == "sub" || func == "div" || func == "pow") && children.Count == 2)
        {
            double leftVal, rightVal;
            bool leftIsConst = IsConst(children[0], out leftVal);
            bool rightIsConst = IsConst(children[1], out rightVal);

            // Multiplication by 1
            if (func == "mul")
            {
                if (leftIsConst && leftVal == 1) return children[1];
                if (rightIsConst && rightVal == 1) return children[0];
                // Multiplying by 0 → 0
                if ((leftIsConst && leftVal == 0) || (rightIsConst && rightVal == 0))
                    return new FunctionTree(new Function("const0"));
            }

            // Addition by 0
            if (func == "add")
            {
                if (leftIsConst && leftVal == 0) return children[1];
                if (rightIsConst && rightVal == 0) return children[0];
            }

            // Subtraction by 0
            if (func == "sub")
            {
                if (rightIsConst && rightVal == 0) return children[0];
            }

            // Division by 1
            if (func == "div")
            {
                if (rightIsConst && rightVal == 1) return children[0];

                // 0 / (anything but 0) = 0
                if (leftIsConst && leftVal == 0)
                {
                    if (!(rightIsConst && rightVal == 0))
                    {
                        return new FunctionTree(new Function("const0"));
                    }
                }
            }

            // Power simplifications
            if (func == "pow")
            {
                if (rightIsConst && rightVal == 1) return children[0];
                if (rightIsConst && rightVal == 0) return new FunctionTree(new Function("const1"));
                if (leftIsConst && leftVal == 1) return new FunctionTree(new Function("const1"));
            }

            // Evaluate if both constants
            if (leftIsConst && rightIsConst)
            {
                double result;
                switch (func)
                {
                    case "add": result = leftVal + rightVal; break;
                    case "sub": result = leftVal - rightVal; break;
                    case "mul": result = leftVal * rightVal; break;
                    case "div":
                        if (rightVal == 0) throw new DivideByZeroException();
                        result = leftVal / rightVal;
                        break;
                    case "pow": result = Math.Pow(leftVal, rightVal); break;
                    default: return tree;
                }
                return new FunctionTree(new Function("const" + result.ToString()));
            }
        }

        // Simplify negation of constants: neg(const5) -> const-5 or neg(conste) -> -e
        if (func == "neg" && children.Count == 1)
        {
            if (IsConst(children[0], out double val))
            {
                return new FunctionTree(new Function("const" + (-val).ToString()));
            }
            else if (IsSpecialConstant(children[0], out string symbol))
            {
                return new FunctionTree(new Function("neg_" + symbol));
            }
        }

        // No simplification possible
        return tree;
    }
}
