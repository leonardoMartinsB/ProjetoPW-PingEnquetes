using Microsoft.AspNetCore.Http;
using System.Text.Json;

public class SessionDataService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string UsersKey = "SessionUsers";
    private const string PollsKey = "SessionPolls";
    private const string CurrentUserIdKey = "CurrentUserId";

    public SessionDataService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ISession Session => _httpContextAccessor.HttpContext.Session;

    public List<User> Users
    {
        get => Session.GetObject<List<User>>(UsersKey) ?? new List<User>();
        set => Session.SetObject(UsersKey, value);
    }

    public List<Poll> Polls
    {
        get => Session.GetObject<List<Poll>>(PollsKey) ?? new List<Poll>();
        set => Session.SetObject(PollsKey, value);
    }

    public User CurrentUser
    {
        get
        {
            var email = Session.GetString(CurrentUserIdKey);
            return email != null ? Users.FirstOrDefault(u => u.Email == email) : null;
        }
        set => Session.SetString(CurrentUserIdKey, value?.Email);
    }

    public bool IsLoggedIn => CurrentUser != null;
}

public static class SessionExtensions
{
    public static void SetObject<T>(this ISession session, string key, T value)
    {
        session.SetString(key, JsonSerializer.Serialize(value));
    }

    public static T GetObject<T>(this ISession session, string key)
    {
        var value = session.GetString(key);
        return value == null ? default : JsonSerializer.Deserialize<T>(value);
    }
}