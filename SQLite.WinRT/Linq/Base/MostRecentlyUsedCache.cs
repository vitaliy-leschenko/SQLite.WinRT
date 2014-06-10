using System.Collections.Generic;

namespace System.Threading
{
    [Flags]
    internal enum LockState
    {
        None = 0,
        Read = 1,
        Write = 2,
        Upgradable = 4,
        UpgradedRead = Upgradable | Read,
        UpgradedWrite = Upgradable | Write
    }

    internal class ThreadLockState
    {
        public LockState LockState;
        public int ReaderRecursiveCount;
        public int UpgradeableRecursiveCount;
        public int WriterRecursiveCount;
    }

    public enum LockRecursionPolicy
    {
        NoRecursion,
        SupportsRecursion
    }

    public class ReaderWriterLockSlim : IDisposable
    {
        /* Position of each bit isn't really important 
		 * but their relative order is
		 */
        private const int RwReadBit = 3;

        private const int RwWait = 1;
        private const int RwWaitUpgrade = 2;
        private const int RwWrite = 4;
        private const int RwRead = 8;
        private static int idPool = int.MinValue;
        [ThreadStatic] private static IDictionary<int, ThreadLockState> currentThreadState;
        private readonly int id = Interlocked.Increment(ref idPool);
        private readonly ManualResetEventSlim readerDoneEvent = new ManualResetEventSlim(true);

        private readonly LockRecursionPolicy recursionPolicy;

        private readonly ManualResetEventSlim upgradableEvent = new ManualResetEventSlim(true);
        private readonly AtomicBoolean upgradableTaken = new AtomicBoolean();
        private readonly ManualResetEventSlim writerDoneEvent = new ManualResetEventSlim(true);
        private bool disposed;

        private int numReadWaiters, numUpgradeWaiters, numWriteWaiters;
        private int rwlock;

        public ReaderWriterLockSlim() : this(LockRecursionPolicy.NoRecursion)
        {
        }

        public ReaderWriterLockSlim(LockRecursionPolicy recursionPolicy)
        {
            this.recursionPolicy = recursionPolicy;
        }

        public bool IsReadLockHeld
        {
            get { return rwlock >= RwRead; }
        }

        public bool IsWriteLockHeld
        {
            get { return (rwlock & RwWrite) > 0; }
        }

        public bool IsUpgradeableReadLockHeld
        {
            get { return upgradableTaken.Value; }
        }

        public int CurrentReadCount
        {
            get { return (rwlock >> RwReadBit) - (IsUpgradeableReadLockHeld ? 1 : 0); }
        }

        public int RecursiveReadCount
        {
            get { return CurrentThreadState.ReaderRecursiveCount; }
        }

        public int RecursiveUpgradeCount
        {
            get { return CurrentThreadState.UpgradeableRecursiveCount; }
        }

        public int RecursiveWriteCount
        {
            get { return CurrentThreadState.WriterRecursiveCount; }
        }

        public int WaitingReadCount
        {
            get { return numReadWaiters; }
        }

        public int WaitingUpgradeCount
        {
            get { return numUpgradeWaiters; }
        }

        public int WaitingWriteCount
        {
            get { return numWriteWaiters; }
        }

        public LockRecursionPolicy RecursionPolicy
        {
            get { return recursionPolicy; }
        }

        private LockState CurrentLockState
        {
            get { return CurrentThreadState.LockState; }
            set { CurrentThreadState.LockState = value; }
        }

        private ThreadLockState CurrentThreadState
        {
            get
            {
                if (currentThreadState == null)
                    currentThreadState = new Dictionary<int, ThreadLockState>();

                ThreadLockState state;
                if (!currentThreadState.TryGetValue(id, out state))
                    currentThreadState[id] = state = new ThreadLockState();

                return state;
            }
        }

        public void Dispose()
        {
            disposed = true;
        }

        public void EnterReadLock()
        {
            TryEnterReadLock(-1);
        }

        public bool TryEnterReadLock(int millisecondsTimeout)
        {
            ThreadLockState ctstate = CurrentThreadState;

            if (CheckState(millisecondsTimeout, LockState.Read))
            {
                ctstate.ReaderRecursiveCount++;
                return true;
            }

            // This is downgrading from upgradable, no need for check since
            // we already have a sort-of read lock that's going to disappear
            // after user calls ExitUpgradeableReadLock.
            // Same idea when recursion is allowed and a write thread wants to
            // go for a Read too.
            if (CurrentLockState.HasFlag(LockState.Upgradable)
                || recursionPolicy == LockRecursionPolicy.SupportsRecursion)
            {
                Interlocked.Add(ref rwlock, RwRead);
                ctstate.LockState ^= LockState.Read;
                ctstate.ReaderRecursiveCount++;

                return true;
            }

            Stopwatch sw = Stopwatch.StartNew();
            Interlocked.Increment(ref numReadWaiters);

            while (millisecondsTimeout == -1 || sw.ElapsedMilliseconds < millisecondsTimeout)
            {
                if ((rwlock & 0x7) > 0)
                {
                    writerDoneEvent.Wait(ComputeTimeout(millisecondsTimeout, sw));
                    continue;
                }

                if ((Interlocked.Add(ref rwlock, RwRead) & 0x7) == 0)
                {
                    ctstate.LockState ^= LockState.Read;
                    ctstate.ReaderRecursiveCount++;
                    Interlocked.Decrement(ref numReadWaiters);
                    if (readerDoneEvent.IsSet)
                        readerDoneEvent.Reset();
                    return true;
                }

                Interlocked.Add(ref rwlock, -RwRead);

                writerDoneEvent.Wait(ComputeTimeout(millisecondsTimeout, sw));
            }

            Interlocked.Decrement(ref numReadWaiters);
            return false;
        }

        public bool TryEnterReadLock(TimeSpan timeout)
        {
            return TryEnterReadLock(CheckTimeout(timeout));
        }

        public void ExitReadLock()
        {
            ThreadLockState ctstate = CurrentThreadState;

            if (!ctstate.LockState.HasFlag(LockState.Read))
                throw new Exception("The current thread has not entered the lock in read mode");

            ctstate.LockState ^= LockState.Read;
            ctstate.ReaderRecursiveCount--;
            if (Interlocked.Add(ref rwlock, -RwRead) >> RwReadBit == 0)
                readerDoneEvent.Set();
        }

        public void EnterWriteLock()
        {
            TryEnterWriteLock(-1);
        }

        public bool TryEnterWriteLock(int millisecondsTimeout)
        {
            ThreadLockState ctstate = CurrentThreadState;

            if (CheckState(millisecondsTimeout, LockState.Write))
            {
                ctstate.WriterRecursiveCount++;
                return true;
            }

            Stopwatch sw = Stopwatch.StartNew();
            Interlocked.Increment(ref numWriteWaiters);
            bool isUpgradable = ctstate.LockState.HasFlag(LockState.Upgradable);

            // If the code goes there that means we had a read lock beforehand
            if (isUpgradable && rwlock >= RwRead)
                Interlocked.Add(ref rwlock, -RwRead);

            int stateCheck = isUpgradable ? RwWaitUpgrade : RwWait;
            int appendValue = RwWait | (isUpgradable ? RwWaitUpgrade : 0);

            while (millisecondsTimeout < 0 || sw.ElapsedMilliseconds < millisecondsTimeout)
            {
                int state = rwlock;

                if (state <= stateCheck)
                {
                    if (Interlocked.CompareExchange(ref rwlock, RwWrite, state) == state)
                    {
                        ctstate.LockState ^= LockState.Write;
                        ctstate.WriterRecursiveCount++;
                        Interlocked.Decrement(ref numWriteWaiters);
                        if (writerDoneEvent.IsSet)
                            writerDoneEvent.Reset();
                        return true;
                    }
                    state = rwlock;
                }

                while ((state & RwWait) == 0 &&
                       Interlocked.CompareExchange(ref rwlock, state | appendValue, state) == state)
                    state = rwlock;

                while (rwlock > stateCheck && (millisecondsTimeout < 0 || sw.ElapsedMilliseconds < millisecondsTimeout))
                    readerDoneEvent.Wait(ComputeTimeout(millisecondsTimeout, sw));
            }

            Interlocked.Decrement(ref numWriteWaiters);
            return false;
        }

        public bool TryEnterWriteLock(TimeSpan timeout)
        {
            return TryEnterWriteLock(CheckTimeout(timeout));
        }

        public void ExitWriteLock()
        {
            ThreadLockState ctstate = CurrentThreadState;

            if (!ctstate.LockState.HasFlag(LockState.Write))
                throw new Exception("The current thread has not entered the lock in write mode");

            ctstate.LockState ^= LockState.Write;
            ctstate.WriterRecursiveCount--;
            Interlocked.Add(ref rwlock, -RwWrite);
            writerDoneEvent.Set();
        }

        public void EnterUpgradeableReadLock()
        {
            TryEnterUpgradeableReadLock(-1);
        }

        //
        // Taking the Upgradable read lock is like taking a read lock
        // but we limit it to a single upgradable at a time.
        //
        public bool TryEnterUpgradeableReadLock(int millisecondsTimeout)
        {
            ThreadLockState ctstate = CurrentThreadState;

            if (CheckState(millisecondsTimeout, LockState.Upgradable))
            {
                ctstate.UpgradeableRecursiveCount++;
                return true;
            }

            if (ctstate.LockState.HasFlag(LockState.Read))
                throw new Exception("The current thread has already entered read mode");

            Stopwatch sw = Stopwatch.StartNew();
            Interlocked.Increment(ref numUpgradeWaiters);

            while (!upgradableEvent.IsSet || !upgradableTaken.TryRelaxedSet())
            {
                if (millisecondsTimeout != -1 && sw.ElapsedMilliseconds > millisecondsTimeout)
                {
                    Interlocked.Decrement(ref numUpgradeWaiters);
                    return false;
                }

                upgradableEvent.Wait(ComputeTimeout(millisecondsTimeout, sw));
            }

            upgradableEvent.Reset();

            if (TryEnterReadLock(ComputeTimeout(millisecondsTimeout, sw)))
            {
                ctstate.LockState = LockState.Upgradable;
                Interlocked.Decrement(ref numUpgradeWaiters);
                ctstate.ReaderRecursiveCount--;
                ctstate.UpgradeableRecursiveCount++;
                return true;
            }

            upgradableTaken.Value = false;
            upgradableEvent.Set();

            Interlocked.Decrement(ref numUpgradeWaiters);

            return false;
        }

        public bool TryEnterUpgradeableReadLock(TimeSpan timeout)
        {
            return TryEnterUpgradeableReadLock(CheckTimeout(timeout));
        }

        public void ExitUpgradeableReadLock()
        {
            ThreadLockState ctstate = CurrentThreadState;

            if (!ctstate.LockState.HasFlag(LockState.Upgradable | LockState.Read))
                throw new Exception("The current thread has not entered the lock in upgradable mode");

            upgradableTaken.Value = false;
            upgradableEvent.Set();

            ctstate.LockState ^= LockState.Upgradable;
            ctstate.UpgradeableRecursiveCount--;
            if (Interlocked.Add(ref rwlock, -RwRead) >> RwReadBit == 0)
                readerDoneEvent.Set();
        }

        private bool CheckState(int millisecondsTimeout, LockState validState)
        {
            if (disposed)
                throw new ObjectDisposedException("ReaderWriterLockSlim");

            if (millisecondsTimeout < Timeout.Infinite)
                throw new ArgumentOutOfRangeException("millisecondsTimeout");

            // Detect and prevent recursion
            LockState ctstate = CurrentLockState;

            if (recursionPolicy == LockRecursionPolicy.NoRecursion)
                if ((ctstate != LockState.None && ctstate != LockState.Upgradable)
                    || (ctstate == LockState.Upgradable && validState == LockState.Upgradable))
                    throw new Exception("The current thread has already a lock and recursion isn't supported");

            // If we already had right lock state, just return
            if (ctstate.HasFlag(validState))
                return true;

            CheckRecursionAuthorization(ctstate, validState);

            return false;
        }

        private static void CheckRecursionAuthorization(LockState ctstate, LockState desiredState)
        {
            // In read mode you can just enter Read recursively
            if (ctstate == LockState.Read)
                throw new Exception();
        }

        private static int CheckTimeout(TimeSpan timeout)
        {
            try
            {
                return checked ((int) timeout.TotalMilliseconds);
            }
            catch (OverflowException)
            {
                throw new ArgumentOutOfRangeException("timeout");
            }
        }

        private static int ComputeTimeout(int millisecondsTimeout, Stopwatch sw)
        {
            return millisecondsTimeout == -1 ? -1 : (int) Math.Max(sw.ElapsedMilliseconds - millisecondsTimeout, 1);
        }
    }

    internal class AtomicBoolean
    {
        private const int UnSet = 0;
        private const int Set = 1;
        private int flag;

        public bool Value
        {
            get { return flag == Set; }
            set { Exchange(value); }
        }

        public bool CompareAndExchange(bool expected, bool newVal)
        {
            int newTemp = newVal ? Set : UnSet;
            int expectedTemp = expected ? Set : UnSet;

            return Interlocked.CompareExchange(ref flag, newTemp, expectedTemp) == expectedTemp;
        }

        public static AtomicBoolean FromValue(bool value)
        {
            var temp = new AtomicBoolean();
            temp.Value = value;

            return temp;
        }

        public bool TrySet()
        {
            return !Exchange(true);
        }

        public bool TryRelaxedSet()
        {
            return flag == UnSet && !Exchange(true);
        }

        public bool Exchange(bool newVal)
        {
            int newTemp = newVal ? Set : UnSet;
            return Interlocked.Exchange(ref flag, newTemp) == Set;
        }

        public bool Equals(AtomicBoolean rhs)
        {
            return flag == rhs.flag;
        }

        public override bool Equals(object rhs)
        {
            return rhs is AtomicBoolean ? Equals((AtomicBoolean) rhs) : false;
        }

        public override int GetHashCode()
        {
            return flag.GetHashCode();
        }

        public static explicit operator bool(AtomicBoolean rhs)
        {
            return rhs.Value;
        }

        public static implicit operator AtomicBoolean(bool rhs)
        {
            return FromValue(rhs);
        }
    }

    public class Stopwatch
    {
        public static readonly long Frequency = 10000000;

        public static readonly bool IsHighResolution = true;

        private long elapsed;
        private bool is_running;
        private long started;

        public TimeSpan Elapsed
        {
            get
            {
                if (IsHighResolution)
                {
                    // convert our ticks to TimeSpace ticks, 100 nano second units
                    // using two divisions helps avoid overflow
                    return TimeSpan.FromTicks(ElapsedTicks / (Frequency / TimeSpan.TicksPerSecond));
                }
                return TimeSpan.FromTicks(ElapsedTicks);
            }
        }

        public long ElapsedMilliseconds
        {
            get
            {
                checked
                {
                    if (IsHighResolution)
                    {
                        return ElapsedTicks / (Frequency / 1000);
                    }
                    return (long) Elapsed.TotalMilliseconds;
                }
            }
        }

        public long ElapsedTicks
        {
            get { return is_running ? GetTimestamp() - started + elapsed : elapsed; }
        }

        public bool IsRunning
        {
            get { return is_running; }
        }

        public static extern long GetTimestamp();

        public static Stopwatch StartNew()
        {
            var s = new Stopwatch();
            s.Start();
            return s;
        }

        public void Reset()
        {
            elapsed = 0;
            is_running = false;
        }

        public void Start()
        {
            if (is_running)
                return;
            started = GetTimestamp();
            is_running = true;
        }

        public void Stop()
        {
            if (!is_running)
                return;
            elapsed += GetTimestamp() - started;
            is_running = false;
        }

        public void Restart()
        {
            started = GetTimestamp();
            elapsed = 0;
            is_running = true;
        }
    }
}