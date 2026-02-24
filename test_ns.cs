namespace A
{
    public class C { public const int X = 1; }
}

namespace A.B
{
    public class D
    {
        public int GetX() { return C.X; }
    }
}

class Program { static void Main() {} }
