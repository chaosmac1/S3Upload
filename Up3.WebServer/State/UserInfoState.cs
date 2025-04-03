using BlazorApp1.Enum;
namespace BlazorApp1.State;

public record struct UserInfoValue(
    string Username,
    string Token,
    EUserType Type
);

public interface IUserInfoState: IState<UserInfoValue> { }

public sealed class UserInfoStateState: State<UserInfoValue>, IUserInfoState;