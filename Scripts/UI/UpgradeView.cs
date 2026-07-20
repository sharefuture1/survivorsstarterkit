using Godot;
using System;
using System.Collections.Generic;

public partial class UpgradeView : Control
{
    private PackedScene _choicePanel;

    public event Action<Choice> OnChoose;

    private bool _choiceLocked;

    public override void _Ready()
    {
        _choicePanel = (PackedScene)GD.Load("res://Prefabs/UI/powerup_block.tscn");
        ProcessMode = ProcessModeEnum.Always;
        Clear();
    }

    internal void Clear()
    {
        foreach (var child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }
        _choiceLocked = false;
    }

    internal void SetChoices(List<Choice> choices)
    {
        Clear();

        foreach (var choice in choices)
        {
            var panel = _choicePanel.Instantiate<Button>();
            panel.ProcessMode = ProcessModeEnum.Always;
            panel.Pressed += () =>
            {
                if (_choiceLocked) return;
                _choiceLocked = true;
                OnChoose?.Invoke(choice);
            };
            AddChild(panel);

            var playerName = panel.GetNode<Label>("MarginContainer/VBoxContainer/VBoxPlayer/Name");
            var playerDescription = panel.GetNode<Label>("MarginContainer/VBoxContainer/VBoxPlayer/Description");
            if (choice.Powerup != null)
            {
                playerName.Text = choice.Powerup.Name;
                playerDescription.Text = string.Format(choice.Powerup.Description, Math.Round(choice.PlayerValue, 1));
            }
            else
            {
                playerName.Text = "No upgrade left";
                playerDescription.Text = "Every upgrade of this run is maxed out.";
            }

            var enemyName = panel.GetNode<Label>("MarginContainer/VBoxContainer/VBoxEnemy/Name");
            var enemyDescription = panel.GetNode<Label>("MarginContainer/VBoxContainer/VBoxEnemy/Description");
            if (choice.EnemyPowerup != null)
            {
                enemyName.Text = choice.EnemyPowerup.Name;
                enemyDescription.Text = string.Format(choice.EnemyPowerup.Description, Math.Round(choice.EnemyValue, 1));
            }
            else
            {
                enemyName.Text = "Nothing";
                enemyDescription.Text = "The horde has no trick left to learn.";
            }
        }
    }

    // Dims the discarded panels; children stay in place until Clear() frees them.
    internal void DisplayChoicePicked(int chosenIndex)
    {
        var children = GetChildren();
        for (int i = 0; i < children.Count; i++)
        {
            if (children[i] is not Button button) continue;
            button.Disabled = true;
            if (i != chosenIndex) button.Modulate = new Color(1, 1, 1, 0.25f);
        }
    }
}
