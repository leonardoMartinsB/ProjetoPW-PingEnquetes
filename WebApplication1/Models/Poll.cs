using System;
using System.Collections.Generic;
using System.Linq;

public class Poll
{
    public int Id { get; set; }
    public string Question { get; set; }
    public string Description { get; set; } = string.Empty;
    public string CreatorUsername { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? ExpiresAt { get; set; }
    public List<PollOption> Options { get; set; } = new List<PollOption>();
    public Dictionary<string, int> UserVotes { get; set; } = new Dictionary<string, int>();

    public bool IsActive => !IsExpired;
    public bool IsExpired => ExpiresAt.HasValue && DateTime.Now > ExpiresAt.Value;
    public DateTime EndDate => ExpiresAt ?? DateTime.MaxValue;
    public int TotalVotes => UserVotes.Count;
    public int ParticipantsCount => UserVotes.Keys.Distinct().Count();

    public void UpdateVoteCounts()
    {
        foreach (var option in Options)
        {
            option.Votes = 0;
        }

        foreach (var vote in UserVotes.Values)
        {
            if (vote >= 0 && vote < Options.Count)
            {
                Options[vote].Votes++;
            }
        }
    }

    public bool HasUserVoted(string userEmail) => UserVotes.ContainsKey(userEmail);

    public int? GetUserVote(string userEmail) =>
        UserVotes.TryGetValue(userEmail, out var vote) ? vote : null;
    public List<int> GetVoteCounts()
    {
        return Options.Select(o => o.Votes).ToList();
    }
}

public class PollOption
{
    public int Id { get; set; }
    public string Text { get; set; }
    public int Votes { get; set; }
}