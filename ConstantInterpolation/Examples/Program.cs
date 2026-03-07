using ConstantInterpolationGenerator;

Console.WriteLine($"ab{1}cd{1.2}ef{1.1m}".Invariant());
Console.WriteLine($"ab{1}cd{1.2}ef{1.1m}".Local("fr"));
Console.WriteLine($"ab{"constant"}cd".Invariant());
Console.WriteLine($"/{1234.5,8:n1}/{1234.5,-8:n1}//{1234.5,1:n1}/".Invariant());
Console.WriteLine($"/{1234.5,8:n1}/{1234.5,-8:n1}//{1234.5,1:n1}/".Local("de"));
Console.WriteLine($"/{1234.5,8:n1}/{1234.5,-8:n1}//{1234.5,1:n1}/".Local("fr"));

#if false
Console.WriteLine(1);
Console.WriteLine($"ab{x}".Invariant());
Console.WriteLine($"ab{DateTime.Now}".Invariant());
Console.WriteLine("ja");
Console.WriteLine($"ab{1}".Local(y));
Console.WriteLine($"ab{1}".Local("xyz"));
Console.WriteLine("abc".Local("xyz"));
#endif
