using System;
using System.Collections;
using System.Threading;

delegate void ThrWork();

class ThrPool
{
    ArrayList buffer;
    ArrayList lst;

    public ThrPool(int thrNum, int bufSize)
    {
        buffer = new ArrayList(bufSize);
        lst = new ArrayList(thrNum);

        for (int aux = 0; aux <= thrNum; aux++)
        {
            ThreadStart ts = new ThreadStart(DoWork);
            Thread t = new Thread(ts);
            lst.Add(t);
            t.Start();
        }
    }

    private void DoWork()
    {
        while (true)
        {
            Monitor.Enter(this);
            if (buffer.Count > 0)
            {
                ThrWork work = (ThrWork)buffer[0];
                buffer.RemoveAt(0);
                Monitor.Exit(this);
                work();
            }
            else
            {
                Monitor.Exit(this);
            }
        }
    }

    public void AssyncInvoke(ThrWork action)
    {
        if (buffer.Count != 0 && buffer.Capacity == buffer.Count)
        {
            buffer.RemoveAt(0);
        }
        buffer.Add(action);
    }
}


class A
{
    private int _id;

    public A(int id)
    {
        _id = id;
    }

    public void DoWorkA()
    {
        Console.WriteLine("A-{0}", _id);
    }
}


class B
{
    private int _id;

    public B(int id)
    {
        _id = id;
    }

    public void DoWorkB()
    {
        Console.WriteLine("B-{0}", _id);
    }
}


class Test
{
    public static void Main()
    {
        ThrPool tpool = new ThrPool(5, 10);

        for (int i = 0; i < 5; i++)
        {
            A a = new A(i);
            tpool.AssyncInvoke(new ThrWork(a.DoWorkA));
            B b = new B(i);
            tpool.AssyncInvoke(new ThrWork(b.DoWorkB));
        }
        Console.ReadLine();
    }
}