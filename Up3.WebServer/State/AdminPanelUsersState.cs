using System.Runtime.CompilerServices;
using BlazorApp1.Enum;
using BlazorApp1.Interface;
namespace BlazorApp1.State;

public record struct NewPassword(bool HasNewPassword, string? Password);
public record struct AdminPanelUserSetting(IUserId Id, string Username, EUserType UserType, NewPassword NewPassword);

public record struct AdminPanelUsersValue(
    IReadOnlyDictionary<IUserId,AdminPanelUserSetting> Old,
    IReadOnlyDictionary<IUserId,AdminPanelUserSetting> New,
    bool CanSave,
    bool IsNoChanges,
    bool IsInitialised
);

public class AdminPanelUsersState: State<AdminPanelUsersValue> {
    public async Task SaveChangesAsync(CancellationToken token = default) {
        this.Value = Value with {
            IsNoChanges = false,
            CanSave = false,
            Old = this.Value.New,
        };

        throw new NotFiniteNumberException(nameof(SaveChangesAsync));
        // TODO Write Save To DB
    }

    public void UpdateAdminPanelUser(AdminPanelUserSetting changes) {
        this.Value = Value with {
            New = this.Value.New.Values
                      .Select(x => x.Id == changes.Id? changes: x)
                      .ToDictionary(x => x.Id)
        };
    }

    public void RollbackToOldState() {
        this.Value = Value with {
            New = Value.Old
        };
    }

    public async Task InitialsAsync() {
        throw new NotImplementedException(nameof(InitialsAsync));
    }
}