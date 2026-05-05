// PC/SC-based NFC reader for the ACR122U (and any other PC/SC-compatible reader).
//
// Replaces the previous Arduino+MFRC522 serial path. Communicates with the
// Windows Smart Card subsystem via winscard.dll, so no SERIAL_NFC define and
// no COM-port plumbing is needed.
//
// Setup (Windows):
//   - Plug in the ACR122U. Windows should auto-install the ACS driver. If not,
//     install the ACR122U PC/SC driver from acs.com.hk.
//   - The "Smart Card" service (SCardSvr) must be running (it is by default).
//   - Verify the reader is visible: in PowerShell, `Get-Service SCardSvr` and
//     check Device Manager → Smart card readers → "ACS ACR122 0".
//   - In Project Settings → Player, Api Compatibility Level must be
//     ".NET Framework" (so DllImport/Marshal are available).
//
// Protocol:
//   - PC/SC GET DATA APDU (FF CA 00 00 00) returns the card UID + 9000.
//   - Works for 4-byte MIFARE Classic, 7-byte DESFire / Ultralight / NTAG, etc.
//   - UID is reported as colon-separated uppercase hex, matching the prior
//     Arduino sketch's wire format (e.g. "04:64:57:52:5D:6F:80").

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using UnityEngine;

public class NFCReader : MonoBehaviour {

    [Header("PC/SC Reader")]
    [Tooltip("Substring of the reader's name to match. Empty = use the first reader found.")]
    public string readerName = "";

    [Tooltip("Log every reader event and UID read to the Unity console.")]
    public bool verbose = true;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    // ── PC/SC constants ───────────────────────────────────────────────────
    const uint SCARD_SCOPE_USER       = 0;
    const uint SCARD_SHARE_SHARED     = 2;
    const uint SCARD_PROTOCOL_T0      = 1;
    const uint SCARD_PROTOCOL_T1      = 2;
    const uint SCARD_LEAVE_CARD       = 0;

    const uint SCARD_STATE_UNAWARE    = 0x00000000;
    const uint SCARD_STATE_CHANGED    = 0x00000002;
    const uint SCARD_STATE_PRESENT    = 0x00000020;

    const uint INFINITE               = 0xFFFFFFFF;

    [StructLayout(LayoutKind.Sequential)]
    struct SCARD_IO_REQUEST {
        public uint dwProtocol;
        public uint cbPciLength;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    struct SCARD_READERSTATE {
        [MarshalAs(UnmanagedType.LPStr)] public string szReader;
        public IntPtr pvUserData;
        public uint   dwCurrentState;
        public uint   dwEventState;
        public uint   cbAtr;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 36)] public byte[] rgbAtr;
    }

    [DllImport("winscard.dll")]
    static extern int SCardEstablishContext(uint dwScope, IntPtr r1, IntPtr r2, out IntPtr phContext);
    [DllImport("winscard.dll")]
    static extern int SCardReleaseContext(IntPtr hContext);
    [DllImport("winscard.dll", CharSet = CharSet.Ansi, EntryPoint = "SCardListReadersA")]
    static extern int SCardListReaders(IntPtr hContext, byte[] mszGroups, byte[] mszReaders, ref uint pcchReaders);
    [DllImport("winscard.dll", CharSet = CharSet.Ansi, EntryPoint = "SCardConnectA")]
    static extern int SCardConnect(IntPtr hContext, string szReader, uint dwShareMode, uint dwPreferredProtocols, out IntPtr phCard, out uint pdwActiveProtocol);
    [DllImport("winscard.dll")]
    static extern int SCardDisconnect(IntPtr hCard, uint dwDisposition);
    [DllImport("winscard.dll")]
    static extern int SCardTransmit(IntPtr hCard, ref SCARD_IO_REQUEST pioSend, byte[] pbSend, uint cbSend, IntPtr pioRecvPci, byte[] pbRecv, ref uint pcbRecv);
    [DllImport("winscard.dll", CharSet = CharSet.Ansi, EntryPoint = "SCardGetStatusChangeA")]
    static extern int SCardGetStatusChange(IntPtr hContext, uint dwTimeout, [In, Out] SCARD_READERSTATE[] rgReaderStates, uint cReaders);

    IntPtr  _ctx = IntPtr.Zero;
    Thread  _thread;
    volatile bool _running;
    string  _resolvedReader;

    string  _pending;
    readonly object _lock = new object();

    void Start() {
        int r = SCardEstablishContext(SCARD_SCOPE_USER, IntPtr.Zero, IntPtr.Zero, out _ctx);
        if (r != 0) {
            Debug.LogWarning($"[NFCReader] SCardEstablishContext failed (0x{r:X8}). Is the Smart Card service running?");
            return;
        }

        _resolvedReader = ResolveReader();
        if (string.IsNullOrEmpty(_resolvedReader)) {
            Debug.LogWarning("[NFCReader] No PC/SC reader found. Plug in the ACR122U and make sure its driver is installed.");
            return;
        }

        Debug.Log($"[NFCReader] Using reader: {_resolvedReader}");
        _running = true;
        _thread  = new Thread(ReadLoop) { IsBackground = true, Name = "NFCReader" };
        _thread.Start();
    }

    string ResolveReader() {
        // Two-call pattern: first SCardListReaders fills the required buffer length.
        uint len = 0;
        int r = SCardListReaders(_ctx, null, null, ref len);
        if (r != 0 || len == 0) return null;
        byte[] buf = new byte[len];
        r = SCardListReaders(_ctx, null, buf, ref len);
        if (r != 0) return null;

        // The buffer is a multi-string: each name is null-terminated, list
        // ends with an extra null. Split on '\0' and skip empties.
        string all = Encoding.ASCII.GetString(buf, 0, (int)len);
        string[] readers = all.Split('\0');

        if (!string.IsNullOrEmpty(readerName)) {
            foreach (var n in readers) {
                if (!string.IsNullOrEmpty(n) && n.IndexOf(readerName, StringComparison.OrdinalIgnoreCase) >= 0)
                    return n;
            }
            Debug.LogWarning($"[NFCReader] Reader matching \"{readerName}\" not found; using first available.");
        }
        foreach (var n in readers) if (!string.IsNullOrEmpty(n)) return n;
        return null;
    }

    void ReadLoop() {
        var states = new SCARD_READERSTATE[1];
        states[0] = new SCARD_READERSTATE {
            szReader       = _resolvedReader,
            dwCurrentState = SCARD_STATE_UNAWARE,
            rgbAtr         = new byte[36],
        };

        // Debounce: suppress repeat reads of the same held card.
        string lastUid = null;
        DateTime lastSwipe = DateTime.MinValue;

        while (_running) {
            // 1s timeout so we can poll _running and exit promptly on shutdown.
            int r = SCardGetStatusChange(_ctx, 1000, states, 1);
            if (!_running) break;

            // 0x8010000A = SCARD_E_TIMEOUT (no change) — totally normal, just loop.
            if (r == unchecked((int)0x8010000A)) continue;
            if (r != 0) {
                if (verbose) Debug.LogWarning($"[NFCReader] SCardGetStatusChange err 0x{r:X8}; reader unplugged?");
                Thread.Sleep(1000);
                continue;
            }

            uint ev = states[0].dwEventState;
            // Ack the change — copy event into current so the next call only fires on the next change.
            states[0].dwCurrentState = ev & ~SCARD_STATE_CHANGED;

            bool present = (ev & SCARD_STATE_PRESENT) != 0;
            if (!present) {
                // Card removed — clear debounce so re-tapping the same card swipes again.
                lastUid = null;
                continue;
            }

            string uid = TryReadUid();
            if (string.IsNullOrEmpty(uid)) continue;

            var now = DateTime.UtcNow;
            bool dup = uid == lastUid && (now - lastSwipe).TotalMilliseconds < 1000;
            if (dup) continue;

            lastUid   = uid;
            lastSwipe = now;
            if (verbose) Debug.Log($"[NFCReader] <- UID:{uid}");
            lock (_lock) { _pending = uid; }
        }
    }

    string TryReadUid() {
        IntPtr card; uint proto;
        int r = SCardConnect(_ctx, _resolvedReader, SCARD_SHARE_SHARED,
                             SCARD_PROTOCOL_T0 | SCARD_PROTOCOL_T1, out card, out proto);
        if (r != 0) return null;
        try {
            var pci = new SCARD_IO_REQUEST {
                dwProtocol  = proto,
                cbPciLength = (uint)Marshal.SizeOf(typeof(SCARD_IO_REQUEST))
            };
            // GET DATA — UID:  CLA=FF INS=CA P1=00 P2=00 Le=00
            byte[] cmd  = { 0xFF, 0xCA, 0x00, 0x00, 0x00 };
            byte[] resp = new byte[258];
            uint   respLen = (uint)resp.Length;

            r = SCardTransmit(card, ref pci, cmd, (uint)cmd.Length, IntPtr.Zero, resp, ref respLen);
            if (r != 0 || respLen < 2) return null;

            // Last two bytes = SW1 SW2; success is 90 00.
            if (resp[respLen - 2] != 0x90 || resp[respLen - 1] != 0x00) return null;

            int uidLen = (int)respLen - 2;
            var sb = new StringBuilder(uidLen * 3);
            for (int i = 0; i < uidLen; i++) {
                if (i > 0) sb.Append(':');
                sb.Append(resp[i].ToString("X2"));
            }
            return sb.ToString();
        }
        finally {
            SCardDisconnect(card, SCARD_LEAVE_CARD);
        }
    }

    void Update() {
        string msg;
        lock (_lock) { msg = _pending; _pending = null; }
        if (!string.IsNullOrEmpty(msg)) HandleSwipe(msg);
    }

    void OnDestroy() {
        _running = false;
        try {
            if (_thread != null && !_thread.Join(1500)) _thread.Interrupt();
        }
        catch { }
        if (_ctx != IntPtr.Zero) {
            SCardReleaseContext(_ctx);
            _ctx = IntPtr.Zero;
        }
    }
#else
    // Non-Windows builds: no PC/SC. Editor F-key sim below still works on macOS/Linux editor.
    void Update() { }
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
    // Editor: F1 / F2 simulate two distinct dummy cards. F3 = random fresh UID.
    const string FakeUidA = "E4:D1:0A:55";
    const string FakeUidB = "7B:2C:9F:31";
    void LateUpdate() {
        if (Input.GetKeyDown(KeyCode.F1)) HandleSwipe(FakeUidA);
        if (Input.GetKeyDown(KeyCode.F2)) HandleSwipe(FakeUidB);
        if (Input.GetKeyDown(KeyCode.F3)) {
            string random = $"{UnityEngine.Random.Range(0, 256):X2}:{UnityEngine.Random.Range(0, 256):X2}:{UnityEngine.Random.Range(0, 256):X2}:{UnityEngine.Random.Range(0, 256):X2}";
            HandleSwipe(random);
        }
    }
#endif
}
