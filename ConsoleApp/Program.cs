using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Collections;
using System.Diagnostics;


//for (int i = 0; i < 10_000; ++i)
//{
//    foreach (int value in SelectManual(source, x => x * 2)) { }
//}

//Console.WriteLine(Enumerable.Select(source, x => x * 2).Sum());

//var c = SelectCompiler(source, x => x * 2);
//Console.WriteLine(c.Sum());
//Console.WriteLine(c.Sum());

//var m = SelectManual(source, x => x * 2);
//Console.WriteLine(m.Sum());
//Console.WriteLine(m.Sum());

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

[MemoryDiagnoser]
[ShortRunJob]
public class Tests
{
    private IEnumerable<int> source = Enumerable.Range(0, 1000).ToArray();

    [Benchmark]
    public int SumCompiler()
    {
        int sum = 0;
        foreach (int i in SelectCompiler(source, i => i * 2)) {
            sum += i;
        }
        return sum;
    }


    [Benchmark]
    public int SumManual()
    {
        int sum = 0;
        foreach (int i in SelectManual(source, i => i * 2))
        {
            sum += i;
        }
        return sum;
    }

    [Benchmark]
    public int SumLinq()
    {
        int sum = 0;
        foreach (int i in Enumerable.Select(source, i => i * 2))
        {
            sum += i;
        }
        return sum;
    }

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

    class SelectManualEnumerable<TSource, TResult> : IEnumerable<TResult>, IEnumerator<TResult>
    {
        private IEnumerable<TSource> _source;
        private Func<TSource, TResult> _selector;

        private int _threadId = Environment.CurrentManagedThreadId;
        private TResult _current = default!;
        private IEnumerator<TSource>? _enumerator;
        private int _state = 0;

        public SelectManualEnumerable(IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            _source = source;
            _selector = selector;
        }

        public IEnumerator<TResult> GetEnumerator()
        {
            //if(Interlocked.CompareExchange(ref _state, 1, 0) == 0)
            if (_threadId == Environment.CurrentManagedThreadId && _state == 0)
            {
                _state = 1;
                return this;
            }
            return new SelectManualEnumerable<TSource, TResult>(_source, _selector) { _state = 1 };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public TResult Current => _current;

        object? IEnumerator.Current => Current;

        public bool MoveNext()
        {
            switch (_state)
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