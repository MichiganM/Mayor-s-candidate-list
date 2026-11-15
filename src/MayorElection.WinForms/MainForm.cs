using MayorElection.Domain.Enums;
using MayorElection.Services;
using MayorElection.Services.DTOs;
using MayorElection.WinForms.Ui;
using Microsoft.Extensions.Logging;
using U = MayorElection.WinForms.Ui.Ui;

namespace MayorElection.WinForms;

public sealed class MainForm : ServiceForm
{
    private readonly CandidateService candidates;
    private readonly ListOrderService order;
    private readonly ExportService files;
    private readonly Func<CandidateEditForm> editor;
    private readonly DataGridView grid = new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        AutoGenerateColumns = false, MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        RowHeadersVisible = false, BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
    };
    private readonly ComboBox statusFilter = U.Combo(), templateFilter = U.Combo(), newStatus = U.Combo();
    private readonly Label summary = new() { AutoSize = true, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    public event EventHandler? LogoutRequested;

    public MainForm(CandidateService candidates, ListOrderService order, ExportService files,
        UserSession session, Func<CandidateEditForm> editor, Func<TwoFactorSetupForm> twoFactor,
        ILogger<MainForm> logger) : base(logger)
    {
        this.candidates = candidates; this.order = order; this.files = files; this.editor = editor;
        Text = $"Выборы мэра — список кандидатов | {session.RequireUser().UserName}";
        ClientSize = new Size(1180, 700); MinimumSize = new Size(1050, 600);
        U.Statuses(statusFilter, true); U.Templates(templateFilter, true); U.Statuses(newStatus);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var heading = new Label { Text = "Кандидаты на выборах мэра", Font = new Font("Segoe UI", 18, FontStyle.Bold), AutoSize = true, Padding = new Padding(0, 0, 0, 14) };
        var filters = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(0, 4, 0, 10) };
        statusFilter.Width = 200; templateFilter.Width = 225;
        filters.Controls.Add(new Label { Text = "Показать:", AutoSize = true, Margin = new Padding(4, 8, 8, 4) });
        filters.Controls.Add(statusFilter); filters.Controls.Add(templateFilter);
        var refresh = U.Button("Обновить"); filters.Controls.Add(refresh);
        foreach (var (property, caption, weight) in new[]
        {
            ("Position", "№", 35F), ("FullName", "ФИО", 180F), ("BirthDate", "Дата рождения", 80F),
            ("Template", "Шаблон", 115F), ("Party", "Партия", 100F),
            ("RegistrationDate", "Регистрация", 85F), ("Status", "Статус", 100F)
        }) grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = property, HeaderText = caption, FillWeight = weight, SortMode = DataGridViewColumnSortMode.NotSortable
        });
        grid.ColumnHeadersHeight = 40; grid.RowTemplate.Height = 32;
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(239, 246, 248);
        Button add = U.Button("Добавить"), edit = U.Button("Изменить"), delete = U.Button("Удалить");
        Button up = U.Button("↑ Вверх"), down = U.Button("↓ Вниз"), apply = U.Button("Сменить статус");
        newStatus.Width = 170;
        var actions = U.Actions(add, edit, delete, up, down, newStatus, apply);
        Button save = U.Button("Сохранить JSON"), load = U.Button("Загрузить JSON");
        Button setup = U.Button("Настроить 2FA"), logout = U.Button("Выйти");
        var bottom = U.Actions(save, load, setup, logout, summary);
        layout.Controls.Add(heading, 0, 0); layout.Controls.Add(filters, 0, 1);
        layout.Controls.Add(grid, 0, 2); layout.Controls.Add(actions, 0, 3); layout.Controls.Add(bottom, 0, 4);
        Controls.Add(layout);
        Shown += async (_, _) => await RunAsync(() => RefreshAsync());
        refresh.Click += async (_, _) => await RunAsync(() => RefreshAsync());
        statusFilter.SelectedIndexChanged += async (_, _) => await RunAsync(() => RefreshAsync());
        templateFilter.SelectedIndexChanged += async (_, _) => await RunAsync(() => RefreshAsync());
        add.Click += async (_, _) => await RunAsync(() => EditAsync(null));
        edit.Click += async (_, _) => await RunAsync(() => EditAsync(SelectedId()));
        grid.CellDoubleClick += async (_, e) => { if (e.RowIndex >= 0) await RunAsync(() => EditAsync(SelectedId())); };
        delete.Click += async (_, _) => await RunAsync(async () =>
        {
            var id = SelectedId();
            if (MessageBox.Show(this, "Удалить выбранного кандидата?", "Удаление", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            await candidates.DeleteAsync(id); await RefreshAsync();
        });
        up.Click += async (_, _) => await RunAsync(() => MoveAsync(MoveDirection.Up));
        down.Click += async (_, _) => await RunAsync(() => MoveAsync(MoveDirection.Down));
        apply.Click += async (_, _) => await RunAsync(async () =>
        {
            var id = SelectedId();
            await candidates.ChangeStatusAsync(id, U.Selected<CandidateStatus>(newStatus)!.Value);
            await RefreshAsync(id);
        });
        save.Click += async (_, _) => await RunAsync(async () =>
        {
            using var dialog = new SaveFileDialog { Filter = "Список кандидатов (*.json)|*.json", DefaultExt = "json", FileName = "Кандидаты.json", AddExtension = true };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            await files.SaveJsonAsync(dialog.FileName);
            MessageBox.Show(this, "Полный список сохранён.", "Экспорт");
        });
        load.Click += async (_, _) => await RunAsync(async () =>
        {
            using var dialog = new OpenFileDialog { Filter = "Список кандидатов (*.json)|*.json", CheckFileExists = true };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            if (MessageBox.Show(this, "Загрузка полностью заменит текущий список кандидатов. Продолжить?", "Импорт списка",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            await files.LoadJsonAsync(dialog.FileName); await RefreshAsync();
        });
        setup.Click += (_, _) => { using var form = twoFactor(); form.ShowDialog(this); if (!session.IsAuthenticated) BeginInvoke(() => LogoutRequested?.Invoke(this, EventArgs.Empty)); };
        logout.Click += (_, _) => LogoutRequested?.Invoke(this, EventArgs.Empty);
    }

    private Guid SelectedId() => grid.CurrentRow?.DataBoundItem is CandidateRow row ? row.Id
        : throw new InvalidOperationException("Выберите кандидата в списке.");

    private async Task RefreshAsync(Guid? selectedId = null)
    {
        var list = await candidates.ListAsync(U.Selected<CandidateStatus>(statusFilter), U.Selected<CandidateTemplate>(templateFilter));
        grid.DataSource = list.Select(CandidateRow.FromDto).ToList();
        summary.Text = $"Показано: {list.Count}";
        if (selectedId.HasValue)
            foreach (DataGridViewRow row in grid.Rows)
                if (row.DataBoundItem is CandidateRow candidate && candidate.Id == selectedId)
                { grid.CurrentCell = row.Cells[0]; break; }
    }

    private async Task EditAsync(Guid? id)
    {
        using var form = editor();
        if (id.HasValue) await form.LoadCandidateAsync(id.Value);
        if (form.ShowDialog(this) == DialogResult.OK) await RefreshAsync(form.SavedCandidateId);
    }

    private async Task MoveAsync(MoveDirection direction)
    {
        var id = SelectedId(); await order.MoveAsync(id, direction); await RefreshAsync(id);
    }

    private sealed record CandidateRow(Guid Id, int Position, string FullName, string BirthDate,
        string Template, string? Party, string RegistrationDate, string Status)
    {
        public static CandidateRow FromDto(CandidateDto dto) => new(dto.Id, dto.Position,
            string.Join(" ", new[] { dto.LastName, dto.FirstName, dto.Patronymic }.Where(x => !string.IsNullOrWhiteSpace(x))),
            dto.BirthDate.ToString("dd.MM.yyyy"), U.Template(dto.Template), dto.PartyName,
            dto.RegistrationDate.ToString("dd.MM.yyyy"), U.Status(dto.Status));
    }
}
