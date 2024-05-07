
foreach (int i in GetValues())
{
    Console.WriteLine(i);
}

static IEnumerable<int> GetValues()
{
    return new List<int>() { 1, 2, 3 };
}