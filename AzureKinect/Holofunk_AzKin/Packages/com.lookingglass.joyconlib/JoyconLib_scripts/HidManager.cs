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

		Debug.Log("Found device(s) with PPlus vendor ID.");
		hid_device_info enumerate;
		int i = 0;
		while (ptr != IntPtr.Zero)
		{
			enumerate = (hid_device_info)Marshal.PtrToStructure(ptr, typeof(hid_device_info));

			Debug.Log(enumerate.product_id);
			if (enumerate.product_id == pplus_product_id)
			{
				IntPtr handle = HIDapi.hid_open_path(enumerate.path);
				Debug.Log(string.Format("PPlus detected!!! Handle is 0x{0:X8}", handle.ToInt64()));
				//HIDapi.hid_set_nonblocking(handle, 1);
				pplus_list.Add(new PPlus(handle));
				++i;
			}
			else
			{
				Debug.Log("Non Joy-Con input device skipped.");
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
		for (int i = 0; i < pplus_list.Count; ++i)
		{
			pplus_list[i].Detach();
		}
	}
}
