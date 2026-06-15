/// <summary>보스 클리어 후 부하 소환을 차단합니다.</summary>
public static class BossSummonGuard
{
	public static bool IsBlocked()
	{
		GameManager game = GameManager.instance;
		if (game == null || !game.isLive)
			return true;

		WaveManager wave = StageManager.instance != null ? StageManager.instance.waveManager : null;
		return wave != null && wave.IsBossPhaseCleared;
	}
}
