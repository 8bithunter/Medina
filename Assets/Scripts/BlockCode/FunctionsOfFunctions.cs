using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

public static class FunctionsOfFunctions
{
    /*FunctionTree Differentiate(FunctionTree tree)
    {
        FunctionTree placeholderTree = new FunctionTree(tree.function);
        FunctionTree differentiatedTree = new FunctionTree(DifferentiateFunction(tree.function));
        for (int i = 0; i < tree.children.Count; i++)
        {
            placeholderTree.AddChild(Differentiate(tree.children[i]));
            differentiatedTree.AddChild(tree.children[i]);
        }
        return null; //placeholderTree * differentiatedTree
    }

    Function DifferentiateFunction(Function function)
    {
        return null; //big ass table
    }
    */

    /*
     * Function.name = Cos
     * 
     * if (fucntion.name == "cos") return "-sin"
     * 
     * 
    */

    public static FunctionTree Differentiate(FunctionTree tree)
    {
        if (tree == null || tree.function == null)
            throw new ArgumentNullException(nameof(tree));

        string func = tree.function.name;
        var children = tree.children;

        // Base cases
        if (func == "var") // variable x
            return new FunctionTree(new Function("const1")); // d/dx x = 1

        if (func.StartsWith("const")) // constant like const5
            return new FunctionTree(new Function("const0")); // derivative of constant is 0

        // Sum and Difference
        if ((func == "add" || func == "sub") && children.Count == 2)
        {
            var leftPrime = Differentiate(children[0]);
            var rightPrime = Differentiate(children[1]);
            var result = new FunctionTree(new Function(func));
            result.AddChild(leftPrime);
            result.AddChild(rightPrime);
            return result;
        }

        // Product rule: (fg)' = f'g + fg'
        if (func == "mul" && children.Count == 2)
        {
            var f = children[0];
            var g = children[1];
            var fPrime = Differentiate(f);
            var gPrime = Differentiate(g);

            var left = new FunctionTree(new Function("mul"));
            left.AddChild(fPrime);
            left.AddChild(CloneTree(g));

            var right = new FunctionTree(new Function("mul"));
            right.AddChild(CloneTree(f));
            right.AddChild(gPrime);

            var sum = new FunctionTree(new Function("add"));
            sum.AddChild(left);
            sum.AddChild(right);

            return sum;
        }

        // Quotient rule: (f/g)' = (f'g - fg') / g^2
        if (func == "div" && children.Count == 2)
        {
            var f = children[0];
            var g = children[1];
            var fPrime = Differentiate(f);
            var gPrime = Differentiate(g);

            var numeratorLeft = new FunctionTree(new Function("mul"));
            numeratorLeft.AddChild(fPrime);
            numeratorLeft.AddChild(CloneTree(g));

            var numeratorRight = new FunctionTree(new Function("mul"));
            numeratorRight.AddChild(CloneTree(f));
            numeratorRight.AddChild(gPrime);

            var numerator = new FunctionTree(new Function("sub"));
            numerator.AddChild(numeratorLeft);
            numerator.AddChild(numeratorRight);

            var denominator = new FunctionTree(new Function("pow"));
            denominator.AddChild(CloneTree(g));
            denominator.AddChild(new FunctionTree(new Function("const2"))); // g^2

            var result = new FunctionTree(new Function("div"));
            result.AddChild(numerator);
            result.AddChild(denominator);

            return result;
        }

        // Power rule with chain: d/dx (f^g)
        if (func == "pow" && children.Count == 2)
        {
            var baseNode = children[0];
            var expNode = children[1];

            // Handle simple constant exponent: d/dx (f^c) = c * f^(c-1) * f'
            if (expNode.function.name.StartsWith("const"))
            {
                double c = double.Parse(expNode.function.name.Substring(5)); // e.g. const2 -> 2
                var cMinusOne = new FunctionTree(new Function("const" + (c - 1).ToString()));

                var fPrime = Differentiate(baseNode);

                var powPart = new FunctionTree(new Function("pow"));
                powPart.AddChild(CloneTree(baseNode));
                powPart.AddChild(cMinusOne);

                var mul1 = new FunctionTree(new Function("mul"));
                mul1.AddChild(expNode); // c
                mul1.AddChild(powPart);

                var result = new FunctionTree(new Function("mul"));
                result.AddChild(mul1);
                result.AddChild(fPrime);

                return result;
            }
            else
            {
                // General case: d/dx f^g = f^g * (g' * ln(f) + g * f'/f)
                var fPrime = Differentiate(baseNode);
                var gPrime = Differentiate(expNode);

                var lnF = new FunctionTree(new Function("ln"));
                lnF.AddChild(CloneTree(baseNode));

                var gPrime_lnF = new FunctionTree(new Function("mul"));
                gPrime_lnF.AddChild(gPrime);
                gPrime_lnF.AddChild(lnF);

                var fPrime_over_f = new FunctionTree(new Function("div"));
                fPrime_over_f.AddChild(fPrime);
                fPrime_over_f.AddChild(CloneTree(baseNode));

                var g_fPrimeOverF = new FunctionTree(new Function("mul"));
                g_fPrimeOverF.AddChild(CloneTree(expNode));
                g_fPrimeOverF.AddChild(fPrime_over_f);

                var innerSum = new FunctionTree(new Function("add"));
                innerSum.AddChild(gPrime_lnF);
                innerSum.AddChild(g_fPrimeOverF);

                var fPowG = new FunctionTree(new Function("pow"));
                fPowG.AddChild(CloneTree(baseNode));
                fPowG.AddChild(CloneTree(expNode));

                var result = new FunctionTree(new Function("mul"));
                result.AddChild(fPowG);
                result.AddChild(innerSum);

                return result;
            }
        }

        // Unary functions with chain rule

        // sin(f)
        if (func == "sin" && children.Count == 1)
        {
            var f = children[0];
            var fPrime = Differentiate(f);

            var cosF = new FunctionTree(new Function("cos"));
            cosF.AddChild(CloneTree(f));

            var result = new FunctionTree(new Function("mul"));
            result.AddChild(cosF);
            result.AddChild(fPrime);

            return result;
        }

        // cos(f)
        if (func == "cos" && children.Count == 1)
        {
            var f = children[0];
            var fPrime = Differentiate(f);

            var sinF = new FunctionTree(new Function("sin"));
            sinF.AddChild(CloneTree(f));

            // Negative sin(f)
            var negSinF = new FunctionTree(new Function("neg"));
            negSinF.AddChild(sinF);

            var result = new FunctionTree(new Function("mul"));
            result.AddChild(negSinF);
            result.AddChild(fPrime);

            return result;
        }

        // tan(f)
        if (func == "tan" && children.Count == 1)
        {
            var f = children[0];
            var fPrime = Differentiate(f);

            var secF = new FunctionTree(new Function("sec"));
            secF.AddChild(CloneTree(f));

            var secFSquared = new FunctionTree(new Function("pow"));
            secFSquared.AddChild(secF);
            secFSquared.AddChild(new FunctionTree(new Function("const2")));

            var result = new FunctionTree(new Function("mul"));
            result.AddChild(secFSquared);
            result.AddChild(fPrime);

            return result;
        }

        // exp(f)
        if (func == "exp" && children.Count == 1)
        {
            var f = children[0];
            var fPrime = Differentiate(f);

            var expF = new FunctionTree(new Function("exp"));
            expF.AddChild(CloneTree(f));

            var result = new FunctionTree(new Function("mul"));
            result.AddChild(expF);
            result.AddChild(fPrime);

            return result;
        }

        // ln(f)
        if (func == "ln" && children.Count == 1)
        {
            var f = children[0];
            var fPrime = Differentiate(f);

            var result = new FunctionTree(new Function("div"));
            result.AddChild(fPrime);
            result.AddChild(CloneTree(f));

            return result;
        }

        // sec(f) = 1/cos(f)
        if (func == "sec" && children.Count == 1)
        {
            var f = children[0];
            var fPrime = Differentiate(f);

            var secF = new FunctionTree(new Function("sec"));
            secF.AddChild(CloneTree(f));

            var tanF = new FunctionTree(new Function("tan"));
            tanF.AddChild(CloneTree(f));

            var mulInner = new FunctionTree(new Function("mul"));
            mulInner.AddChild(secF);
            mulInner.AddChild(tanF);

            var result = new FunctionTree(new Function("mul"));
            result.AddChild(mulInner);
            result.AddChild(fPrime);

            return result;
        }

        // csc(f) = 1/sin(f)
        if (func == "csc" && children.Count == 1)
        {
            var f = children[0];
            var fPrime = Differentiate(f);

            var cscF = new FunctionTree(new Function("csc"));
            cscF.AddChild(CloneTree(f));

            var cotF = new FunctionTree(new Function("cot"));
            cotF.AddChild(CloneTree(f));

            var mulInner = new FunctionTree(new Function("mul"));
            mulInner.AddChild(cscF);
            mulInner.AddChild(cotF);

            var negMulInner = new FunctionTree(new Function("neg"));
            negMulInner.AddChild(mulInner);

            var result = new FunctionTree(new Function("mul"));
            result.AddChild(negMulInner);
            result.AddChild(fPrime);

            return result;
        }

        // cot(f) = cos(f)/sin(f)
        if (func == "cot" && children.Count == 1)
        {
            var f = children[0];
            var fPrime = Differentiate(f);

            var cotF = new FunctionTree(new Function("cot"));
            cotF.AddChild(CloneTree(f));

            var cscF = new FunctionTree(new Function("csc"));
            cscF.AddChild(CloneTree(f));

            var cscSquared = new FunctionTree(new Function("pow"));
            cscSquared.AddChild(cscF);
            cscSquared.AddChild(new FunctionTree(new Function("const2")));

            var negCSCSquared = new FunctionTree(new Function("neg"));
            negCSCSquared.AddChild(cscSquared);

            var result = new FunctionTree(new Function("mul"));
            result.AddChild(negCSCSquared);
            result.AddChild(fPrime);

            return result;
        }

                // arcsin(f)
        if (func == "arcsin" && children.Count == 1)
        {
            var f = children[0];
            var fPrime = Differentiate(f);

            var fSquared = new FunctionTree(new Function("pow"));
            fSquared.AddChild(CloneTree(f));
            fSquared.AddChild(new FunctionTree(new Function("const2")));

            var one = new FunctionTree(new Function("const1"));

            var insideRoot = new FunctionTree(new Function("sub"));
            insideRoot.AddChild(one);
            insideRoot.AddChild(fSquared);

            var sqrt = new FunctionTree(new Function("pow"));
            sqrt.AddChild(insideRoot);
            sqrt.AddChild(new FunctionTree(new Function("const0.5")));

            var result = new FunctionTree(new Function("div"));
            result.AddChild(fPrime);
            result.AddChild(sqrt);

            return result;
        }

        // arccos(f)
        if (func == "arccos" && children.Count == 1)
        {
            var arcsinTree = Differentiate(new FunctionTree(new Function("arcsin")) { children = children });
            var neg = new FunctionTree(new Function("neg"));
            neg.AddChild(arcsinTree);
            return neg;
        }

        // arctan(f)
        if (func == "arctan" && children.Count == 1)
        {
            var f = children[0];
            var fPrime = Differentiate(f);

            var fSquared = new FunctionTree(new Function("pow"));
            fSquared.AddChild(CloneTree(f));
            fSquared.AddChild(new FunctionTree(new Function("const2")));

            var one = new FunctionTree(new Function("const1"));

            var denominator = new FunctionTree(new Function("add"));
            denominator.AddChild(one);
            denominator.AddChild(fSquared);

            var result = new FunctionTree(new Function("div"));
            result.AddChild(fPrime);
            result.AddChild(denominator);

            return result;
        }

        // arccot(f) = -1 / (1 + f^2)
        if (func == "arccot" && children.Count == 1)
        {
            var arctanTree = Differentiate(new FunctionTree(new Function("arctan")) { children = children });
            var neg = new FunctionTree(new Function("neg"));
            neg.AddChild(arctanTree);
            return neg;
        }

        // arcsec(f)
        if (func == "arcsec" && children.Count == 1)
        {
            var f = children[0];
            var fPrime = Differentiate(f);

            var fSquared = new FunctionTree(new Function("pow"));
            fSquared.AddChild(CloneTree(f));
            fSquared.AddChild(new FunctionTree(new Function("const2")));

            var one = new FunctionTree(new Function("const1"));

            var insideRoot = new FunctionTree(new Function("sub"));
            insideRoot.AddChild(fSquared);
            insideRoot.AddChild(one);

            var sqrt = new FunctionTree(new Function("pow"));
            sqrt.AddChild(insideRoot);
            sqrt.AddChild(new FunctionTree(new Function("const0.5")));

            var absF = new FunctionTree(new Function("abs"));
            absF.AddChild(CloneTree(f));

            var denominator = new FunctionTree(new Function("mul"));
            denominator.AddChild(absF);
            denominator.AddChild(sqrt);

            var result = new FunctionTree(new Function("div"));
            result.AddChild(fPrime);
            result.AddChild(denominator);

            return result;
        }

        // arccsc(f)
        if (func == "arccsc" && children.Count == 1)
        {
            var arcsecTree = Differentiate(new FunctionTree(new Function("arcsec")) { children = children });
            var neg = new FunctionTree(new Function("neg"));
            neg.AddChild(arcsecTree);
            return neg;
        }


        // negation (unary minus)
        if (func == "neg" && children.Count == 1)
        {
            var fPrime = Differentiate(children[0]);
            var negPrime = new FunctionTree(new Function("neg"));
            negPrime.AddChild(fPrime);
            return negPrime;
        }

        throw new NotImplementedException($"Derivative not implemented for function '{func}'");

        if (func == "pow" && tree.children.Count == 2)
        {
            var f = tree.children[0]; // base
            var g = tree.children[1]; // exponent

            // pow(f, g)
            var outer = new FunctionTree(new Function("pow"));
            outer.AddChild(CloneTree(f));
            outer.AddChild(CloneTree(g));

            // g' * ln(f)
            var lnF = new FunctionTree(new Function("ln"));
            lnF.AddChild(CloneTree(f));

            var gPrime = Differentiate(g);
            var term1 = new FunctionTree(new Function("mul"));
            term1.AddChild(gPrime);
            term1.AddChild(lnF);

            // g * f'/f
            var fPrime = Differentiate(f);
            var divF = new FunctionTree(new Function("div"));
            divF.AddChild(fPrime);
            divF.AddChild(CloneTree(f));

            var term2 = new FunctionTree(new Function("mul"));
            term2.AddChild(CloneTree(g));
            term2.AddChild(divF);

            // sum = term1 + term2
            var sum = new FunctionTree(new Function("add"));
            sum.AddChild(term1);
            sum.AddChild(term2);

            // result = outer * sum
            var result = new FunctionTree(new Function("mul"));
            result.AddChild(outer);
            result.AddChild(sum);

            return result;
        }


        return new FunctionTree(new Function("unsupported"));
    }

    public static Function DifferentiateFunction(Function function)
    {
        switch (function.name.ToLower())
        {
            case "sin": return new Function("cos");
            case "cos": return new Function("neg_sin"); // Handle sign separately
            case "exp": return new Function("exp");
            case "ln": return new Function("inv"); // 1/x
            default: return new Function("unsupported");
        }
    }

    public static FunctionTree CloneTree(FunctionTree original)
    {
        FunctionTree clone = new FunctionTree(new Function(original.function.name));
        foreach (var child in original.children)
        {
            clone.AddChild(CloneTree(child));
        }
        return clone;
    }

}
