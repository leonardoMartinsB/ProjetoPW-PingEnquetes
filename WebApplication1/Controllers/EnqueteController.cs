using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

public class EnqueteController : Controller
{
    private readonly PollRepository _pollRepository;

    public EnqueteController(PollRepository pollRepository)
    {
        _pollRepository = pollRepository;
    }

    [HttpGet]
    public IActionResult Index()
    {
        if (!User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index") });
        }

        var polls = _pollRepository.GetAllPolls()
            .OrderByDescending(p => p.CreatedAt)
            .ToList();

        foreach (var poll in polls)
        {
            poll.UpdateVoteCounts();
        }

        return View(polls);
    }

    [HttpGet]
    public IActionResult Listar()
    {
        if (!User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Listar") });
        }

        var polls = _pollRepository.GetAllPolls()
            .OrderByDescending(p => p.CreatedAt)
            .ToList();

        foreach (var poll in polls)
        {
            poll.UpdateVoteCounts();
        }

        return View(polls);
    }

    [HttpGet]
    public IActionResult Criar()
    {
        if (!User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Criar") });
        }

        return View();
    }

    [HttpPost]
    public IActionResult Criar(string question, List<string> options, DateTime? expireDateTime, string description = "")
    {
        if (!User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Login", "Account");
        }

        options = options?.Where(o => !string.IsNullOrWhiteSpace(o)).ToList();

        if (string.IsNullOrWhiteSpace(question))
        {
            ModelState.AddModelError("", "A pergunta da enquete é obrigatória");
            return View();
        }
            
        if (options == null || options.Count < 2)
        {
            ModelState.AddModelError("", "Uma enquete precisa ter pelo menos 2 opções");
            return View();
        }

        _pollRepository.AddPoll(question, options, GetCurrentUserName(), expireDateTime, description);

        TempData["SuccessMessage"] = "Enquete criada com sucesso!";
        return RedirectToAction("Listar");
    }

    [HttpGet]
    public IActionResult Detalhes(int id)
    {
        if (!User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Detalhes", new { id }) });
        }

        var poll = _pollRepository.GetPollById(id);
        if (poll == null)
        {
            return NotFound();
        }

        poll.UpdateVoteCounts();

        var currentUserEmail = GetCurrentUserEmail();
        ViewBag.UserVoted = poll.UserVotes.ContainsKey(currentUserEmail) ? poll.UserVotes[currentUserEmail] : (int?)null;
        ViewBag.HasVoted = poll.UserVotes.ContainsKey(currentUserEmail);
        ViewBag.IsCreator = poll.CreatorUsername == GetCurrentUserName();
        ViewBag.VoteCounts = poll.GetVoteCounts();

        return View(poll);
    }

    [HttpGet]
    public IActionResult Votar(int id)
    {
        if (!User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Votar", new { id }) });
        }

        var poll = _pollRepository.GetPollById(id);
        if (poll == null)
        {
            return NotFound();
        }

        if (poll.IsExpired)
        {
            TempData["ErrorMessage"] = "Esta enquete já expirou.";
            return RedirectToAction("Detalhes", new { id });
        }

        var currentUserEmail = GetCurrentUserEmail();
        if (poll.UserVotes.ContainsKey(currentUserEmail))
        {
            TempData["ErrorMessage"] = "Você já votou nesta enquete.";
            return RedirectToAction("Detalhes", new { id });
        }

        poll.UpdateVoteCounts();
        return View(poll);
    }

    [HttpPost]
    public IActionResult Votar(int pollId, int optionIndex)
    {
        if (!User.Identity.IsAuthenticated)
            return RedirectToAction("Login", "Account");

        var userEmail = GetCurrentUserEmail();
        var success = _pollRepository.AddVote(pollId, userEmail, optionIndex);

        if (!success)
        {
            TempData["ErrorMessage"] = "Não foi possível registrar seu voto. A enquete pode ter expirado ou você já votou.";
        }
        else
        {
            TempData["SuccessMessage"] = "Seu voto foi registrado com sucesso!";
        }

        return RedirectToAction("Detalhes", new { id = pollId });
    }

    [HttpPost]
    public IActionResult Vote([FromBody] VoteRequest request)
    {
        if (!User.Identity.IsAuthenticated)
            return Json(new { success = false, message = "Você precisa estar logado para votar." });

        var userEmail = GetCurrentUserEmail();
        var success = _pollRepository.AddVote(request.PollId, userEmail, request.OptionId - 1);

        if (success)
        {
            var poll = _pollRepository.GetPollById(request.PollId);
            poll?.UpdateVoteCounts();
            return Json(new { success = true, message = "Voto registrado com sucesso!" });
        }
        else
        {
            return Json(new { success = false, message = "Não foi possível registrar seu voto. A enquete pode ter expirado." });
        }
    }

    [HttpPost]
    public IActionResult Excluir(int id)
    {
        if (!User.Identity.IsAuthenticated)
            return RedirectToAction("Login", "Account");

        var poll = _pollRepository.GetPollById(id);
        if (poll == null)
        {
            return NotFound();
        }

        if (poll.CreatorUsername != GetCurrentUserName())
        {
            return Forbid();
        }

        _pollRepository.DeletePoll(id);
        TempData["SuccessMessage"] = "Enquete excluída com sucesso!";

        return RedirectToAction("Listar");
    }

    [HttpGet]
    public IActionResult Gerenciar()
    {
        if (!User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Gerenciar") });
        }

        var userPolls = _pollRepository.GetAllPolls()
            .Where(p => p.CreatorUsername == GetCurrentUserName())
            .OrderByDescending(p => p.CreatedAt)
            .ToList();

        foreach (var poll in userPolls)
        {
            poll.UpdateVoteCounts();
        }

        return View(userPolls);
    }

    [HttpGet]
    public IActionResult GetPollsData()
    {
        if (!User.Identity.IsAuthenticated)
        {
            return Json(new { success = false, message = "Não autenticado" });
        }

        var polls = _pollRepository.GetAllPolls()
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new {
                id = p.Id,
                question = p.Question,
                description = p.Description,
                isExpired = p.IsExpired,
                isActive = p.IsActive,
                creatorUsername = p.CreatorUsername,
                createdAt = p.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                expiresAt = p.ExpiresAt?.ToString("yyyy-MM-dd HH:mm"),
                totalVotes = p.TotalVotes,
                optionsCount = p.Options.Count,
                participantsCount = p.ParticipantsCount
            })
            .ToList();

        return Json(new { success = true, polls });
    }

    private string GetCurrentUserEmail()
    {
        return User.FindFirst(ClaimTypes.Email)?.Value ?? "";
    }

    private string GetCurrentUserName()
    {
        return User.FindFirst(ClaimTypes.Name)?.Value ??
               User.FindFirst(ClaimTypes.Email)?.Value ??
               "";
    }
}

public class VoteRequest
{
    public int PollId { get; set; }
    public int OptionId { get; set; }
}