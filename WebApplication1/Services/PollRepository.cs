using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

public class PollRepository
{
    private const string DataFilePath = "Data/polls.json";
    private List<Poll> _polls;
    private readonly object _lock = new object();

    public PollRepository()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DataFilePath) ?? "Data");
        LoadPolls();
    }

    public List<Poll> GetAllPolls() => _polls.ToList();

    public Poll GetPollById(int id) => _polls.FirstOrDefault(p => p.Id == id);

    public void AddPoll(string question, List<string> options, string creatorUsername, DateTime? expireDateTime = null, string description = "")
    {
        lock (_lock)
        {
            var poll = new Poll
            {
                Question = question,
                Description = description,
                CreatorUsername = creatorUsername,
                CreatedAt = DateTime.Now,
                ExpiresAt = expireDateTime,
                Options = options.Select((text, index) => new PollOption
                {
                    Id = index + 1,
                    Text = text,
                    Votes = 0
                }).ToList(),
                UserVotes = new Dictionary<string, int>()
            };

            poll.Id = _polls.Count > 0 ? _polls.Max(p => p.Id) + 1 : 1;
            _polls.Add(poll);
            SavePolls();
        }
    }

    public void AddPoll(Poll poll)
    {
        lock (_lock)
        {
            poll.Id = _polls.Count > 0 ? _polls.Max(p => p.Id) + 1 : 1;

            for (int i = 0; i < poll.Options.Count; i++)
            {
                if (poll.Options[i].Id == 0)
                {
                    poll.Options[i].Id = i + 1;
                }
            }

            _polls.Add(poll);
            SavePolls();
        }
    }
    public bool AddVote(int pollId, string userEmail, int optionIndex)
    {
        lock (_lock)
        {
            var poll = GetPollById(pollId);
            if (poll == null || poll.IsExpired) return false;
            if (optionIndex < 0 || optionIndex >= poll.Options.Count) return false;

            if (poll.UserVotes.ContainsKey(userEmail))
            {
                return false; 
            }

            poll.UserVotes.Add(userEmail, optionIndex);
            poll.UpdateVoteCounts();
            SavePolls();
            return true;
        }
    }
    public bool DeletePoll(int id)
    {
        lock (_lock)
        {
            var poll = GetPollById(id);
            if (poll == null) return false;

            _polls.Remove(poll);
            SavePolls();
            return true;
        }
    }
    private void LoadPolls()
    {
        try
        {
            if (File.Exists(DataFilePath))
            {
                var json = File.ReadAllText(DataFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() }
                };

                _polls = JsonSerializer.Deserialize<List<Poll>>(json, options) ?? new List<Poll>();

                foreach (var poll in _polls.Where(p => p.UserVotes == null))
                {
                    poll.UserVotes = new Dictionary<string, int>();
                }

                foreach (var poll in _polls)
                {
                    poll.UpdateVoteCounts();
                }
            }
            else
            {
                _polls = new List<Poll>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading polls: {ex.Message}");
            _polls = new List<Poll>();
        }
    }

    private void SavePolls()
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(_polls, options);
            File.WriteAllText(DataFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving polls: {ex.Message}");
        }
    }
}