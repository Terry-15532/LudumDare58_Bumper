// To enable real serial port NFC reading:
//   1. Project Settings → Player → Api Compatibility Level = .NET Framework
//   2. Add  SERIAL_NFC  to Project Settings → Player → Scripting Define Symbols
// Without that define the component still works in Editor via F1/F2 simulation.

#if SERIAL_NFC
using System.IO.Ports;
using System.Threading;
#endif
using UnityEngine;

public class NFCReader : MonoBehaviour {

    [Header("Serial Port")]
    public string portName = "COM3";
    public int    baudRate = 9600;

    [Header("State")]
    public bool blueReady = false;
    public bool redReady  = false;

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
                string line = _port.ReadLine().Trim().ToUpper();
                if (line == "BLUE" || line == "RED") {
                    lock (_lock) { _pending = line; }
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

    void HandleSwipe(string side) {
        if (!Game.instance.nfcMode && !Game.instance.matchStarted) {
            Game.instance.EnterNFCMode();
        }
        if (!Game.instance.nfcMode) return;

        if (side == "BLUE" && !blueReady) {
            blueReady = true;
            Debug.Log("[NFCReader] Blue ready");
            Game.instance.NFCPlayerReady(PlayerSide.Blue);
        }
        else if (side == "RED" && !redReady) {
            redReady = true;
            Debug.Log("[NFCReader] Red ready");
            Game.instance.NFCPlayerReady(PlayerSide.Red);
        }

        if (blueReady && redReady) {
            blueReady = redReady = false;
            Game.instance.NFCStartMatch();
            Debug.Log("[NFCReader] Both players ready — match started");
        }
    }

#if UNITY_EDITOR
    // Editor: F1 = simulate blue swipe, F2 = simulate red swipe
    void LateUpdate() {
        if (Input.GetKeyDown(KeyCode.F1)) HandleSwipe("BLUE");
        if (Input.GetKeyDown(KeyCode.F2)) HandleSwipe("RED");
    }
#endif
}
