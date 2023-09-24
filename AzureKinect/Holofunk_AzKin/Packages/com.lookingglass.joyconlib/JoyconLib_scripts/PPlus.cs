#define DEBUG

using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;

using System.Threading;
using UnityEngine;

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
    public bool isLeft;
    public enum state_ : uint
    {
        NOT_ATTACHED,
        DROPPED,
        NO_PPLUSES,
        ATTACHED,
        INPUT_MODE_0x30,
    };
    public state_ state;
    public enum Button : int
    {
        MICROPHONE = 0,
        LEFT = 1,
        RIGHT = 2,
        LIGHT = 3,
        TEAMS = 4,
    };
    public const int BUTTON_COUNT = 5;
    private bool[] buttons_down = new bool[BUTTON_COUNT];
    private bool[] buttons_up = new bool[BUTTON_COUNT];
    private bool[] buttons = new bool[BUTTON_COUNT];
    private bool[] down_ = new bool[BUTTON_COUNT];

    private 
	IntPtr handle;

    private bool stop_polling = false;
    private int timestamp;

    // PPlus HID report IDs from my PPlus HID sleuthing
    private static readonly int[] REPORT_IDS = new[] { 1, 2, 3, 4, 5, 9, 39, 42, 49, 57, -101 };
    private static readonly int REPORT_ID_COUNT = REPORT_IDS.Length;
    private const int KEYBOARD_REPORT_ID = 1;

    private Dictionary<int, int> report_id_counts = new Dictionary<int, int>();

    private const uint MAX_REPORT_LEN = 256;

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
    public bool GetButtonDown(Button b)
    {
        return buttons_down[(int)b];
    }
    public bool GetButton(Button b)
    {
        return buttons[(int)b];
    }
    public bool GetButtonUp(Button b)
    {
        return buttons_up[(int)b];
    }
	public int Attach(byte leds_ = 0x0)
    {
        // DO NOTHING YET
        return 0;
    }
    public void Detach()
    {
        stop_polling = true;
        if (state > state_.DROPPED)
        {
            HIDapi.hid_close(handle);
        }
        state = state_.NOT_ATTACHED;
    }
    private byte ts_enqueue;
    private byte ts_dequeue;
    private System.DateTime ts_prev;
    private byte[] raw_buf = new byte[MAX_REPORT_LEN]; // should be indexed by report ID
    private int ReceiveRaw()
    {
        if (handle == IntPtr.Zero) return -2;
        HIDapi.hid_set_nonblocking(handle, 0);
        int ret = HIDapi.hid_read(handle, raw_buf, new UIntPtr(MAX_REPORT_LEN));

        if (ret > 0)
        {
            lock (reports)
            {
                reports.Enqueue(new Report(raw_buf, ret, System.DateTime.Now));
            }
            if (ts_enqueue == raw_buf[1])
            {
                //DebugPrint(string.Format("Duplicate timestamp enqueued. TS: {0:X2}", ts_en), DebugType.THREADING);
            }
            ts_enqueue = raw_buf[1];
            DebugPrint(string.Format("Enqueue. Bytes read: {0:D}. Report ID: {1:X2}. Timestamp: {1:X2}", ret, raw_buf[0], raw_buf[1]), DebugType.THREADING);
        }
        return ret;
    }
    private Thread PollThreadObj;
    private void Poll()
    {
        int attempts = 0;
        while (!stop_polling & state > state_.NO_PPLUSES)
        {
            int a = ReceiveRaw();
            a = ReceiveRaw();
            if (a > 0)
            {
                //state = state_.IMU_DATA_OK;
                attempts = 0;
            }
            else if (attempts > 1000)
            {
                state = state_.DROPPED;
                DebugPrint("Connection lost. Is the PPlus connected?", DebugType.ALL);
                break;
            }
            else
            {
                DebugPrint("Pause 5ms", DebugType.THREADING);
                Thread.Sleep((Int32)5);
            }
            ++attempts;
        }
        DebugPrint("End poll loop.", DebugType.THREADING);
    }
    byte[] last_report_buf = new byte[MAX_REPORT_LEN];
    int last_report_len = 0;
    byte[] this_report_buf = new byte[MAX_REPORT_LEN];
    int this_report_len = 0;
    public void Update()
    {
        if (state > state_.NO_PPLUSES)
        {
            while (reports.Count > 0)
            {
                Report rep;
                lock (reports)
                {
                    rep = reports.Dequeue();
                    Array.Copy(rep.Bytes, this_report_buf, rep.Bytes.Length);
                    this_report_len = rep.Bytes.Length;
                }
                if (ts_dequeue == this_report_buf[1])
                {
                    //DebugPrint(string.Format("Duplicate timestamp dequeued. TS: {0:X2}", ts_de), DebugType.THREADING);
                }
                ts_dequeue = this_report_buf[1];
                DebugPrint(string.Format("Dequeue. Queue length: {0:d}. Packet length: {1:d}. Packet ID: {2:X2}. Timestamp: {3:X2}. Lag to dequeue: {4:s}. Lag between packets: {5:s}",
                    reports.Count, this_report_len, this_report_buf[0], this_report_buf[1], System.DateTime.Now.Subtract(rep.GetTime()), rep.GetTime().Subtract(ts_prev)), DebugType.THREADING);
                ts_prev = rep.GetTime();
            }
            ProcessButtonsAndStick(this_report_buf);

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
    private int ProcessButtonsAndStick(byte[] report_buf)
    {
        if (report_buf[0] == 0x00) return -1;

        lock (buttons)
        {
            lock (down_)
            {
                for (int i = 0; i < buttons.Length; ++i)
                {
                    down_[i] = buttons[i];
                }
            }
            /*
            buttons[(int)Button.DPAD_DOWN] = (report_buf[3 + (isLeft ? 2 : 0)] & (isLeft ? 0x01 : 0x04)) != 0;
            buttons[(int)Button.DPAD_RIGHT] = (report_buf[3 + (isLeft ? 2 : 0)] & (isLeft ? 0x04 : 0x08)) != 0;
            buttons[(int)Button.DPAD_UP] = (report_buf[3 + (isLeft ? 2 : 0)] & (isLeft ? 0x02 : 0x02)) != 0;
            buttons[(int)Button.DPAD_LEFT] = (report_buf[3 + (isLeft ? 2 : 0)] & (isLeft ? 0x08 : 0x01)) != 0;
            buttons[(int)Button.HOME] = ((report_buf[4] & 0x10) != 0);
            buttons[(int)Button.MINUS] = ((report_buf[4] & 0x01) != 0);
            buttons[(int)Button.PLUS] = ((report_buf[4] & 0x02) != 0);
            buttons[(int)Button.STICK] = ((report_buf[4] & (isLeft ? 0x08 : 0x04)) != 0);
            buttons[(int)Button.SHOULDER_1] = (report_buf[3 + (isLeft ? 2 : 0)] & 0x40) != 0;
            buttons[(int)Button.SHOULDER_2] = (report_buf[3 + (isLeft ? 2 : 0)] & 0x80) != 0;
            buttons[(int)Button.SR] = (report_buf[3 + (isLeft ? 2 : 0)] & 0x10) != 0;
            buttons[(int)Button.SL] = (report_buf[3 + (isLeft ? 2 : 0)] & 0x20) != 0;
            */
            lock (buttons_up)
            {
                lock (buttons_down)
                {
                    for (int i = 0; i < buttons.Length; ++i)
                    {
                        buttons_up[i] = (down_[i] & !buttons[i]);
                        buttons_down[i] = (!down_[i] & buttons[i]);
                    }
                }
            }
        }
        return 0;
    }
	
    public void Begin()
    {
        if (PollThreadObj == null)
        {
            PollThreadObj = new Thread(new ThreadStart(Poll));
            PollThreadObj.Start();
        }
    }
    private byte[] Subcommand(byte sc, byte[] buf, uint len, bool print = true)
    {
        byte[] response = new byte[0];
        /*
        byte[] buf_ = new byte[report_len];
        Array.Copy(default_buf, 0, buf_, 2, 8);
        Array.Copy(buf, 0, buf_, 11, len);
        buf_[10] = sc;
        buf_[1] = global_count;
        buf_[0] = 0x1;
        if (global_count == 0xf) global_count = 0;
        else ++global_count;
        if (print) { PrintArray(buf_, DebugType.COMMS, len, 11, "Subcommand 0x" + string.Format("{0:X2}", sc) + " sent. Data: 0x{0:S}"); };
        HIDapi.hid_write(handle, buf_, new UIntPtr(len + 11));
        int res = HIDapi.hid_read_timeout(handle, response, new UIntPtr(report_len), 50);
        if (res < 1) DebugPrint("No response.", DebugType.COMMS);
        else if (print) { PrintArray(response, DebugType.COMMS, report_len - 1, 1, "Response ID 0x" + string.Format("{0:X2}", response[0]) + ". Data: 0x{0:S}"); }
        */
        return response;
    }

    private void PrintArray<T>(T[] arr, DebugType d = DebugType.NONE, uint len = 0, uint start = 0, string format = "{0:S}")
    {
        if (d != debug_type && debug_type != DebugType.ALL) return;
        if (len == 0) len = (uint)arr.Length;
        string tostr = "";
        for (int i = 0; i < len; ++i)
        {
            tostr += string.Format((arr[0] is byte) ? "{0:X2} " : ((arr[0] is float) ? "{0:F} " : "{0:D} "), arr[i + start]);
        }
        DebugPrint(string.Format(format, tostr), d);
    }
}
