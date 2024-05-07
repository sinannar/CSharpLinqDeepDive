
//Enumerable.Select<int, int>(null, i => i * 2);


Console.WriteLine(0);
IEnumerable<int> e = Select<int, int>(null, i => i * 2);
Console.WriteLine(1);
IEnumerator<int> enumarator = e.GetEnumerator();
Console.WriteLine(2);
enumarator.MoveNext();
Console.WriteLine(3);

static IEnumerable<TResult> Select<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, TResult> selector)
{
    ArgumentNullException.ThrowIfNull(source);
    ArgumentNullException.ThrowIfNull(selector);

    foreach (var item in source)
    {
        yield return selector(item);
    }
}
