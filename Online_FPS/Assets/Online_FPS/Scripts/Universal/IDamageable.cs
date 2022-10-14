public interface IDamageable
{
    public void TakeDamage(float _dmg, string _damager = "", int _actor = -1);
    public void Heal(float value);
    public bool CanHeal();
}
