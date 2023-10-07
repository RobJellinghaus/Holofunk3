using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;
using System;
public class HidManager: MonoBehaviour
{
    // Settings accessible via Unity
    public bool EnableIMU = true;
    public bool EnableLocalize = true;

	private const ushort joycon_vendor_id = 0x057e;
	private const ushort joycon_product_l_id = 0x2006;
	private const ushort joycon_product_r_id = 0x2007;

	private const ushort pplus_vendor_id = 0x045e;
	private const ushort pplus_product_id = 0x0851;
	// The reports we receive from button presses are all for report ID 4, which is this:
    //	Report Descriptor: (23 bytes)
    //0x06, 0x00, 0xff, 0x0a, 0x37, 0xff, 0xa1, 0x01, 0x85, 0x04,
    //0x09, 0x03, 0x15, 0x00, 0x25, 0xff, 0x75, 0x08, 0x95, 0x07,
    //0x81, 0x02, 0xc0,
    //Device Found
    //  type: 045e 0851
    //  path: \\?\HID#{00001812-0000-1000-8000-00805f9b34fb}_Dev_VID&02045e_PID&0851_REV&0125_ec5a511e8abe&Col06#9&2105eab6&0&0005#{4d1e55b2-f16f-11cf-88cb-001111000030}
    //  serial_number: ec5a511e8abe
    //  Manufacturer: Microsoft
    //  Product:      Microsoft Presenter+
    //  Release:      125
    //  Interface:    -1
    //  Usage(page) : 0xff38 (0xff00)
    //  Bus type: 2
    //
    //0x06, 0x00, 0xFF,  // Usage Page (Vendor Defined 0xFF00)
    //0x0A, 0x37, 0xFF,  // Usage (0xFF37)
    //0xA1, 0x01,        // Collection (Application)
    //0x85, 0x04,        //   Report ID (4)
    //0x09, 0x03,        //   Usage (0x03)
    //0x15, 0x00,        //   Logical Minimum (0)
    //0x25, 0xFF,        //   Logical Maximum (-1)
    //0x75, 0x08,        //   Report Size (8)
    //0x95, 0x07,        //   Report Count (7)
    //0x81, 0x02,        //   Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null Position)
    //0xC0,              // End Collection
	private const ushort pplus_usage_for_report_4 = 0xFF37;

    public List<Joycon> joycon_list = new List<Joycon>(); // Array of all connected Joy-Cons
	public List<PPlus> pplus_list = new List<PPlus>(); // Array of all connected PPluses
    static HidManager instance;

    public static HidManager Instance
    {
        get { return instance; }
    }

	void Awake()
	{
		if (instance != null) Destroy(gameObject);
		instance = this;

		HIDapi.hid_init();

		//AwakeJoycons();
		AwakePplus();
	}

	void AwakePplus()
    {
		IntPtr ptr = HIDapi.hid_enumerate(pplus_vendor_id, 0x0);
		IntPtr top_ptr = ptr;

		if (ptr == IntPtr.Zero)
		{
			HIDapi.hid_free_enumeration(ptr);
			Debug.Log("No PPluses found. Oh well. So sad.");
		}

		Debug.Log($"Found device(s) with PPlus vendor ID. ptr is 0x{ptr.ToInt64():X8}");
		hid_device_info enumerate;
		int i = 0;
		while (ptr != IntPtr.Zero)
		{
			enumerate = (hid_device_info)Marshal.PtrToStructure(ptr, typeof(hid_device_info));

			if (enumerate.product_id == pplus_product_id && enumerate.usage == pplus_usage_for_report_4)
			{
				IntPtr handle = HIDapi.hid_open_path(enumerate.path);
				Debug.Log(string.Format("PPlus detected!!! Handle is 0x{0:X8} - usage is 0x{1:X2}", handle.ToInt64(), enumerate.usage));
				HIDapi.hid_set_nonblocking(handle, 1);
				pplus_list.Add(new PPlus(handle));
				++i;
			}
			else
			{
				Debug.Log("Non-PPlus input device skipped.");
			}
			ptr = enumerate.next;
		}
		HIDapi.hid_free_enumeration(top_ptr);
	}

	void AwakeJoycons()
	{
		bool isLeft = false;

		IntPtr ptr = HIDapi.hid_enumerate(joycon_vendor_id, 0x0);
		IntPtr top_ptr = ptr;

		if (ptr == IntPtr.Zero)
		{
			HIDapi.hid_free_enumeration(ptr);
			Debug.Log ("No Joy-Cons found. Oh well. So sad.");
		}
		hid_device_info enumerate;
		int i = 0;
		while (ptr != IntPtr.Zero) {
			enumerate = (hid_device_info)Marshal.PtrToStructure (ptr, typeof(hid_device_info));

			Debug.Log (enumerate.product_id);
				if (enumerate.product_id == joycon_product_l_id || enumerate.product_id == joycon_product_r_id) {
					if (enumerate.product_id == joycon_product_l_id) {
						isLeft = true;
						Debug.Log ("Left Joy-Con connected!!!");
					} else if (enumerate.product_id == joycon_product_r_id) {
						isLeft = false;
						Debug.Log ("Right Joy-Con connected!!!");
					} else {
						Debug.Log ("Non Joy-Con input device skipped.");
					}
					IntPtr handle = HIDapi.hid_open_path (enumerate.path);
					HIDapi.hid_set_nonblocking (handle, 1);
					joycon_list.Add (new Joycon (handle, EnableIMU, EnableLocalize & EnableIMU, 0.05f, isLeft));
					++i;
				}
				ptr = enumerate.next;
			}
		HIDapi.hid_free_enumeration (top_ptr);
    }

    void Start()
    {
		for (int i = 0; i < joycon_list.Count; ++i)
		{
			Joycon jc = joycon_list[i];
			byte LEDs = 0x0;
			LEDs |= (byte)(0x1 << i);
			jc.Attach(leds_: LEDs);
			jc.Begin();
		}

		for (int i = 0; i < pplus_list.Count; ++i)
		{
			PPlus pp = pplus_list[i];
			pp.Attach();
			pp.Begin();
		}
	}

	void Update()
    {
		for (int i = 0; i < joycon_list.Count; ++i)
		{
			joycon_list[i].Update();
		}
		for (int i = 0; i < pplus_list.Count; ++i)
		{
			pplus_list[i].Update();
		}
	}

	void OnApplicationQuit()
    {
		for (int i = 0; i < joycon_list.Count; ++i)
		{
			joycon_list[i].Detach();
		}

		Debug.Log($"HidManager.OnApplicationQuit(): detaching {pplus_list.Count} PPluses.");
		for (int i = 0; i < pplus_list.Count; ++i)
		{
			pplus_list[i].Detach();
		}
	}
}
