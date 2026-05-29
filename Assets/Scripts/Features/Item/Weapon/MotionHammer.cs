public class MotionHammer : Motion
{
	protected override void OnStartMotion() {}

	protected override float GetDefaultTime() => instance.spawntime;

	protected override bool ShouldDestroyOnHit() => false;
}
