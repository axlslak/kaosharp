using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace AOSharp.Core
{
    // TEMPORARY: remove this helper, Game hooks and Sync benchmark after the trial.
    // Runs on the game thread only. No per-update allocation or output.
    public static class TemporaryUpdateTiming
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

        private sealed class Sample
        {
            internal long WorkCount, WorkTicks, WorkMax;
            internal long GapCount, GapTicks, GapMax;
            internal long SyncCount, SyncTicks, SyncMax;

            internal string Summary(string prefix)
            {
                return " " + prefix + "work_n=" + WorkCount
                    + " " + prefix + "work_avg_ms=" + Milliseconds(WorkTicks, WorkCount)
                    + " " + prefix + "work_max_ms=" + Milliseconds(WorkMax, 1)
                    + " " + prefix + "gap_n=" + GapCount
                    + " " + prefix + "gap_avg_ms=" + Milliseconds(GapTicks, GapCount)
                    + " " + prefix + "gap_max_ms=" + Milliseconds(GapMax, 1)
                    + " " + prefix + "sync_n=" + SyncCount
                    + " " + prefix + "sync_avg_ms=" + Milliseconds(SyncTicks, SyncCount)
                    + " " + prefix + "sync_max_ms=" + Milliseconds(SyncMax, 1);
            }
        }

        private static bool _enabled, _inUpdate, _syncMarked;
        private static uint _processId;
        private static long _entry, _lastExit;
        private static Sample _foreground, _background, _current, _previous;

        private static string Milliseconds(long ticks, long count) => count == 0 ? "n/a"
            : (ticks * (1000.0 / Stopwatch.Frequency) / count).ToString("0.000", CultureInfo.InvariantCulture);

        public static void StartArea()
        {
            using (var process = Process.GetCurrentProcess())
                _processId = (uint)process.Id;
            _foreground = new Sample();
            _background = new Sample();
            _current = _previous = null;
            _lastExit = 0;
            _inUpdate = false;
            _enabled = true;
        }

        internal static void BeginUpdate()
        {
            if (!_enabled) return;
            long now = Stopwatch.GetTimestamp();
            if (_lastExit != 0)
            {
                long gap = now - _lastExit;
                _previous.GapCount++;
                _previous.GapTicks += gap;
                if (gap > _previous.GapMax) _previous.GapMax = gap;
            }
            uint processId;
            GetWindowThreadProcessId(GetForegroundWindow(), out processId);
            _current = processId == _processId ? _foreground : _background;
            _entry = now;
            _inUpdate = true;
            _syncMarked = false;
        }

        public static void MarkSync()
        {
            if (!_enabled || !_inUpdate || _syncMarked) return;
            long delay = Stopwatch.GetTimestamp() - _entry;
            _current.SyncCount++;
            _current.SyncTicks += delay;
            if (delay > _current.SyncMax) _current.SyncMax = delay;
            _syncMarked = true;
        }

        internal static void EndUpdate()
        {
            if (!_enabled || !_inUpdate) return;
            long now = Stopwatch.GetTimestamp();
            long work = now - _entry;
            _current.WorkCount++;
            _current.WorkTicks += work;
            if (work > _current.WorkMax) _current.WorkMax = work;
            _lastExit = now;
            _previous = _current;
            _inUpdate = false;
        }

        public static string FinishArea()
        {
            if (!_enabled) return " core_timing=inactive";
            bool partial = _inUpdate;
            Stop();
            return " core_timing=entry-exit-v1 core_partial=" + (partial ? "1" : "0")
                + _foreground.Summary("core_FG_") + _background.Summary("core_BG_");
        }

        public static void Stop()
        {
            _enabled = _inUpdate = false;
            _lastExit = 0;
        }
    }
}
