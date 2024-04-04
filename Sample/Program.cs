using Hazelnut.Teok;

var diff = Different.Determine<char>("abcdefg".ToCharArray(), "adedgdfg".ToCharArray());

foreach (var (d, ch) in diff)
{
    switch (d)
    {
        case DifferentKind.None:
            Console.ForegroundColor = ConsoleColor.Black;
            break;
        
        case DifferentKind.Added:
            Console.ForegroundColor = ConsoleColor.Blue;
            break;
        
        case DifferentKind.Removed:
            Console.ForegroundColor = ConsoleColor.Red;
            break;
    }
    
    Console.Write(ch);
}

Console.WriteLine();