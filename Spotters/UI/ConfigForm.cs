using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Spotters.UI;

public sealed class ConfigForm : Form
{
    private readonly ComboBox _deviceCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 350 };
    private readonly TextBox _userNameText = new() { Width = 200 };
    private readonly Button _addButton = new() { Text = "Add user", Width = 100 };
    private readonly Button _saveButton = new() { Text = "Save", Width = 100 };
    private readonly ListBox _usersList = new() { Width = 450, Height = 150 };
    private readonly Button _removeButton = new() { Text = "Remove selected", Width = 150 };

    private readonly AppConfig _original;
    public AppConfig? Result { get; private set; }

    private List<string> _devices = new();

    public ConfigForm(AppConfig original)
    {
        _original = Clone(original);
        Text = "Spotters – Settings";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Width = 635;
        Height = 400;

        var userLabel = new Label { Text = "User name:", AutoSize = true };
        var deviceLabel = new Label { Text = "Audio input:", AutoSize = true };

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(10),
            AutoScroll = true
        };

        // Top controls
        flow.Controls.Add(userLabel);
        flow.Controls.Add(_userNameText);
        flow.Controls.Add(deviceLabel);
        flow.Controls.Add(_deviceCombo);
        flow.Controls.Add(_addButton);
        flow.Controls.Add(new Label { Text = "Users:", AutoSize = true, Padding = new Padding(0, 10, 0, 0) });
        flow.Controls.Add(_usersList);
        flow.Controls.Add(_removeButton);
        flow.Controls.Add(_saveButton);

        Controls.Add(flow);

        Load += async (_, _) => await OnLoadedAsync();
        _addButton.Click += (_, _) => AddUser();
        _removeButton.Click += (_, _) => RemoveSelected();
        _saveButton.Click += async (_, _) => await SaveAndCloseAsync();
    }

    private async Task OnLoadedAsync()
    {
        // Enumerate audio capture devices using NAudio
        await Task.Run(() =>
        {
            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {
                var dev = WaveIn.GetCapabilities(i);
                _devices.Add(dev.ProductName);
            }
        });

        _deviceCombo.Items.Clear();
        foreach (var dev in _devices)
        {
            _deviceCombo.Items.Add(dev);
        }

        RefreshUsersList();
    }

    private void RefreshUsersList()
    {
        _usersList.Items.Clear();
        foreach (var u in _original.Users)
        {
            var productName = _devices.FirstOrDefault(d => d == u.AudioDeviceProductName) ?? "(unknown)";
            _usersList.Items.Add($"{u.UserName} -> {productName}");
        }
    }

    private void AddUser()
    {
        if (string.IsNullOrWhiteSpace(_userNameText.Text))
        {
            MessageBox.Show("User name cannot be empty.", "Spotters", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (_deviceCombo.SelectedIndex < 0)
        {
            MessageBox.Show("Please select an audio input device.", "Spotters", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var device = _devices[_deviceCombo.SelectedIndex];
        _original.Users.Add(new UserAudioMapping
        {
            UserName = _userNameText.Text.Trim(),
            AudioDeviceProductName = device
        });

        _userNameText.Clear();
        _deviceCombo.SelectedIndex = -1;

        RefreshUsersList();
    }

    private void RemoveSelected()
    {
        if (_usersList.SelectedIndex < 0) return;
        _original.Users.RemoveAt(_usersList.SelectedIndex);
        RefreshUsersList();
    }

    private async Task SaveAndCloseAsync()
    {
        // Simulate async work if you want to validate/save remotely.
        await Task.CompletedTask;
        Result = _original;
        DialogResult = DialogResult.OK;
        Close();
    }

    private static AppConfig Clone(AppConfig cfg) => new()
    {
        Users = cfg.Users.Select(u => new UserAudioMapping { UserName = u.UserName, AudioDeviceProductName = u.AudioDeviceProductName }).ToList()
    };
}