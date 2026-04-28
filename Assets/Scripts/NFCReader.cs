// To enable real serial port NFC reading:
//   1. Project Settings → Player → Api Compatibility Level = .NET Framework
//   2. Add  SERIAL_NFC  to Project Settings → Player → Scripting Define Symbols
// Without that define the component still works in Editor via F1/F2 simulation.
//
// Protocol (matches Arduino/NFCReader/NFCReader.ino):
//   - Serial, 9600 baud.
//   - Each swipe is one line:  "UID:xx:xx:xx:xx[:xx:xx:xx]"
//   - Lines starting with '#' are diagnostic; ignored here.
//   - Unity side is responsible for slot assignment and profile lookup.

#if SERIAL_NFC
using System.IO.Ports;
using System.Threading;
#endif
using UnityEngine;

public class NFCReader : MonoBehaviour {

    [Header("Serial Port")]
    public string portName = "COM8";
    public int    baudRate = 9600;

#if SERIAL_NFC
    SerialPort _port;
    Thread     _thread;
    string     _pending;
    readonly object _lock = new object();
    bool _running = false;

    void Start() {
        OpenPort();
    }

    void OpenPort() {
        try {
            _port    = new SerialPort(portName, baudRate) { ReadTimeout = 500 };
            _port.Open();
            _running = true;
            _thread  = new Thread(ReadLoop) { IsBackground = true };
            _thread.Start();
            Debug.Log($"[NFCReader] Opened {portName}");
        }
        catch (System.Exception e) {
            Debug.LogWarning($"[NFCReader] Could not open {portName}: {e.Message}");
        }
    }

    void ReadLoop() {
        while (_running && _port != null && _port.IsOpen) {
            try {
                string line = _port.ReadLine().Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;
                if (line.StartsWith("UID:", System.StringComparison.OrdinalIgnoreCase)) {
                    lock (_lock) { _pending = line.Substring(4).ToUpper(); }
                }
            }
            catch (System.TimeoutException) { }
            catch { break; }
        }
    }

    void Update() {
        string msg = null;
        lock (_lock) { msg = _pending; _pending = null; }
        if (msg != null) HandleSwipe(msg);
    }

    void OnDestroy() {
        _running = false;
        try { _port?.Close(); } catch { }
    }
#endif

    // `uid` is the normalized hex form, e.g. "04:64:57:52:5D:6F:80"
    void HandleSwipe(string uid) {
        if (string.IsNullOrEmpty(uid)) return;

        if (!Game.instance.nfcMode && !Game.instance.matchStarted) {
            Game.instance.EnterNFCMode();
        }
        if (!Game.instance.nfcMode) return;

        Debug.Log($"[NFCReader] Swipe UID={uid}");
        Game.instance.OnNFCSwipe(uid);
    }

#if UNITY_EDITOR
    // Editor: F1 / F2 simulate two distinct dummy cards.
    // Useful for testing without hardware.
    const string FakeUidA = "E4:D1:0A:55";
    const string FakeUidB = "7B:2C:9F:31";
    void LateUpdate() {
        if (Input.GetKeyDown(KeyCode.F1)) HandleSwipe(FakeUidA);
        if (Input.GetKeyDown(KeyCode.F2)) HandleSwipe(FakeUidB);
        // F3 swipes a random fresh UID — good for exercising the "new player" flow.
        if (Input.GetKeyDown(KeyCode.F3)) {
            string random = $"{Random.Range(0, 256):X2}:{Random.Range(0, 256):X2}:{Random.Range(0, 256):X2}:{Random.Range(0, 256):X2}";
            HandleSwipe(random);
        }
    }
#endif
}
