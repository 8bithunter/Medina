using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

static public class FunctionParser
{
    static private string input;
    static private int pos;
    static private char Current => pos < input.Length ? input[pos] : '\0';

    static public FunctionTree Parse(string expr)
    {
        input = expr.Replace(" ", "").ToLower();
        pos = 0;
        return ParseExpression();
    }

    static private FunctionTree ParseExpression()
    {
        var node = ParseTerm();
        while (Current == '+' || Current == '-')
        {
            char op = Current;
            pos++;
            var right = ParseTerm();
            var func = new Function(op == '+' ? "add" : "sub");
            var parent = new FunctionTree(func);
            parent.AddChild(node);
            parent.AddChild(right);
            node = parent;
        }
        return node;
    }

    static private FunctionTree ParseTerm()
    {
        var node = ParseFactor();
        while (Current == '*' || Current == '/')
        {
            char op = Current;
            pos++;
            var right = ParseFactor();
            var func = new Function(op == '*' ? "mul" : "div");
            var parent = new FunctionTree(func);
            parent.AddChild(node);
            parent.AddChild(right);
            node = parent;
        }
        return node;
    }

    static private FunctionTree ParseFactor()
    {
        var node = ParseUnary();
        while (Current == '^')
        {
            pos++;
            var exponent = ParseUnary();
            var parent = new FunctionTree(new Function("pow"));
            parent.AddChild(node);
            parent.AddChild(exponent);
            node = parent;
        }
        return node;
    }

    static private FunctionTree ParseUnary()
    {
        if (Current == '-')
        {
            pos++;
            var node = ParseUnary();
            var zero = new FunctionTree(new Function("const0"));
            var parent = new FunctionTree(new Function("sub"));
            parent.AddChild(zero);
            parent.AddChild(node);
            return parent;
        }
        return ParseAtom();
    }

    static private FunctionTree ParseAtom()
    {
        if (Current == '(')
        {
            pos++;
            var node = ParseExpression();
            Expect(')');
            return node;
        }

        if (char.IsLetter(Current))
        {
            string name = ParseIdentifier();

            // function call
            if (Current == '(')
            {
                pos++; // skip '('
                var func = new Function(name);
                var tree = new FunctionTree(func);

                if (Current != ')')
                {
                    while (true)
                    {
                        var arg = ParseExpression();
                        tree.AddChild(arg);
                        if (Current == ',') pos++;
                        else break;
                    }
                }

                Expect(')');
                return tree;
            }

            // constants or variables
            if (name == "x") return new FunctionTree(new Function("var"));
            if (name == "e") return new FunctionTree(new Function("conste"));
            if (name == "pi") return new FunctionTree(new Function("constpi"));
            throw new Exception("Unknown symbol: " + name);
        }

        if (char.IsDigit(Current))
        {
            string number = ParseNumber();
            return new FunctionTree(new Function("const" + number));
        }

        throw new Exception("Unexpected character: " + Current);
    }

    static private string ParseIdentifier()
    {
        int start = pos;
        while (char.IsLetter(Current)) pos++;
        return input.Substring(start, pos - start);
    }

    static private string ParseNumber()
    {
        int start = pos;
        while (char.IsDigit(Current) || Current == '.') pos++;
        return input.Substring(start, pos - start);
    }

    static private void Expect(char c)
    {
        if (Current != c)
            throw new Exception($"Expected '{c}', found '{Current}'");
        pos++;
    }
}
