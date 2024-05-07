using System.Collections;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

IEnumerable<int> source = Enumerable.Range(0, 1000).ToArray();
Console.WriteLine(Enumerable.Select(source, x => x * 2).Sum());
Console.WriteLine(SelectCompiler(source, x => x * 2).Sum());
Console.WriteLine(SelectManual(source, x => x * 2).Sum());

static IEnumerable<TResult> SelectCompiler<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, TResult> selector)
{
    ArgumentNullException.ThrowIfNull(source);
    ArgumentNullException.ThrowIfNull(selector);

    return Impl(source, selector);

    static IEnumerable<TResult> Impl<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, TResult> selector)
    {

        foreach (var item in source)
        {
            yield return selector(item);
        }
    }
}

static IEnumerable<TResult> SelectManual<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, TResult> selector)
{
    ArgumentNullException.ThrowIfNull(source);
    ArgumentNullException.ThrowIfNull(selector);

    return new SelectManualEnumerable<TSource, TResult>(source, selector);
}

class SelectManualEnumerable<TSource, TResult> : IEnumerable<TResult>
{
    private IEnumerable<TSource> _source;
    private Func<TSource, TResult> _selector;

    public SelectManualEnumerable(IEnumerable<TSource> source, Func<TSource, TResult> selector)
    {
        _source = source;
        _selector = selector;
    }

    public IEnumerator<TResult> GetEnumerator()
    {
        return new Enumerator(_source, _selector);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed class Enumerator : IEnumerator<TResult>
    {
        private IEnumerable<TSource> _source;
        private Func<TSource, TResult> _selector;

        private TResult _current = default!;
        private IEnumerator<TSource>? _enumerator;
        private int _state = 1;

        public Enumerator(IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            _source = source;
            _selector = selector;
        }

        public TResult Current => _current;

        object? IEnumerator.Current => Current;

        public bool MoveNext()
        {
            switch(_state)
            {
                case 1:
                    _enumerator = _source.GetEnumerator();
                    _state = 2;
                    goto case 2;
                case 2:
                    Debug.Assert(_enumerator is not null);
                    try
                    {
                        if (_enumerator.MoveNext())
                        {
                            _current = _selector(_enumerator.Current);
                            return true;
                            // yield return
                        }
                    }
                    catch
                    {
                        Dispose();
                        throw;
                    }
                    break;
            }

            Dispose();
            return false;
        }

        public void Dispose()
        {
            _state = -1;
            _enumerator?.Dispose();
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }
    }
}