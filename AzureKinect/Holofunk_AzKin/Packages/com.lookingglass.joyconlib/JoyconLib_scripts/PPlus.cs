#define DEBUG

using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;

using System.Threading;
using UnityEngine;
using System.Collections.Concurrent;

public class PPlus
{
    public enum DebugType : int
    {
        NONE,
        ALL,
        COMMS,
        THREADING,
        IMU,
        RUMBLE,
    };
	public DebugType debug_type = DebugType.THREADING;
    public enum State : uint
    {
        NOT_ATTACHED,
        DROPPED,
        NO_PPLUSES,
        ATTACHED,
        INPUT_MODE_0x30,
    };
    public State state;
    public enum Button : int
    {
        NONE = 0,
        MIKE = 1,
        LEFT = 2,
        RIGHT = 3,
        LIGHT = 4,
        TEAMS = 5,
    };
    public const int BUTTON_COUNT = 5;

    private 
	IntPtr handle;

    private bool keep_polling = false;
    private int timestamp;

    // PPlus HID report IDs from my PPlus HID sleuthing
    private static readonly int[] REPORT_IDS = new[] { 1, 2, 3, 4, 5, 9, 39, 42, 49, 57, -101 };

    // Report ID 4 (usage 0xFF37) is always 8 bytes, and seems to have all the button info we need...?!
    private const uint MAX_REPORT_LEN = 8;

    private struct Report
    {
        byte[] r;
        System.DateTime t;
        public Report(byte[] report, int len, System.DateTime time)
        {
            r = new byte[len];
            Array.Copy(report, r, len);
            t = time;
        }
        public System.DateTime GetTime()
        {
            return t;
        }
        public byte[] Bytes => r;
    };
    private Queue<Report> reports = new Queue<Report>();
    
    private byte global_count = 0;
    private string debug_str;

	public PPlus(IntPtr handle_)
    {
		handle = handle_;
        debug_type = DebugType.THREADING;
    }
    public void DebugPrint(String s, DebugType d)
    {
        if (debug_type == DebugType.NONE) return;
        if (d == DebugType.ALL || d == debug_type || debug_type == DebugType.ALL)
        {
            Debug.Log(s);
        }
    }
	public int Attach(byte leds_ = 0x0)
    {
        state = State.ATTACHED;
        keep_polling = true;
        return 0;
    }
    public void Detach()
    {
        keep_polling = false;
        if (state > State.DROPPED)
        {
            Debug.Log($"Detaching from PPlus with handle 0x{handle.ToInt64():X8}");
            HIDapi.hid_close(handle);
        }
        state = State.NOT_ATTACHED;

        // and sleep just momentarily to let poll loop drain
        Thread.Sleep((Int32)5);
    }
    private byte ts_enqueue;
    private byte ts_dequeue;
    private System.DateTime ts_prev;
    private byte[] enqueue_buf = new byte[MAX_REPORT_LEN]; // should be indexed by report ID
    private bool reports_equal(byte[] a, byte[] b)
    {
        return a[0] == b[0]
            && a[1] == b[1]
            && a[2] == b[2]
            && a[3] == b[3]
            && a[4] == b[4]
            && a[5] == b[5]
            && a[6] == b[6]
            && a[7] == b[7];
    }
    private int ReceiveRaw()
    {
        if (handle == IntPtr.Zero) return -2;
        HIDapi.hid_set_nonblocking(handle, 1);
        int ret = HIDapi.hid_read(handle, enqueue_buf, new UIntPtr(MAX_REPORT_LEN));

        if (ret > 0)
        {
            lock (reports)
            {
                if (reports.Count > 0 && reports_equal(reports.Peek().Bytes, enqueue_buf))
                {
                    // don't enqueue, it's redundant
                }
                else 
                {
                    reports.Enqueue(new Report(enqueue_buf, ret, System.DateTime.Now));
                    DebugPrint(string.Format("Enqueue. Bytes read: {0:D}. Report ID: {1:X2}. Timestamp: {1:X2}", ret, enqueue_buf[0], enqueue_buf[1]), DebugType.THREADING);
                }
            }
            if (ts_enqueue == enqueue_buf[1])
            {
                //DebugPrint(string.Format("Duplicate timestamp enqueued. TS: {0:X2}", ts_en), DebugType.THREADING);
            }
            ts_enqueue = enqueue_buf[1];
        }
        return ret;
    }
    private Thread PollThreadObj;
    private const int attempts_before_drop = 10000;
    private void Poll()
    {
        //Debug.Log(string.Format("PPlus.Poll(): handle 0x{0:X8}: polling", handle.ToInt64()));

        int attempts = 0;
        while (keep_polling && state > State.NO_PPLUSES)
        {
            int a = ReceiveRaw();
            a = ReceiveRaw();
            if (a > 0)
            {
                //Debug.Log(string.Format("PPlus.Poll(): handle 0x{0:X8}: received", handle.ToInt64()));

                //state = state_.IMU_DATA_OK;
                attempts = 0;
            }
            /*
            else if (attempts > attempts_before_drop)
            {
                state = State.DROPPED;
                Debug.Log(string.Format("PPlus.Poll(): handle 0x{0:X8}: Connection lost. Is the PPlus connected?", handle.ToInt64()));
                break;
            }
            */
            else
            {
                //DebugPrint("Pause 5ms", DebugType.THREADING);
                Thread.Sleep((Int32)5);
            }
            ++attempts;
        }
        Debug.Log(string.Format("PPlus.Poll(): handle 0x{0:X8}: End poll loop.", handle.ToInt64()));
    }
    byte[] last_dequeue_buf = new byte[MAX_REPORT_LEN];
    byte[] dequeue_buf = new byte[MAX_REPORT_LEN];
    public void Update()
    {
        if (state > State.NO_PPLUSES)
        {
            while (reports.Count > 0)
            {
                Report rep;
                lock (reports)
                {
                    rep = reports.Dequeue();
                    Array.Copy(rep.Bytes, dequeue_buf, MAX_REPORT_LEN);
                }
                if (ts_dequeue == dequeue_buf[1])
                {
                    //DebugPrint(string.Format("Duplicate timestamp dequeued. TS: {0:X2}", ts_de), DebugType.THREADING);
                }
                ts_dequeue = dequeue_buf[1];
                DebugPrint(string.Format("Dequeue message. Queue length: {0:d}. Report ID: {1:X2}. Timestamp: {2:X2} Lag to dequeue: {3}ms. Lag between packets: {4}ms",
                    reports.Count, dequeue_buf[0], dequeue_buf[1], System.DateTime.Now.Subtract(rep.GetTime()).TotalMilliseconds, rep.GetTime().Subtract(ts_prev).TotalMilliseconds), DebugType.THREADING);
                ts_prev = rep.GetTime();
            }

            bool equals_last_buf = true;
            for (int i = 0; i < MAX_REPORT_LEN; i++)
            {
                if (dequeue_buf[i] != last_dequeue_buf[i])
                {
                    equals_last_buf = false;
                    break;
                }
            }

            if (equals_last_buf)
            {
                // ignore duplicate messages, of which we seem to get a lot
                return;
            }

            Array.Copy(dequeue_buf, last_dequeue_buf, MAX_REPORT_LEN);

            ProcessButtonsAndStick(dequeue_buf);

            /*
            System.Text.StringBuilder stringBuilder = null;
            for (int i = 0; i < this_report_buf.Length; i++)
            {
                if (this_report_buf[i] != last_report_buf[i]
                    && !skip_indices.Contains(i))
                { 
                    if (stringBuilder == null)
                    {
                        stringBuilder = new System.Text.StringBuilder();
                    }
                    stringBuilder.Append($"[{i}]: {last_report_buf[i]:X2} => {this_report_buf[i]:X2}\n");
                }
                last_report_buf[i] = this_report_buf[i];
            }
            if (stringBuilder != null)
            {
                DebugPrint($"Report bufs differ for PPlus #{this.handle.ToInt64():0x}:\n{stringBuilder.ToString()}", DebugType.THREADING);
            }
            else
            {
                //DebugPrint("Report bufs are identical.", DebugType.THREADING);
            }
            */
        }
    }

    /// <summary>
    /// Event for a Presenter Plus button press; if down, the button just went down; if !down, the button just went up.
    /// </summary>
    public struct ButtonEvent
    {
        public readonly Button button;
        public readonly bool down;
        public ButtonEvent(Button b, bool d)
        {
            button = b;
            down = d;
        }
    }

    private ConcurrentQueue<ButtonEvent> queue = new ConcurrentQueue<ButtonEvent>();

    /// <summary>
    /// Poll this to get all events.
    /// </summary>
    public bool TryDequeueEvent(out ButtonEvent evt)
    {
        return queue.TryDequeue(out evt);
    }

    private void ProcessButtonsAndStick(byte[] report_buf)
    {
        if (report_buf[0] == 0x00) return;

        if (report_buf[0] != 0x04) return;

        // Mike: 043F010000000000 down, 043F000000000000 up
        // Left: 043B010000000000 down, 043B000000000000
        // Right: 043C010000000000 down, 043C000000000000 up
        // Light: 043D010000000000 down, 043D000000000000 up
        // Teams: 043E010000000000 down, 043E000000000000 up
        Button b = Button.NONE;
        switch (report_buf[1])
        {
            case 0x3F: b = Button.MIKE; break;
            case 0x3B: b = Button.LEFT; break;
            case 0x3C: b = Button.RIGHT; break;
            case 0x3D: b = Button.LIGHT; break;
            case 0x3E: b = Button.TEAMS; break;
        }
        bool down = report_buf[2] == 0x1;

        // 0x04 report (keyboard) is 8 bytes
        DebugPrint(string.Format("{0:X2}{1:X2}{2:X2}{3:X2}{4:X2}{5:X2}{6:X2}{7:X2} - posting ButtonEvent button: {8}, down: {9}",
            report_buf[0], report_buf[1], report_buf[2], report_buf[3], report_buf[4], report_buf[5], report_buf[6], report_buf[7], b, down), DebugType.THREADING);

        queue.Enqueue(new ButtonEvent(b, down));
    }
	
    public void Begin()
    {
        Debug.Log(string.Format("PPlus.Begin(): beginning for handle 0x{0:X8}", handle.ToInt64()));

        if (PollThreadObj == null)
        {
            PollThreadObj = new Thread(new ThreadStart(Poll));
            PollThreadObj.Start();
        }
    }
}
