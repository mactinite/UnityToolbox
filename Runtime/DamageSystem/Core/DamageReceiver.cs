using System;
using UnityEngine;

namespace toolbox.DamageSystem
{
    /// <summary>
    /// DamageReceiver that simply tracks health and fires events.
    /// Forms the base for the DamageReceiver system.
    /// </summary>
    public class DamageReceiver<T> : MonoBehaviour where T : DamageBase, new()
    {
        public float health = 100;
        public float maxHealth = 100;
        public bool destroyed = false;
        public Action<Vector2, T> OnDamage;
        public Action<float> OnHeal;
        public Action<Vector2> OnDestroyed;
        public Func<T, T> OnProcessDamage;

        [HideInInspector]
        public bool wasDamagedThisFrame = false;


        private float iTimer = 0;
        public float iTime = 0.08f;
        public bool invincible = false;
        public virtual void Update()
        {
            if (iTimer >= 0)
            {
                iTimer -= Time.deltaTime;
            }

        }

        private void LateUpdate()
        {
            if (wasDamagedThisFrame)
            {
                wasDamagedThisFrame = false;
            }
        }

        public void Damage(T damage)
        {
            DamageAt(damage, transform.position);
        }

        public void DamageAt(T damage, Vector2 at)
        {
            if (iTimer > 0 || invincible) return;
            // let extensions pre-process damage. Iterate through registered processors and execute the method
            T dmg = damage;
            if (OnProcessDamage != null)
            {
                foreach (Func<T, T> subscriber in OnProcessDamage.GetInvocationList())
                {
                    dmg = subscriber(dmg);
                }
            }

            if (dmg.Amount <= 0) return;

            // apply damage and invoke events
            if (health - dmg.Amount <= 0 && !destroyed)
            {
                destroyed = true;
                health = 0;
                dmg.NewHealth = health;
                // emit destroyed event and let extensions handle reactions like recycling or destroying.
                OnDamage?.Invoke(at, dmg);
                OnDestroyed?.Invoke(at);
            }
            else
            {
                health -= dmg.Amount;
                dmg.NewHealth = health;
                // same as destroyed event, but for damage. Extensions can handle things like spawning effects.
                OnDamage?.Invoke(at, dmg);
            }
            iTimer = iTime;
            wasDamagedThisFrame = true;
        }

        public void Heal(float amount)
        {
            if (health + amount <= maxHealth)
            {
                health = health + amount;
                OnHeal?.Invoke(amount);
            }
            else
            {
                var healAmt = maxHealth - health;
                health = maxHealth;
                OnHeal?.Invoke(healAmt);
            }
        }
    }
}
