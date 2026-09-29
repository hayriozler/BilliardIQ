namespace Scoreboard.WebApp.Domain;

// ---------- Identity / Organization ----------
public enum UserStatus { Active, Invited, Suspended }
public enum SubscriptionPlan { Free, Basic, Pro }
public enum StaffRole { Owner, Manager, Cashier, Waiter, Referee }

// ---------- Table / Device ----------
public enum TableType { Match284, Size250, Size230, Pool, Snooker }
public enum TableStatus { Available, InUse, Reserved, Maintenance, OutOfService }
public enum DeviceType { Scoreboard, Display, Pos }
public enum DevicePlatform { Android, Ios, Web }

// ---------- People ----------
public enum Gender { Male, Female, Other, Undisclosed }
public enum Handedness { Right, Left }
public enum ClubMembershipRole { Member, Captain, Coach, Manager }
public enum TeamMemberRole { Captain, Player, Reserve }
public enum MembershipTier { Standard, Silver, Gold, Student }

// ---------- Session / Reservation / POS ----------
public enum ReservationStatus { Pending, Confirmed, CheckedIn, Cancelled, NoShow }
public enum TableSessionStatus { Open, Paused, Closed, Settled, Voided }
public enum OrderItemStatus { Ordered, Served, Cancelled }
public enum PaymentMethod { Cash, Card, Transfer, PrepaidBalance, Complimentary }
public enum PaymentStatus { Completed, Refunded, Failed }

// ---------- Scoring ----------
public enum Discipline
{
    ThreeCushion,   // 3 bant
    OneCushion,     // 1 bant
    Straight,       // serbest
    Balkline47_1,
    Balkline47_2,   // kadre 47/2
    Balkline71_2,
    Artistic
}

public enum MatchFormat { Points, Sets }
public enum MatchStatus { Scheduled, Warmup, Live, Paused, Finished, Abandoned, Walkover }
public enum MatchContext { Casual, Training, Tournament, League }
public enum Side { A, B }
public enum BallColor { White, Yellow }
public enum MatchResult { Win, Loss, Draw }

public enum MatchEventType
{
    // yaşam döngüsü
    MatchCreated, LagCompleted, MatchStarted, MatchPaused, MatchResumed,
    MatchFinished, MatchAbandoned, SetStarted, SetFinished,
    // oyun
    Point, PointAdjusted, Miss, Foul, ExtensionUsed, TimeoutCalled,
    ShotClockExpired, PenaltyShot,
    // düzeltme
    Undo
}

// ---------- Competition ----------
public enum TournamentStatus { Draft, RegistrationOpen, RegistrationClosed, InProgress, Completed, Cancelled }
public enum StageType { RoundRobin, SingleElimination, DoubleElimination, Swiss }
public enum Tiebreaker { MatchPoints, GeneralAverage, HighRun, HeadToHead, BestGameAverage }
public enum EntryStatus { Registered, Confirmed, Withdrawn, Disqualified, Eliminated }
public enum SeasonStatus { Upcoming, Active, Completed }
public enum FixtureStatus { Scheduled, InProgress, Completed, Postponed, Forfeited }

// ---------- Stats ----------
public enum StatsScope { All, Official, Casual }
public enum RatingSystem { Elo, AverageBased }
