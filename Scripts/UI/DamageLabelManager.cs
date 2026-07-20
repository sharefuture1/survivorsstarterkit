using Godot;
using System.Collections.Generic;

public partial class DamageLabelManager : Control
{
    public const int InitialPoolSize = 50;

    private readonly Queue<Label> _pool = new();

    private LabelSettings _labelSettings;
    private Camera3D _camera;
    private GameManager _gameManager;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        MouseFilter = MouseFilterEnum.Ignore;

        _camera = GetViewport().GetCamera3D();
        _gameManager = GetNode<GameManager>("/root/GameManager");
        _gameManager.OnEnemyHit += OnEnemyHit;

        // One shared immutable settings object; allocating a new one per label defeats the pool.
        _labelSettings = new LabelSettings()
        {
            FontColor = new Color(1, 0, 0),
            FontSize = 30,
            OutlineSize = 4,
            OutlineColor = new Color(0, 0, 0),
        };

        for (int i = 0; i < InitialPoolSize; ++i)
        {
            _pool.Enqueue(CreateLabel());
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        // The GameManager autoload outlives this scene; a stale handler would target a freed node.
        _gameManager.OnEnemyHit -= OnEnemyHit;

        // Pooled labels are not in the tree, so they must be freed manually.
        while (_pool.Count > 0) _pool.Dequeue().Free();
    }

    private Label CreateLabel() => new()
    {
        ProcessMode = ProcessModeEnum.Always,
        LabelSettings = _labelSettings,
        MouseFilter = MouseFilterEnum.Ignore,
    };

    private Label GetLabel()
    {
        var label = _pool.Count == 0 ? CreateLabel() : _pool.Dequeue();
        label.Modulate = new Color(1, 1, 1, 1);
        AddChild(label);
        return label;
    }

    private void ReturnToPool(Label label)
    {
        _pool.Enqueue(label);
        RemoveChild(label);
    }

    private void OnEnemyHit(Enemy enemy, int damages)
    {
        var label = GetLabel();
        label.Text = damages.ToString();

        Vector2 labelPos = _camera.UnprojectPosition(enemy.GlobalPosition);
        labelPos.X -= label.Size.X / 2;
        labelPos.Y -= 50;
        label.Position = labelPos;
        var tween = CreateTween();
        tween.TweenProperty(label, "position:y", labelPos.Y - 50, 0.75f);
        tween.Parallel().TweenProperty(label, "modulate", new Color(0, 0, 0, 0), 0.5f).SetDelay(0.25f);
        tween.TweenCallback(Callable.From(() => ReturnToPool(label)));
    }
}
