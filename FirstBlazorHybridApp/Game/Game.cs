using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FirstBlazorHybridApp.Game {
    public class Team
    {
        public string Name { get; set; }
        public IList<Player> ActivePlayers { get; private set; }
        public IList<Player> BenchPlayers { get; private set; }

        private Metadata metadata;
        public Team(Metadata metadata, string name)
        {
            this.metadata = metadata;
            Name = name;

            ActivePlayers = new List<Player>();
            BenchPlayers = new List<Player>();
        }

        public void Add(Player player) {
            if (!metadata.NumActivePlayers.HasValue) {
                ActivePlayers.Add(player);
            }
            else {
                if (ActivePlayers.Count == metadata.NumActivePlayers.Value) {
                    BenchPlayers.Add(player);
                }
                else {
                    ActivePlayers.Add(player);
                }
            }
        }

        public void AddActivePlayer(Player player, int index)
        {
            player.Team = this;
            ActivePlayers.Insert(index, player);
            if (metadata.NumActivePlayers.HasValue)
            {
                if (ActivePlayers.Count > metadata.NumActivePlayers.Value)
                {
                    Player end = ActivePlayers[ActivePlayers.Count - 1];
                    ActivePlayers.RemoveAt(ActivePlayers.Count - 1);
                    BenchPlayers.Insert(0, end);
                }
            }
        }

        public void AddBenchPlayer(Player player, int index)
        {
            player.Team = this;
            BenchPlayers.Insert(index, player);
        }

        public void RemoveActivePlayer(int index)
        {
            ActivePlayers.RemoveAt(index);
        }

        public void RemoveBenchPlayer(int index)
        {
            BenchPlayers.RemoveAt(index);
        }

    }

    public class Player
    {
        public string Name { get; set; }

        [JsonIgnore]
        public Team Team { get; set; }

        public Player(string name, Team team)
        {
            Name = name;
            Team = team;
        }
    }

    public enum QuestionStatus
    {
        TOSSUP,
        BONUS,
        BOUNCED_BONUS,
        COMPLETE,
    }

    public class Question
    {
        private Metadata metadata;
        public TossupResult? TossupResult { get; private set; }
        public List<BonusResult> BonusResults { get; private set; } = new List<BonusResult>();

        public List<Player> HeardBy { get; } = new List<Player>();
        public Player? AnsweredByPlayer { get; private set; }
        public Team? AnsweredByTeam { get; private set; }
        public List<Player> NegsByPlayer { get; } = new List<Player>();
        public List<Team> NegsByTeam { get; } = new List<Team>();
        public QuestionStatus QuestionStatus { get; private set; } = QuestionStatus.TOSSUP;

        public Question(Metadata metadata)
        {
            this.metadata = metadata;
        }

        public int GetPtsEarned(Team team)
        {
            int score = 0;
            if (NegsByTeam.Contains(team))
            {
                score += metadata.NegWeight ?? 0;
            }

            if (AnsweredByTeam == team)
            {
                score += TossupResult == Game.TossupResult.POWER
                    ? (metadata.PowerWeight ?? metadata.TossupWeight)
                    : metadata.TossupWeight;

                foreach (var bonusResult in BonusResults)
                {
                    score += bonusResult == BonusResult.ANSWERED ? metadata.BonusWeight : 0;
                }
            }
            else
            {
                foreach (var bonusResult in BonusResults)
                {
                    score += bonusResult == BonusResult.BOUNCE_BACK ? metadata.BonusWeight : 0;
                }
            }

            return score;
        }

        public bool CanAnswerTossup(Player player)
        {
            if (QuestionStatus != QuestionStatus.TOSSUP) return false;

            foreach (var negTeam in NegsByTeam)
            {
                if (player.Team == negTeam)
                {
                    return false;
                }
            }

            return true;
        }

        private void AddHeardBy(IList<Team> teams)
        {
            foreach (Team team in teams)
            {
                HeardBy.AddRange(team.ActivePlayers);
            }
        }

        public void AwardPower(Player player, IList<Team> teams)
        {
            // TODO: HARSHA: validation that team didn't already neg the tossup
            TossupResult = Game.TossupResult.POWER;
            AnsweredByPlayer = player;
            AnsweredByTeam = player.Team;
            AddHeardBy(teams);
            QuestionStatus = QuestionStatus.BONUS;
        }

        public void AwardTossup(Player player, IList<Team> teams)
        {
            // TODO: HARSHA: validation that team didn't already neg the tossup
            TossupResult = Game.TossupResult.TOSSUP;
            AnsweredByPlayer = player;
            AnsweredByTeam = player.Team;
            AddHeardBy(teams);
            QuestionStatus = QuestionStatus.BONUS;
        }

        public void AwardNeg(Player player)
        {
            NegsByPlayer.Add(player);
            NegsByTeam.Add(player.Team);
        }

        public void AwardNoAnswer(IList<Team> teams)
        {
            // TODO: HARSHA: we could have a bug with bouncebacks if we allow you to retroactively change tossup result.
            TossupResult = Game.TossupResult.NO_ANSWER;
            AnsweredByPlayer = null;
            AnsweredByTeam = null;
            AddHeardBy(teams);
            QuestionStatus = QuestionStatus.COMPLETE;
        }

        public void AwardBonus(bool isCorrect)
        {
            if (TossupResult == Game.TossupResult.NO_ANSWER || QuestionStatus == QuestionStatus.TOSSUP || QuestionStatus == QuestionStatus.COMPLETE)
            {
                throw new Exception("Can't award bonus if the question is not in bonus phase.");
            }

            if (BonusResults.Count >= this.metadata.NumBonusPerTossup)
            {
                throw new Exception($"Can't award more than {this.metadata.NumBonusPerTossup} bonuses per tossup.");
            }

            if (isCorrect)
            {
                if (QuestionStatus == QuestionStatus.BONUS)
                {
                    BonusResults.Add(BonusResult.ANSWERED);
                }
                else
                {
                    // question status is bounced_bonus
                    BonusResults.Add(BonusResult.BOUNCE_BACK);
                    QuestionStatus = QuestionStatus.BONUS;
                }
            }
            else if (metadata.BounceBacks)
            {
                if (QuestionStatus == QuestionStatus.BONUS)
                {
                    QuestionStatus = QuestionStatus.BOUNCED_BONUS;
                }
                else
                {
                    // question status is bounced_bonus
                    BonusResults.Add(BonusResult.NO_ANSWER);
                    QuestionStatus = QuestionStatus.BONUS;
                }
            }
            else
            {
                BonusResults.Add(BonusResult.NO_ANSWER);
            }

            if (BonusResults.Count == metadata.NumBonusPerTossup)
            {
                QuestionStatus = QuestionStatus.COMPLETE;
            }
        }
    }
}