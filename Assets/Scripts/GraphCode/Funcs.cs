using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using System.Net;
using System;
using UnityEngine.UI;

public class Funcs : MonoBehaviour
{
    public static double dx = 0.00000001;
    public static int integrationResolution = 1000;
    public Transform starti;
    
    static Complex integrationStart;

    public static Matrix matrix;

    public void Start()
    {
        matrix = GetComponent<Matrix>();
    }
    private void Update()
    {
        integrationStart = new Complex(starti.position.x, starti.position.y);
    }

    public static Complex Function(Complex unscaledz)
    {
        Complex z = unscaledz * Scaler.scale;
        Complex symmetryNumber = integrationStart * Scaler.scale;
        Complex output;

            //Change the right side of the following assignment to your desired function using "z" as your variable
            //eg. Complex output = Complex.Sin(z) + Complex.Pow(z, 3) + z;
            output = Complex.Pow(Complex.Pow(z ,z), z);
            //output = Complex.Tan((CreateSymmetry(z, symmetryNumber)));

        return output / Scaler.scale;
    }

    /*public static Complex Derivative(Complex z)
    {
        Complex fz = Function(z);
        Complex fz_plus_h = Function(z + dx);
        Complex fz_minus_h = Function(z - dx);

        double df_dx = (fz_plus_h.Real - fz_minus_h.Real) / (2 * dx);
        double df_dy = (fz_plus_h.Imaginary - fz_minus_h.Imaginary) / (2 * dx);

        return new Complex(df_dx, df_dy) / Scaler.scale;
    }*/

    public static Complex Derivative(Complex z)
    {
        Complex fz_plus_h = Function(z + new Complex(dx, 0));
        Complex fz_minus_h = Function(z - new Complex(dx, 0));

        return ((fz_plus_h - fz_minus_h) / (2 * dx)) / Scaler.scale;
    }




    public static Complex RiemannSum(Complex endPoint)
    {
        Complex antiResult = Complex.Zero;

        Complex deltaZ = (endPoint - integrationStart) / integrationResolution;

        for (int i = 0; i < integrationResolution; i++)
        {
            Complex z = integrationStart + deltaZ * i;
            Complex f_z = Function(z);
            antiResult += f_z * deltaZ;
        }
        return (antiResult * Scaler.scale);
    }

    public static Complex SimpsonsRule(Complex end)
    {
        Complex h = (end - integrationStart) / integrationResolution;
        Complex result = Function(integrationStart) + Function(end);

        for (int i = 1; i < integrationResolution; i += 2)
        {
            result += 4 * Function(integrationStart + i * h);
        }

        for (int i = 2; i < integrationResolution - 1; i += 2)
        {
            result += 2 * Function(integrationStart + i * h);
        }

        return (result * h / 3.0) * Scaler.scale;
    }

    public static Complex Mandelbrot(Complex z)
    {
        Complex output = Complex.Zero;
        for (int i = 0; i < 100; i++)
        {
            output = Complex.Pow(output, 2) + z;
        }
        return output;
    }

    public static Complex Zeta(Complex z)
    {
        if (z.Real >= 0.5)
        {
            Complex output = Complex.Zero;
            for (int i = 1; i < 1000; i++)
            {
                output = output + 1 / Complex.Pow(i, z);
            }
            return output;
        }
        else
        {
            Complex output = Complex.Pow(2, z) * Complex.Pow(Math.PI, z - 1);
            return (output * Complex.Sin((Math.PI * z) / 2) * Gamma(1 - z) * Zeta(1 - z));
        }
    }

    static int g = 7;
    static double[] p = {0.99999999999980993, 676.5203681218851, -1259.1392167224028,
         771.32342877765313, -176.61502916214059, 12.507343278686905,
         -0.13857109526572012, 9.9843695780195716e-6, 1.5056327351493116e-7};

    public static Complex Gamma(Complex z)
    {
        if (z.Real < 0.5)
        {
            return Math.PI / (Complex.Sin(Math.PI * z) * Gamma(1 - z));
        }
        else
        {
            z -= 1;
            Complex x = p[0];
            for (var i = 1; i < g + 2; i++)
            {
                x += p[i] / (z + i);
            }
            Complex t = z + g + 0.5;
            return Complex.Sqrt(2 * Math.PI) * (Complex.Pow(t, z + 0.5)) * Complex.Exp(-t) * x;
        }
    }

    public static Complex LambertW(Complex z, int maxIterations = 100, double tolerance = 1e-10)
    {
        if (z == Complex.Zero) return Complex.Zero;

        Complex w = z.Magnitude < 1 ? z : Complex.Log(z);

        for (int i = 0; i < maxIterations; i++)
        {
            Complex ew = Complex.Exp(w);
            Complex wew = w * ew;
            Complex deltaW = (wew - z) / (ew * (w + 1) - (w + 2) * (wew - z) / (2 * w + 2));
            w -= deltaW;

            if (Complex.Abs(deltaW) < tolerance)
                break;
        }

        return w;
    }

    public static Complex CreateSymmetry(Complex z, Complex symmetryNumber)
    {
        return (Complex.Pow(z, symmetryNumber)) / Complex.Abs(Complex.Pow(z, symmetryNumber - 1));
    }
}

