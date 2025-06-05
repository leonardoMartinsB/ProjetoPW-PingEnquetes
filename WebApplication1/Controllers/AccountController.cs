using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.RegularExpressions;

public class AccountController : Controller
{
    private readonly AuthDataService _authData;
    private readonly ILogger<AccountController> _logger;

    public AccountController(AuthDataService authData, ILogger<AccountController> logger)
    {
        _authData = authData;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string returnUrl = null)
    {
        if (User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Index", "Enquete");
        }
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password, string returnUrl = null, bool rememberMe = false)
    {
        try
        {
            var user = _authData.GetUserByEmail(email);
            if (user == null || user.Password != password)
            {
                _logger.LogWarning("Tentativa de login falhou para o email: {Email}", email);
                return Json(new { success = false, message = "Email ou senha incorretos." });
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("UserId", user.Email)
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(1),
                AllowRefresh = true
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            _logger.LogInformation("Usuário {Email} logado com sucesso", email);
            return Json(new
            {
                success = true,
                message = "Login realizado com sucesso!",
                redirectUrl = Url.Action("Index", "Enquete")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante o login para o email: {Email}", email);
            return Json(new { success = false, message = "Ocorreu um erro durante o login." });
        }
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Index", "Enquete");
        }
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(string name, string email, string password)
    {
        try
        {
            if (!Regex.IsMatch(name, @"^[a-zA-ZÀ-ÿ\s]+$"))
            {
                return Json(new { success = false, message = "O nome deve conter apenas letras." });
            }

            if (_authData.Users.Any(u => u.Email == email))
            {
                return Json(new { success = false, message = "Este email já está em uso." });
            }

            var newUser = new User
            {
                Name = name.Trim(),
                Email = email.ToLower().Trim(),
                Password = password
            };

            _authData.AddUser(newUser);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, newUser.Name),
                new Claim(ClaimTypes.Email, newUser.Email),
                new Claim("UserId", newUser.Email)
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
                });

            _logger.LogInformation("Novo usuário registrado: {Email}", email);
            return Json(new
            {
                success = true,
                message = $"Cadastro realizado com sucesso, {name}!",
                redirectUrl = Url.Action("Index", "Enquete")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante registro para o email: {Email}", email);
            return Json(new { success = false, message = "Ocorreu um erro durante o registro." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var userName = User.Identity?.Name ?? "Usuário desconhecido";

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (HttpContext.Session != null)
            {
                HttpContext.Session.Clear();
            }

            Response.Cookies.Delete(".AspNetCore.Cookies");
            Response.Cookies.Delete("Identity.Application");

            _logger.LogInformation("Usuário {UserName} realizou logout com sucesso", userName);

            return Json(new
            {
                success = true,
                message = "Logout realizado com sucesso!",
                redirectUrl = Url.Action("Index", "Home")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante logout para o usuário: {UserName}", User.Identity?.Name);
            try
            {
                if (HttpContext.Session != null)
                {
                    HttpContext.Session.Clear();
                }
                Response.Cookies.Delete(".AspNetCore.Cookies");
                Response.Cookies.Delete("Identity.Application");
            }
            catch
            {
                
            }

            return Json(new
            {
                success = false,
                message = "Ocorreu um erro durante o logout, mas a sessão foi encerrada."
            });
        }
    }
}