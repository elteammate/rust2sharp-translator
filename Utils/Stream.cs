using System.Collections.Immutable;
using NUnit.Framework;

namespace Rust2SharpTranslator.Utils;

public class EndOfStreamException : Exception
{
    public EndOfStreamException() : base("End of stream") { }
}

public class Stream<T>
{
    public Stream(IEnumerable<T> data)
    {
        Data = data.ToImmutableList();
    }
    
    private Stream(ImmutableList<T> data, int index)
    {
        Data = data;
        Index = index;
    }

    private ImmutableList<T> Data { get; }
    private int Index { get; set; }
    
    private T? Get(int index)
    {
        if (index >= Data.Count)
            return default;
        return Data[index];
    }
    
    public T? Next() => Get(Index++);
    
    public bool HasNext() => Index < Data.Count;
    
    public T? Peek(int offset = 0) => Get(Index + offset);
    
    public Stream<T> Fork() => new(Data, Index);
    
    public bool IfMatchConsume(T value)
    {
        if (!Peek()!.Equals(value)) return false;
        Next();
        return true;
    }
    
    public Stream<T> Skip(int count = 1) {
        Index += count;
        return this;
    }
    
    public IEnumerable<T> Take(int count)
    {
        for (var i = 0; i < count && HasNext(); i++)
            yield return Next()!;
    }

    public IEnumerable<T> TakeWhile(Predicate<T> predicate)
    {
        while (true)
        {
            if (!HasNext() || !predicate(Peek()!))
                yield break;
            yield return Next()!;
        }
    }
    
    public IEnumerable<T> TakeUntil(T[] values)
    {
        while (true)
        {
            if (!HasNext() || Fork().Take(values.Length).SequenceEqual(values))
                yield break;
            yield return Next()!;
        }
    }

    public void SkipWhile(Predicate<T> predicate)
    {
        while (true)
        {
            if (!HasNext() || !predicate(Peek()!))
                return;
            Next();
        }
    }
}


internal class __StreamTests__
{
    [Test]
    public void Stream_TestNext_ReturnsCorrectValues()
    {
        var stream = new Stream<int>(new[] {1, 2, 3, 4, 5});
        Assert.AreEqual(1, stream.Next());
        Assert.AreEqual(2, stream.Next());
        Assert.AreEqual(3, stream.Next());
        Assert.AreEqual(4, stream.Next());
        Assert.AreEqual(5, stream.Next());
        Assert.IsFalse(stream.HasNext());
    }
    
    [Test]
    public void Stream_TestLookAhead_ReturnsCorrectValues()
    {
        var stream = new Stream<int>(new[] {1, 2, 3, 4, 5});
        Assert.AreEqual(1, stream.Peek());
        Assert.AreEqual(1, stream.Next());
        Assert.AreEqual(2, stream.Peek());
        Assert.AreEqual(2, stream.Peek());
        Assert.AreEqual(2, stream.Next());
        Assert.AreEqual(3, stream.Peek());
        Assert.AreEqual(3, stream.Next());
        Assert.AreEqual(4, stream.Peek());
        Assert.AreEqual(4, stream.Next());
        Assert.AreEqual(5, stream.Peek());
        Assert.AreEqual(5, stream.Peek());
        Assert.AreEqual(5, stream.Next());
        Assert.IsFalse(stream.HasNext());
    }
    
    [Test]
    public void Stream_TestFork_ReturnsCorrectValues()
    {
        var stream = new Stream<int>(new[] {1, 2, 3, 4, 5});
        var fork = stream.Fork();
        Assert.AreEqual(1, stream.Next());
        Assert.AreEqual(1, fork.Next());
        Assert.AreEqual(2, stream.Next());
        Assert.AreEqual(2, fork.Next());
        Assert.AreEqual(3, stream.Next());
        Assert.AreEqual(3, fork.Next());
        Assert.AreEqual(4, stream.Next());
        Assert.AreEqual(4, fork.Next());
        Assert.AreEqual(5, stream.Next());
        Assert.AreEqual(5, fork.Next());
        Assert.IsFalse(stream.HasNext());
        Assert.IsFalse(fork.HasNext());
    }
    
    [Test]
    public void Stream_TestTakeWhile_ReturnsCorrectValues()
    {
        var stream = new Stream<int>(new[] {1, 2, 3, 4, 5});
        var result = stream.TakeWhile(x => x < 4).ToList();
        Assert.AreEqual(new[] {1, 2, 3}, result);
        Assert.AreEqual(4, stream.Next());
        Assert.AreEqual(5, stream.Next());
        Assert.IsFalse(stream.HasNext());
    }
    
    [Test]
    public void Stream_TestTakeWhile_ThrowsWhenStops()
    {
        var stream = new Stream<int>(new[] {1, 2, 3, 4, 5});
        // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
        Assert.Throws<EndOfStreamException>(() => stream.TakeWhile(x => x < 10).ToArray());
    }
}
