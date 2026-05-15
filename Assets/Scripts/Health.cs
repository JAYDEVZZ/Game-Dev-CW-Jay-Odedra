using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float maxHealth = 100f;
    private float _currentHealth;

    [Header("Animation")]
    [SerializeField] private AnyStateAnimator anyStateAnimator;

    [Header("Events")]
    public UnityEvent onDeath;
    public UnityEvent<float> onDamageTaken; // passes 0-1 for ui 

    public bool IsDead { get; private set; }
    public float NormalisedHealth => _currentHealth / maxHealth;

    private void Awake()
    {
        _currentHealth = maxHealth;
    }


    public void TakeDamage(float amount)
    {
        if (IsDead) return;

        _currentHealth = Mathf.Max(0f, _currentHealth - amount);
        onDamageTaken.Invoke(NormalisedHealth);

        if (_currentHealth <= 0f) Die();
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        _currentHealth = Mathf.Min(maxHealth, _currentHealth + amount);
        onDamageTaken.Invoke(NormalisedHealth);
    }


    private void Die()
    {
        if (IsDead) return;
        IsDead = true;

        if (anyStateAnimator != null)
            anyStateAnimator.TryPlayAnimaiton("Die");

        onDeath.Invoke();
    }
}