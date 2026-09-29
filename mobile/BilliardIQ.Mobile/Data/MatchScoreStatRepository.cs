using BillardIQ.Mobile.Models;
using BillardIQ.Mobile.Services;

namespace BillardIQ.Mobile.Data;

public class MatchScoreStatRepository(DatabaseExecutor dbExecutor) : BaseRepo
{
    public async Task<IReadOnlyList<MatchScoreStat>> GetByMatchResultIdAsync(int matchResultId) =>
        await dbExecutor.ReadDataAsync<MatchScoreStat>(
            "SELECT Id, MatchResultId, PlayerSlot, BucketIndex, TotalPoints FROM match_score_stat WHERE MatchResultId = @MatchResultId ORDER BY BucketIndex",
            [new("@MatchResultId", matchResultId)]);
}
