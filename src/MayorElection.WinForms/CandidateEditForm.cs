using MayorElection.Domain.Enums;
using MayorElection.Services;
using MayorElection.Services.DTOs;
using MayorElection.WinForms.Ui;
using Microsoft.Extensions.Logging;
using U = MayorElection.WinForms.Ui.Ui;

namespace MayorElection.WinForms;

public sealed class CandidateEditForm : ServiceForm
{
    private readonly CandidateService candidates;
    private readonly TextBox lastName = U.Input(), firstName = U.Input(), patronymic = U.Input(), party = U.Input();
    private readonly ComboBox template = U.Combo(), status = U.Combo();
    private readonly DateTimePicker birth = new() { Format = DateTimePickerFormat.Short, Dock = DockStyle.Fill };
    private readonly DateTimePicker registration = new() { Format = DateTimePickerFormat.Short, Dock = DockStyle.Fill };
    private Guid? candidateId;
    public Guid? SavedCandidateId { get; private set; }

    public CandidateEditForm(CandidateService candidates, ILogger<CandidateEditForm> logger) : base(logger)
    {
        this.candidates = candidates;
        Text = "Новый кандидат"; ClientSize = new Size(620, 510); MinimumSize = new Size(620, 550);
        party.MaxLength = 200;
        birth.Value = new DateTime(1980, 1, 1); registration.Value = DateTime.Today;
        U.Templates(template); U.Statuses(status);
        party.Enabled = false;
        template.SelectedIndexChanged += (_, _) => party.Enabled = U.Selected<CandidateTemplate>(template) == CandidateTemplate.PartyCandidate;
        var fields = U.Fields();
        U.Field(fields, "Шаблон", template); U.Field(fields, "Фамилия", lastName);
        U.Field(fields, "Имя", firstName); U.Field(fields, "Отчество (если есть)", patronymic);
        U.Field(fields, "Дата рождения", birth); U.Field(fields, "Дата регистрации", registration);
        U.Field(fields, "Статус", status); U.Field(fields, "Партия", party);
        ValidationField("LastName", lastName); ValidationField("FirstName", firstName);
        ValidationField("Patronymic", patronymic); ValidationField("BirthDate", birth);
        ValidationField("RegistrationDate", registration); ValidationField("PartyName", party);
        ValidationField("Status", status); ValidationField("Template", template);
        var save = U.Button("Сохранить"); var cancel = U.Button("Отмена"); cancel.DialogResult = DialogResult.Cancel;
        save.Click += async (_, _) => await RunAsync(async () =>
        {
            var dto = new CandidateDto
            {
                LastName = lastName.Text, FirstName = firstName.Text, Patronymic = patronymic.Text,
                BirthDate = DateOnly.FromDateTime(birth.Value), RegistrationDate = DateOnly.FromDateTime(registration.Value),
                Status = U.Selected<CandidateStatus>(status)!.Value,
                Template = U.Selected<CandidateTemplate>(template)!.Value,
                PartyName = U.Selected<CandidateTemplate>(template) == CandidateTemplate.PartyCandidate ? party.Text : null
            };
            if (candidateId is { } id)
            {
                await candidates.UpdateAsync(id, dto);
                SavedCandidateId = id;
            }
            else SavedCandidateId = (await candidates.AddAsync(dto)).Id;
            DialogResult = DialogResult.OK;
        });
        Controls.Add(fields); Controls.Add(U.Actions(save, cancel)); AcceptButton = save; CancelButton = cancel;
    }

    public async Task LoadCandidateAsync(Guid id)
    {
        var dto = await candidates.GetAsync(id);
        candidateId = id; Text = $"Редактирование кандидата № {dto.Position}";
        lastName.Text = dto.LastName; firstName.Text = dto.FirstName; patronymic.Text = dto.Patronymic;
        birth.Value = dto.BirthDate.ToDateTime(TimeOnly.MinValue);
        registration.Value = dto.RegistrationDate.ToDateTime(TimeOnly.MinValue);
        U.Select(template, dto.Template); U.Select(status, dto.Status);
        party.Text = dto.PartyName; template.Enabled = false;
    }
}
