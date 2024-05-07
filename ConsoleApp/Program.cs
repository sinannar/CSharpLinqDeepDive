IEnumerable<int> e = GetValues();
using IEnumerator<int> enumerator = e.GetEnumerator();
while (enumerator.MoveNext())
{
    int i = enumerator.Current;
    Console.WriteLine(i);
}

foreach (int i in GetValues())
{
    Console.WriteLine(i);
}

static IEnumerable<int> GetValues()
{
    yield return 1;
    yield return 2;
    yield return 3;
}