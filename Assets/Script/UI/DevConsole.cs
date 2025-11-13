using UnityEngine;

public class DevConsole : MonoBehaviour
{
    private static DevConsole _instance;
    [Header("Hotkeys")]
    public KeyCode toggleKey = KeyCode.BackQuote; // `
    public KeyCode executeKey = KeyCode.Return;    // Enter
    public KeyCode closeKey = KeyCode.Escape;

    [Header("Style")]
    public int fontSize = 16;
    public float width = 600f;
    public float height = 110f;
    public float margin = 12f;

    private bool _open = false;
    private string _input = "";
    private string _output = "You Shouldn't be HERE";

    // 让 Update() 也能触发提交，避免 IMGUI/IME 吞键
    private bool _submitRequested = false;


    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }


    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            _open = !_open;
            if (_open) GUI.FocusControl("DevConsoleInput");
        }

        if (!_open) return;

        if (Input.GetKeyDown(closeKey))
        {
            _open = false;
            return;
        }

        // ★ 兜底：在 Update 里监听回车（包括小键盘 Enter）
        if (Input.GetKeyDown(executeKey) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            _submitRequested = true;
        }
    }

    void OnGUI()
    {
        if (!_open) return;

        var prevTF = GUI.skin.textField.fontSize;
        var prevLB = GUI.skin.label.fontSize;
        var prevBX = GUI.skin.box.fontSize;
        GUI.skin.textField.fontSize = GUI.skin.label.fontSize = GUI.skin.box.fontSize = fontSize;

        float x = (Screen.width - width) * 0.5f;
        float y = Screen.height - height - margin;

        // 背板
        GUI.color = new Color(0, 0, 0, 0.7f);
        GUI.Box(new Rect(x, y, width, height), GUIContent.none);
        GUI.color = Color.white;

        // 输出区
        GUI.Label(new Rect(x + 10, y + 10, width - 20, height - 70), _output);

        // 输入框
        GUI.SetNextControlName("DevConsoleInput");
        _input = GUI.TextField(new Rect(x + 10, y + height - 50, width - 110, 30), _input);

        // “Run” 按钮（鼠标兜底）
        if (GUI.Button(new Rect(x + width - 90, y + height - 50, 80, 30), "Run"))
        {
            Execute(_input.Trim());
            _input = "";
            GUI.FocusControl("DevConsoleInput");
        }

        // ★ 原本的 IMGUI 回车检测（保留）
        var e = Event.current;
        if (e.type == EventType.KeyDown && (e.keyCode == executeKey || e.keyCode == KeyCode.KeypadEnter))
        {
            Execute(_input.Trim());
            _input = "";
            GUI.FocusControl("DevConsoleInput");
            e.Use();
        }

        // ★ 兜底提交：如果 Update() 里收到了回车，这里再执行一次
        if (_submitRequested)
        {
            _submitRequested = false;
            Execute(_input.Trim());
            _input = "";
            GUI.FocusControl("DevConsoleInput");
        }

        GUI.skin.textField.fontSize = prevTF;
        GUI.skin.label.fontSize = prevLB;
        GUI.skin.box.fontSize = prevBX;
    }

    private void Execute(string cmd)
    {
        if (string.IsNullOrEmpty(cmd)) return;

        if (DevTools.Instance == null)
        {
            _output = "DevTools not present (make sure DevTools.cs is in the project and not disabled).";
            Debug.LogWarning("[DevConsole] DevTools.Instance is null");
            return;
        }

        string result = DevTools.Instance.ExecuteCommand(cmd);
        if (string.IsNullOrEmpty(result)) result = "(no output)";
        _output = result;

        // 给点反馈
        SoundManager.Instance?.PlaySound(SoundType.UIClick, null, 0.45f);
        Debug.Log("[DevConsole] " + result);
    }
}


