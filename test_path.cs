using System;
using System.IO;

class Program {
    static void Main() {
        var chars = Path.GetInvalidPathChars();
        Console.WriteLine($"Count: {chars.Length}");
        foreach(var c in chars) Console.Write($"{(int)c} ");
        Console.WriteLine();
    }
}
