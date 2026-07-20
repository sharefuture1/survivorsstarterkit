using Godot;

public partial class Hud : Control
{
    private GameManager _gameManager;

    private ProgressBar _xpBar;
    private ProgressBar _lifeBar;
    private Label _gameTimeLabel;
    private Label _levelLabel;
    private Label _killsLabel;
    private Control _pausePanel;
    private Control _gameOverPanel;
    private Label _gameOverStats;

    public UpgradeView UpgradeView { get; private set; }

    private int _lastSecond = -1;
    private int _lastLevel = -1;
    private int _lastKills = -1;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        _gameManager = GetNode<GameManager>("/root/GameManager");

        _xpBar = GetNode<ProgressBar>("PlayerXPBar");
        _lifeBar = GetNode<ProgressBar>("PlayerLifeBar");
        _gameTimeLabel = GetNode<Label>("GameTime");
        _levelLabel = GetNode<Label>("LevelLabel");
        _killsLabel = GetNode<Label>("KillsLabel");
        UpgradeView = GetNode<UpgradeView>("UpgradeContainer");

        _pausePanel = GetNode<Control>("PausePanel");
        _gameOverPanel = GetNode<Control>("GameOverPanel");
        _gameOverStats = GetNode<Label>("GameOverPanel/CenterContainer/VBoxContainer/Stats");

        GetNode<Button>("PausePanel/CenterContainer/VBoxContainer/ResumeButton").Pressed += _gameManager.TogglePauseMenu;
        GetNode<Button>("PausePanel/CenterContainer/VBoxContainer/RestartButton").Pressed += _gameManager.RestartRun;
        GetNode<Button>("PausePanel/CenterContainer/VBoxContainer/QuitButton").Pressed += () => GetTree().Quit();
        GetNode<Button>("GameOverPanel/CenterContainer/VBoxContainer/RestartButton").Pressed += _gameManager.RestartRun;
        GetNode<Button>("GameOverPanel/CenterContainer/VBoxContainer/QuitButton").Pressed += () => GetTree().Quit();

        _gameManager.RegisterHud(this);
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("ui_cancel")) _gameManager.TogglePauseMenu();

        int second = (int)_gameManager.GameTime;
        if (second != _lastSecond)
        {
            _lastSecond = second;
            _gameTimeLabel.Text = $"{second / 60:00}:{second % 60:00}";
        }

        if (_gameManager.PlayerLevel != _lastLevel)
        {
            _lastLevel = _gameManager.PlayerLevel;
            _levelLabel.Text = $"Lv {_lastLevel}";
        }

        if (_gameManager.Kills != _lastKills)
        {
            _lastKills = _gameManager.Kills;
            _killsLabel.Text = $"Kills: {_lastKills}";
        }

        _xpBar.MaxValue = _gameManager.MaxPlayerXp;
        _xpBar.Value = _gameManager.PlayerXp;

        var player = _gameManager.Player;
        if (player != null)
        {
            _lifeBar.MaxValue = player.MaxLifepoints;
            _lifeBar.Value = player.Lifepoints;
        }
    }

    public void ShowGameOver(double gameTime, int kills, int level)
    {
        int seconds = (int)gameTime;
        _gameOverStats.Text = $"Survived {seconds / 60:00}:{seconds % 60:00}\nLevel {level}  •  {kills} kills";
        _gameOverPanel.Visible = true;
    }

    public void SetPauseMenuVisible(bool visible) => _pausePanel.Visible = visible;
}
